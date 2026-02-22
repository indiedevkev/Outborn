using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

/// <summary>
/// DayNightSystem - Steuert Sonne, Mond, Licht und Himmelfarben
/// Sonne geht um ~6:00 auf, steht um 12:00 oben, geht um ~22:00/23:00 unter
/// </summary>
public class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance { get; private set; }

    [Header("Zeit")]
    [Range(0f, 24f)] public float currentHour = 8f;
    [Range(0.1f, 100f)] public float timeScale = 1f;
    public bool pauseTime = false;

    [Header("Sonne")]
    public Light sunLight;
    public Transform sunTransform;
    [ColorUsage(true, true)] public Color sunriseSunsetColor = new Color(1f, 0.4f, 0.1f, 1f);
    [ColorUsage(true, true)] public Color noonColor = new Color(1f, 0.95f, 0.8f, 1f);
    [ColorUsage(true, true)] public Color goldenHourColor = new Color(1f, 0.6f, 0.1f, 1f);
    public float maxSunIntensity = 3f;
    public float sunriseHour = 6f;
    public float sunsetHour = 22f;

    [Header("Mond")]
    public Light moonLight;
    public Transform moonTransform;
    [ColorUsage(true, true)] public Color moonColor = new Color(0.4f, 0.5f, 0.7f, 1f);
    public float maxMoonIntensity = 0.3f;

    [Header("Ambient Light")]
    [ColorUsage(true, true)] public Color dayAmbient = new Color(0.5f, 0.6f, 0.8f, 1f);
    [ColorUsage(true, true)] public Color sunriseAmbient = new Color(0.6f, 0.35f, 0.2f, 1f);
    [ColorUsage(true, true)] public Color nightAmbient = new Color(0.05f, 0.07f, 0.15f, 1f);
    [ColorUsage(true, true)] public Color goldenHourAmbient = new Color(0.55f, 0.3f, 0.1f, 1f);

    [Header("Himmel - URP Volume")]
    public Volume skyVolume;
    // Nur URP-kompatible Volume-Komponenten:
    private ColorAdjustments colorAdjustments;
    private Bloom bloom;
    private Vignette vignette;

    [Header("Himmelfarben Gradient")]
    public Gradient skyColorDay;
    public Gradient skyHorizonDay;

    [Header("Schatten")]
    public float maxShadowStrength = 0.9f;

    // Events
    public event Action<float> OnHourChanged;
    public event Action OnSunrise;
    public event Action OnSunset;
    public event Action OnNightStart;
    public event Action OnDawnStart;

    private float previousHour;
    private bool isDaytime;
    private bool sunriseTriggered, sunsetTriggered;

    // Öffentliche Properties
    public float CurrentHour => currentHour;
    public bool IsDaytime => isDaytime;
    public float DayProgress => Mathf.InverseLerp(sunriseHour, sunsetHour, currentHour);
    public float NightProgress => isDaytime ? 0f : Mathf.InverseLerp(sunsetHour, sunriseHour + 24f,
        currentHour < sunriseHour ? currentHour + 24f : currentHour);

    private void Awake()
    {
        Instance = this;
        SetupDefaultGradients();
    }

    private void Start()
    {
        if (skyVolume != null && skyVolume.profile != null)
        {
            skyVolume.profile.TryGet(out colorAdjustments);
            skyVolume.profile.TryGet(out bloom);
            skyVolume.profile.TryGet(out vignette);
        }
        ApplyTimeOfDay();
    }

    private void Update()
    {
        if (!pauseTime)
        {
            // 1 Echtzeit-Sekunde = timeScale Spielminuten
            // timeScale=60 → 1 Spielstunde pro Echtminute
            currentHour += Time.deltaTime * timeScale / 3600f;
            if (currentHour >= 24f) currentHour -= 24f;
        }

        // Event triggers
        float prevH = previousHour;
        if (Mathf.Abs(currentHour - prevH) > 0.001f)
            OnHourChanged?.Invoke(currentHour);

        bool wasDay = isDaytime;
        isDaytime = currentHour >= sunriseHour && currentHour < sunsetHour;

        if (!wasDay && isDaytime) OnSunrise?.Invoke();
        if (wasDay && !isDaytime) OnSunset?.Invoke();

        previousHour = currentHour;
        ApplyTimeOfDay();
    }

    private void ApplyTimeOfDay()
    {
        UpdateSunPosition();
        UpdateMoonPosition();
        UpdateLightColors();
        UpdateAmbientLight();
        UpdatePostProcessing();
    }

    private void UpdateSunPosition()
    {
        if (sunLight == null && sunTransform == null) return;

        // Sonne bewegt sich von Ost (Aufgang 6h) über Süden (12h) nach West (Untergang 22h)
        // Gesamter Tag = sunsetHour - sunriseHour = 16h
        float dayLength = sunsetHour - sunriseHour;
        float dayT = (currentHour - sunriseHour) / dayLength; // 0=sunrise, 1=sunset

        // Pitch: -90° (unter Horizont morgens) → 0° (Horizont Aufgang) → 90° (Zenith Mittag) → 0° (Horizont Untergang) → -90°
        float pitch = Mathf.Sin(dayT * Mathf.PI) * 90f;

        // Yaw: Sonne geht im Osten auf (270° in Unity = Westen, also Osten = 90°) und geht im Westen unter
        float yaw = Mathf.Lerp(90f, 270f, dayT); // Ost → West

        Transform t = sunTransform != null ? sunTransform : sunLight.transform;
        t.rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (sunLight != null)
        {
            bool sunVisible = currentHour >= sunriseHour && currentHour <= sunsetHour;
            float altitude = Mathf.Sin(dayT * Mathf.PI);
            float intensityCurve = Mathf.Clamp01(altitude * 2f); // fades at horizon
            sunLight.intensity = sunVisible ? maxSunIntensity * intensityCurve : 0f;
            sunLight.shadows = altitude > 0.05f ? LightShadows.Soft : LightShadows.None;
            sunLight.shadowStrength = maxShadowStrength * Mathf.Clamp01(altitude * 3f);
        }
    }

    private void UpdateMoonPosition()
    {
        if (moonLight == null && moonTransform == null) return;

        // Mond: Nachts sichtbar, Gegenpol zur Sonne
        // Nacht: 22:00 → 06:00 = 8h
        float nightLength = 24f - (sunsetHour - sunriseHour); // z.B. 8h
        float nightT;
        if (currentHour >= sunsetHour)
            nightT = (currentHour - sunsetHour) / nightLength;
        else if (currentHour < sunriseHour)
            nightT = (currentHour + (24f - sunsetHour)) / nightLength;
        else
            nightT = 0f; // Tag

        float pitch = Mathf.Sin(nightT * Mathf.PI) * 80f;
        float yaw = Mathf.Lerp(70f, 260f, nightT);

        Transform t = moonTransform != null ? moonTransform : moonLight.transform;
        t.rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (moonLight != null)
        {
            bool isNight = currentHour < sunriseHour || currentHour >= sunsetHour;
            float moonAlt = Mathf.Sin(nightT * Mathf.PI);
            moonLight.intensity = isNight ? maxMoonIntensity * Mathf.Clamp01(moonAlt * 2f + 0.2f) : 0f;
        }
    }

    private void UpdateLightColors()
    {
        if (sunLight == null) return;

        float h = currentHour;
        Color targetColor;

        // Morgendämmerung 5-7h
        if (h >= 5f && h < 7f)
        {
            float t = Mathf.InverseLerp(5f, 7f, h);
            targetColor = Color.Lerp(sunriseSunsetColor, noonColor, t);
        }
        // Tag 7-19h
        else if (h >= 7f && h < 19f)
        {
            targetColor = noonColor;
        }
        // Golden Hour 19-21h
        else if (h >= 19f && h < 21f)
        {
            float t = Mathf.InverseLerp(19f, 21f, h);
            targetColor = Color.Lerp(noonColor, goldenHourColor, t);
        }
        // Sonnenuntergang 21-23h
        else if (h >= 21f && h < 23f)
        {
            float t = Mathf.InverseLerp(21f, 23f, h);
            targetColor = Color.Lerp(goldenHourColor, sunriseSunsetColor * 0.3f, t);
        }
        else
        {
            targetColor = noonColor * 0.01f; // Nacht
        }

        sunLight.color = Color.Lerp(sunLight.color, targetColor, Time.deltaTime * 2f);

        if (moonLight != null)
            moonLight.color = moonColor;
    }

    private void UpdateAmbientLight()
    {
        float h = currentHour;
        Color ambientTarget;

        if (h >= 5f && h < 7f)
            ambientTarget = Color.Lerp(nightAmbient, sunriseAmbient, Mathf.InverseLerp(5f, 7f, h));
        else if (h >= 7f && h < 18f)
            ambientTarget = Color.Lerp(sunriseAmbient, dayAmbient, Mathf.InverseLerp(7f, 10f, h));
        else if (h >= 18f && h < 21f)
            ambientTarget = Color.Lerp(dayAmbient, goldenHourAmbient, Mathf.InverseLerp(18f, 21f, h));
        else if (h >= 21f && h < 23f)
            ambientTarget = Color.Lerp(goldenHourAmbient, nightAmbient, Mathf.InverseLerp(21f, 23f, h));
        else
            ambientTarget = nightAmbient;

        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, ambientTarget, Time.deltaTime * 1.5f);
    }

    private void UpdatePostProcessing()
    {
        float h = currentHour;

        if (colorAdjustments != null)
        {
            // Golden Hour: warmer Ton, leicht erhöhte Sättigung
            if (h >= 18f && h < 22f)
            {
                float t = Mathf.Sin(Mathf.InverseLerp(18f, 22f, h) * Mathf.PI);
                colorAdjustments.colorFilter.value = Color.Lerp(Color.white, new Color(1.1f, 0.85f, 0.6f), t * 0.5f);
                colorAdjustments.saturation.value = Mathf.Lerp(0f, 15f, t);
            }
            else if (h >= 6f && h < 18f)
            {
                // Kräftiger Tag
                colorAdjustments.colorFilter.value = Color.Lerp(colorAdjustments.colorFilter.value, Color.white, Time.deltaTime);
                colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, 10f, Time.deltaTime);
            }
            else
            {
                // Nacht: entsättigt, bläulich
                colorAdjustments.colorFilter.value = Color.Lerp(colorAdjustments.colorFilter.value, new Color(0.6f, 0.65f, 0.9f), Time.deltaTime);
                colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, -20f, Time.deltaTime);
            }
        }

        // Bloom: Glühen bei Sonnenaufgang/untergang stärker
        if (bloom != null)
        {
            bool isGoldenHour = h >= 18f && h < 22f;
            bool isSunrise = h >= 5f && h < 7f;
            float targetBloom = (isGoldenHour || isSunrise) ? 0.5f : 0.2f;
            bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, targetBloom, Time.deltaTime);
        }
    }

    private void SetupDefaultGradients()
    {
        // Defaults werden im Editor gesetzt, hier nur Fallback-Init
    }

    // Öffentliche Methoden
    public void SetTime(float hour) => currentHour = Mathf.Clamp(hour, 0f, 24f);
    public void SetTimeScale(float scale) => timeScale = Mathf.Max(0.01f, scale);

    public string GetTimeString()
    {
        int h = (int)currentHour;
        int m = (int)((currentHour - h) * 60f);
        return $"{h:00}:{m:00}";
    }

    public float GetSunAltitude()
    {
        float dayLength = sunsetHour - sunriseHour;
        float dayT = (currentHour - sunriseHour) / dayLength;
        return Mathf.Sin(dayT * Mathf.PI);
    }
}

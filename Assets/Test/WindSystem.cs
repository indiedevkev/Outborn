using UnityEngine;
using System;

/// <summary>
/// WindSystem - Zentrales Wind-Steuerungssystem
/// Steuert alle anderen Wetter- und Partikel-Systeme
/// </summary>
public class WindSystem : MonoBehaviour
{
    public static WindSystem Instance { get; private set; }

    [Header("Wind Basis")]
    [Range(0f, 1f)] public float windStrength = 0.2f;      // 0=Stille, 1=Sturm
    [Range(0f, 360f)] public float windDirection = 45f;     // Grad
    public float windChangeSpeed = 0.05f;                   // Wie schnell ändert sich der Wind
    public bool randomizeWind = true;

    [Header("Wind Gust (Böen)")]
    public bool enableGusts = true;
    [Range(0f, 1f)] public float gustFrequency = 0.3f;
    [Range(0f, 1f)] public float gustStrength = 0.4f;      // Zusätzliche Stärke bei Böen
    private float currentGustIntensity = 0f;

    [Header("Wettertyp")]
    public WeatherType currentWeather = WeatherType.Clear;
    public float weatherTransitionSpeed = 0.5f;             // Sekunden pro Übergang

    [Header("Bewölkung")]
    [Range(0f, 1f)] public float cloudCoverage = 0.1f;

    [Header("Niederschlag")]
    [Range(0f, 1f)] public float precipitationIntensity = 0f;

    // Interne Werte (smooth interpoliert)
    private float _smoothWindStrength;
    private float _smoothWindDir;
    private float _noiseTime = 0f;
    private float _gustTimer = 0f;
    private float _gustCooldown = 0f;

    // Events
    public event Action<WeatherType> OnWeatherChanged;
    public event Action<float> OnGustStart;              // Böen-Intensität
    public event Action OnLightningStrike;

    // Properties für andere Systeme
    public Vector3 WindDirection3D => new Vector3(
        Mathf.Sin((_smoothWindDir + currentGustIntensity * 20f) * Mathf.Deg2Rad),
        0f,
        Mathf.Cos((_smoothWindDir + currentGustIntensity * 20f) * Mathf.Deg2Rad)
    );
    public float WindStrength => _smoothWindStrength + currentGustIntensity * gustStrength;
    public float GustIntensity => currentGustIntensity;
    public bool IsStormy => currentWeather == WeatherType.Storm;
    public bool IsRaining => currentWeather == WeatherType.Rain || currentWeather == WeatherType.Storm;
    public bool IsSnowing => currentWeather == WeatherType.Snow;
    public float CloudCoverage => cloudCoverage;

    private WeatherType _previousWeather;

    private void Awake()
    {
        Instance = this;
        _smoothWindStrength = windStrength;
        _smoothWindDir = windDirection;
    }

    private void Update()
    {
        UpdateWind();
        UpdateGusts();
        ApplyToShaders();
    }

    private void UpdateWind()
    {
        _noiseTime += Time.deltaTime * windChangeSpeed;

        if (randomizeWind)
        {
            // Perlin noise für natürliche Windänderungen
            float noiseX = Mathf.PerlinNoise(_noiseTime, 0f);
            float noiseY = Mathf.PerlinNoise(0f, _noiseTime + 100f);

            // Windstärke sanft variieren
            float targetStrength = windStrength + (noiseX - 0.5f) * 0.15f;
            targetStrength = Mathf.Clamp01(targetStrength);
            _smoothWindStrength = Mathf.Lerp(_smoothWindStrength, targetStrength, Time.deltaTime * 2f);

            // Windrichtung langsam ändern
            float targetDir = windDirection + (noiseY - 0.5f) * 30f;
            _smoothWindDir = Mathf.LerpAngle(_smoothWindDir, targetDir, Time.deltaTime * 0.5f);
        }
        else
        {
            _smoothWindStrength = Mathf.Lerp(_smoothWindStrength, windStrength, Time.deltaTime * 3f);
            _smoothWindDir = Mathf.LerpAngle(_smoothWindDir, windDirection, Time.deltaTime * 2f);
        }
    }

    private void UpdateGusts()
    {
        if (!enableGusts) 
        {
            currentGustIntensity = Mathf.Lerp(currentGustIntensity, 0f, Time.deltaTime * 3f);
            return;
        }

        _gustCooldown -= Time.deltaTime;

        if (_gustTimer > 0f)
        {
            _gustTimer -= Time.deltaTime;
            // Böe-Profil: schnell aufkommen, langsam abflauen
            float gustProfile = _gustTimer < 0.5f ?
                Mathf.InverseLerp(0f, 0.5f, _gustTimer) :
                1f;
            currentGustIntensity = Mathf.Lerp(currentGustIntensity, gustProfile, Time.deltaTime * 5f);
        }
        else
        {
            currentGustIntensity = Mathf.Lerp(currentGustIntensity, 0f, Time.deltaTime * 2f);

            // Neue Böe starten
            if (_gustCooldown <= 0f && _smoothWindStrength > 0.1f)
            {
                float baseChance = gustFrequency * _smoothWindStrength;
                if (UnityEngine.Random.value < baseChance * Time.deltaTime * 2f)
                {
                    StartGust();
                }
            }
        }
    }

    private void StartGust()
    {
        _gustTimer = UnityEngine.Random.Range(0.5f, 3f) * (1f + _smoothWindStrength);
        _gustCooldown = UnityEngine.Random.Range(3f, 15f);
        OnGustStart?.Invoke(currentGustIntensity);
    }

    private void ApplyToShaders()
    {
        // Globale Shader-Properties setzen für Vegetation etc.
        Shader.SetGlobalVector("_WindDirection", WindDirection3D);
        Shader.SetGlobalFloat("_WindStrength", WindStrength);
        Shader.SetGlobalFloat("_WindGustIntensity", currentGustIntensity);
        Shader.SetGlobalFloat("_Time2", Time.time); // Wind-Animations-Zeit
    }

    // === WETTER STEUERUNG ===
    public void SetWeather(WeatherType weather, float transitionDuration = -1f)
    {
        _previousWeather = currentWeather;
        currentWeather = weather;

        // Wind anpassen je nach Wetter
        switch (weather)
        {
            case WeatherType.Clear:
                windStrength = 0.1f;
                cloudCoverage = 0.05f;
                break;
            case WeatherType.Cloudy:
                windStrength = 0.2f;
                cloudCoverage = 0.6f;
                break;
            case WeatherType.Rain:
                windStrength = 0.35f;
                cloudCoverage = 0.85f;
                break;
            case WeatherType.Storm:
                windStrength = 0.8f;
                cloudCoverage = 1f;
                enableGusts = true;
                gustFrequency = 0.7f;
                break;
            case WeatherType.Snow:
                windStrength = 0.25f;
                cloudCoverage = 0.75f;
                break;
        }

        OnWeatherChanged?.Invoke(weather);
    }

    public void TriggerLightning() => OnLightningStrike?.Invoke();

    // Debug - Manuell eine Böe auslösen
    [ContextMenu("Trigger Gust")]
    public void TriggerGust() => StartGust();

    [ContextMenu("Set Storm")]
    public void SetStorm() => SetWeather(WeatherType.Storm);

    [ContextMenu("Set Clear")]
    public void SetClear() => SetWeather(WeatherType.Clear);
}

public enum WeatherType
{
    Clear,
    Cloudy,
    Rain,
    Storm,
    Snow
}

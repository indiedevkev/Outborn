using UnityEngine;

/// <summary>
/// SkyboxController - Steuert die Procedural Skybox Farben per Tageszeit
/// URP-kompatibel: kein Volume Override nötig, direkt aufs Skybox Material
/// 
/// Setup:
/// 1. Material erstellen: Shader → Skybox/Procedural
/// 2. Window → Rendering → Lighting → Skybox Material → zuweisen
/// 3. Dieses Script auf AtmosphereManager, skyboxMaterial zuweisen
/// </summary>
public class SkyboxController : MonoBehaviour
{
    [Header("Skybox Material")]
    [Tooltip("Das Skybox/Procedural Material aus dem Lighting Fenster")]
    public Material skyboxMaterial;

    [Header("Himmel Farben - Tag")]
    public Color skyTintDay = new Color(0.38f, 0.62f, 0.88f);
    public float exposureDay = 1.3f;
    public float atmosphereDay = 1.1f;

    [Header("Himmel Farben - Sunrise/Sunset")]
    public Color skyTintSunrise = new Color(0.6f, 0.35f, 0.2f);
    public float exposureSunrise = 1.0f;

    [Header("Himmel Farben - Golden Hour")]
    public Color skyTintGoldenHour = new Color(0.7f, 0.42f, 0.15f);
    public float exposureGoldenHour = 1.1f;

    [Header("Himmel Farben - Nacht")]
    public Color skyTintNight = new Color(0.03f, 0.04f, 0.1f);
    public float exposureNight = 0.05f;
    public float atmosphereNight = 0.6f;

    [Header("Horizont Rotstich nach Sonnenuntergang")]
    [Tooltip("Der rote/orange Schimmer am Horizont nach Sonnenuntergang")]
    public Color horizonGlowColor = new Color(0.8f, 0.25f, 0.05f);
    public float horizonGlowDuration = 1.5f; // Stunden nach Sonnenuntergang

    [Header("Sonne im Shader")]
    public float sunSizeDay = 0.04f;
    public float sunSizeNight = 0.0f;

    // Shader Property IDs (gecacht für Performance)
    private static readonly int _SkyTint = Shader.PropertyToID("_SkyTint");
    private static readonly int _Exposure = Shader.PropertyToID("_Exposure");
    private static readonly int _AtmosphereThickness = Shader.PropertyToID("_AtmosphereThickness");
    private static readonly int _SunSize = Shader.PropertyToID("_SunSize");
    private static readonly int _GroundColor = Shader.PropertyToID("_GroundColor");

    private Color currentSkyTint;
    private float currentExposure;
    private float currentAtmosphere;

    private void Start()
    {
        if (skyboxMaterial == null)
        {
            // Automatisch die aktuelle Skybox holen
            skyboxMaterial = RenderSettings.skybox;
        }

        if (skyboxMaterial == null)
        {
            Debug.LogWarning("SkyboxController: Kein Skybox Material zugewiesen und keins in RenderSettings gefunden!\n" +
                             "→ Window → Rendering → Lighting → Skybox Material setzen");
            return;
        }

        if (!skyboxMaterial.shader.name.Contains("Procedural") && !skyboxMaterial.shader.name.Contains("Skybox"))
        {
            Debug.LogWarning($"SkyboxController: Material hat Shader '{skyboxMaterial.shader.name}'. " +
                             "Erwartet wird 'Skybox/Procedural'.");
        }

        // Startwerte
        currentSkyTint = skyTintDay;
        currentExposure = exposureDay;
        currentAtmosphere = atmosphereDay;
        ApplyToMaterial();
    }

    private void Update()
    {
        if (skyboxMaterial == null || DayNightSystem.Instance == null) return;

        float h = DayNightSystem.Instance.CurrentHour;
        float sunriseH = DayNightSystem.Instance.sunriseHour;
        float sunsetH = DayNightSystem.Instance.sunsetHour;

        Color targetTint;
        float targetExposure;
        float targetAtmosphere;

        // Nacht (komplett dunkel)
        if (h < sunriseH - 1f || h > sunsetH + horizonGlowDuration)
        {
            targetTint = skyTintNight;
            targetExposure = exposureNight;
            targetAtmosphere = atmosphereNight;
        }
        // Morgendämmerung (1h vor Sunrise bis Sunrise)
        else if (h >= sunriseH - 1f && h < sunriseH)
        {
            float t = Mathf.InverseLerp(sunriseH - 1f, sunriseH, h);
            targetTint = Color.Lerp(skyTintNight, skyTintSunrise, t);
            targetExposure = Mathf.Lerp(exposureNight, exposureSunrise, t);
            targetAtmosphere = Mathf.Lerp(atmosphereNight, atmosphereDay, t);
        }
        // Sonnenaufgang (Sunrise bis Sunrise+1.5h)
        else if (h >= sunriseH && h < sunriseH + 1.5f)
        {
            float t = Mathf.InverseLerp(sunriseH, sunriseH + 1.5f, h);
            targetTint = Color.Lerp(skyTintSunrise, skyTintDay, t);
            targetExposure = Mathf.Lerp(exposureSunrise, exposureDay, t);
            targetAtmosphere = atmosphereDay;
        }
        // Tag
        else if (h >= sunriseH + 1.5f && h < sunsetH - 2f)
        {
            targetTint = skyTintDay;
            targetExposure = exposureDay;
            targetAtmosphere = atmosphereDay;
        }
        // Golden Hour (2h vor Sunset)
        else if (h >= sunsetH - 2f && h < sunsetH)
        {
            float t = Mathf.InverseLerp(sunsetH - 2f, sunsetH, h);
            targetTint = Color.Lerp(skyTintDay, skyTintGoldenHour, t);
            targetExposure = Mathf.Lerp(exposureDay, exposureGoldenHour, t);
            targetAtmosphere = atmosphereDay;
        }
        // Sonnenuntergang + Rotstich am Horizont
        else if (h >= sunsetH && h <= sunsetH + horizonGlowDuration)
        {
            float t = Mathf.InverseLerp(sunsetH, sunsetH + horizonGlowDuration, h);
            targetTint = Color.Lerp(horizonGlowColor, skyTintNight, t);
            targetExposure = Mathf.Lerp(exposureGoldenHour * 0.6f, exposureNight, t);
            targetAtmosphere = Mathf.Lerp(atmosphereDay, atmosphereNight, t);
        }
        else
        {
            targetTint = skyTintNight;
            targetExposure = exposureNight;
            targetAtmosphere = atmosphereNight;
        }

        // Smooth interpolieren
        float speed = Time.deltaTime * 1.5f;
        currentSkyTint = Color.Lerp(currentSkyTint, targetTint, speed);
        currentExposure = Mathf.Lerp(currentExposure, targetExposure, speed);
        currentAtmosphere = Mathf.Lerp(currentAtmosphere, targetAtmosphere, speed);

        ApplyToMaterial();

        // Ambient Light ebenfalls anpassen (Skybox-basiert)
        DynamicGI.UpdateEnvironment();
    }

    private void ApplyToMaterial()
    {
        if (skyboxMaterial == null) return;
        skyboxMaterial.SetColor(_SkyTint, currentSkyTint);
        skyboxMaterial.SetFloat(_Exposure, currentExposure);
        skyboxMaterial.SetFloat(_AtmosphereThickness, currentAtmosphere);

        // Sonne nur tagsüber sichtbar
        bool isDay = DayNightSystem.Instance != null && DayNightSystem.Instance.IsDaytime;
        skyboxMaterial.SetFloat(_SunSize, isDay ? sunSizeDay : sunSizeNight);
    }

    // Im Editor live vorschauen
    private void OnValidate()
    {
        if (skyboxMaterial != null && Application.isPlaying)
            ApplyToMaterial();
    }
}

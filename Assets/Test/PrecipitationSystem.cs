using UnityEngine;
using System.Collections;

/// <summary>
/// PrecipitationSystem - Regen, Schnee, Blitze
/// Regen mit Boden-Splashes, Sturm mit Blitzen
/// </summary>
public class PrecipitationSystem : MonoBehaviour
{
    [Header("Regen")]
    public bool enableRain = true;
    public ParticleSystem rainParticles;
    public ParticleSystem rainSplashParticles;    // Boden-Splashes
    [Range(0f, 5000f)] public float maxRainRate = 3000f;
    public Color rainColor = new Color(0.6f, 0.7f, 0.9f, 0.6f);

    [Header("Schnee")]
    public bool enableSnow = true;
    public ParticleSystem snowParticles;
    [Range(0f, 2000f)] public float maxSnowRate = 500f;
    public Color snowColor = new Color(0.95f, 0.97f, 1f, 0.9f);

    [Header("Blitze")]
    public bool enableLightning = true;
    public Light lightningLight;
    public float lightningMinInterval = 5f;
    public float lightningMaxInterval = 20f;
    public float lightningFlashDuration = 0.15f;
    [ColorUsage(true, true)] public Color lightningColor = new Color(0.7f, 0.8f, 1f, 1f);
    public float lightningIntensity = 15f;

    [Header("Audio (Optional)")]
    public AudioSource rainAudio;
    public AudioSource thunderAudio;
    public AudioClip[] thunderClips;

    [Header("Fog bei Regen/Schnee")]
    public bool adjustFog = true;
    public float rainFogDensity = 0.04f;
    public float snowFogDensity = 0.02f;
    public Color rainFogColor = new Color(0.5f, 0.55f, 0.6f);
    public Color snowFogColor = new Color(0.85f, 0.88f, 0.92f);

    private float currentPrecipitation = 0f;
    private float lightningTimer = 0f;
    private float nextLightningTime = 10f;
    private bool isLightningFlashing = false;
    private float originalFogDensity;
    private Color originalFogColor;

    private void Start()
    {
        originalFogDensity = RenderSettings.fogDensity;
        originalFogColor = RenderSettings.fogColor;

        SetupParticleSystems();

        if (WindSystem.Instance != null)
        {
            WindSystem.Instance.OnWeatherChanged += HandleWeatherChange;
            WindSystem.Instance.OnLightningStrike += TriggerLightning;
        }
    }

    private void SetupParticleSystems()
    {
        // Regen erstellen falls nicht zugewiesen
        if (rainParticles == null)
            rainParticles = CreateRainSystem();

        if (rainSplashParticles == null)
            rainSplashParticles = CreateSplashSystem();

        if (snowParticles == null)
            snowParticles = CreateSnowSystem();

        if (lightningLight == null)
        {
            var lgo = new GameObject("LightningLight");
            lgo.transform.SetParent(transform);
            lightningLight = lgo.AddComponent<Light>();
            lightningLight.type = LightType.Directional;
            lightningLight.color = lightningColor;
            lightningLight.intensity = 0f;
            lightningLight.shadows = LightShadows.None;
        }
    }

    private ParticleSystem CreateRainSystem()
    {
        GameObject go = new GameObject("RainPS");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 20f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = 10000;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
        main.startColor = rainColor;
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(60f, 1f, 60f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.1f;
        renderer.lengthScale = 2f;
        renderer.material = CreateParticleMaterial(rainColor);

        return ps;
    }

    private ParticleSystem CreateSplashSystem()
    {
        GameObject go = new GameObject("RainSplashPS");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 0.05f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = 3000;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new Color(rainColor.r, rainColor.g, rainColor.b, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(60f, 0.1f, 60f);

        var velOverLife = ps.velocityOverLifetime;
        velOverLife.enabled = true;
        velOverLife.y = new ParticleSystem.MinMaxCurve(0f, 2f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0f); sc.AddKey(0.1f, 1f); sc.AddKey(1f, 0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial(rainColor);

        return ps;
    }

    private ParticleSystem CreateSnowSystem()
    {
        GameObject go = new GameObject("SnowPS");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 20f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = 5000;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = snowColor;
        main.gravityModifier = 0.3f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(60f, 1f, 60f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.2f;

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.z = new ParticleSystem.MinMaxCurve(-30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial(snowColor);

        return ps;
    }

    private Material CreateParticleMaterial(Color col)
    {
        return AtmosphereSetup.CreateParticleMaterial(col, false);
    }

    private void Update()
    {
        if (WindSystem.Instance == null) return;

        var wind = WindSystem.Instance;
        bool isRaining = wind.IsRaining;
        bool isSnowing = wind.IsSnowing;
        float intensity = wind.WindStrength;

        currentPrecipitation = Mathf.Lerp(currentPrecipitation,
            (isRaining || isSnowing) ? intensity : 0f,
            Time.deltaTime * 0.5f);

        UpdateRain(isRaining, intensity);
        UpdateSnow(isSnowing, intensity);
        UpdateFog(isRaining, isSnowing);
        UpdateWindEffect();

        if (enableLightning && wind.IsStormy)
            UpdateLightning();
    }

    private void UpdateRain(bool active, float intensity)
    {
        if (!rainParticles || !rainSplashParticles) return;

        var em = rainParticles.emission;
        var sem = rainSplashParticles.emission;
        float rate = active ? maxRainRate * intensity : 0f;
        em.rateOverTime = Mathf.Lerp(em.rateOverTime.constant, rate, Time.deltaTime * 2f);
        sem.rateOverTime = em.rateOverTime.constant * 0.3f;

        if (rainAudio != null)
        {
            rainAudio.volume = Mathf.Lerp(rainAudio.volume, active ? intensity * 0.7f : 0f, Time.deltaTime);
        }
    }

    private void UpdateSnow(bool active, float intensity)
    {
        if (!snowParticles) return;
        var em = snowParticles.emission;
        float rate = active ? maxSnowRate * intensity : 0f;
        em.rateOverTime = Mathf.Lerp(em.rateOverTime.constant, rate, Time.deltaTime * 2f);
    }

    private void UpdateWindEffect()
    {
        if (WindSystem.Instance == null) return;
        // Regen schräg stellen je nach Wind
        Vector3 windDir = WindSystem.Instance.WindDirection3D;
        float windStr = WindSystem.Instance.WindStrength;

        if (rainParticles != null)
        {
            var velOverLife = rainParticles.velocityOverLifetime;
            velOverLife.x = windDir.x * windStr * 5f;
            velOverLife.z = windDir.z * windStr * 5f;
        }
    }

    private void UpdateFog(bool isRaining, bool isSnowing)
    {
        if (!adjustFog) return;

        if (isRaining)
        {
            float intensity = WindSystem.Instance.WindStrength;
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, rainFogDensity * intensity, Time.deltaTime * 0.3f);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, rainFogColor, Time.deltaTime * 0.3f);
        }
        else if (isSnowing)
        {
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, snowFogDensity, Time.deltaTime * 0.3f);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, snowFogColor, Time.deltaTime * 0.3f);
        }
        else
        {
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, originalFogDensity, Time.deltaTime * 0.2f);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, originalFogColor, Time.deltaTime * 0.2f);
        }
    }

    private void UpdateLightning()
    {
        lightningTimer += Time.deltaTime;
        if (lightningTimer >= nextLightningTime && !isLightningFlashing)
        {
            lightningTimer = 0f;
            nextLightningTime = Random.Range(lightningMinInterval, lightningMaxInterval);
            StartCoroutine(LightningFlash());
        }
    }

    private IEnumerator LightningFlash()
    {
        isLightningFlashing = true;
        int flashes = Random.Range(1, 4);

        for (int f = 0; f < flashes; f++)
        {
            lightningLight.intensity = lightningIntensity * Random.Range(0.5f, 1f);
            lightningLight.color = lightningColor;

            if (thunderAudio != null && thunderClips != null && thunderClips.Length > 0)
            {
                float delay = Random.Range(0.5f, 3f); // Schall kommt später
                float dist = Random.Range(100f, 3000f);
                StartCoroutine(PlayThunderDelayed(Mathf.Clamp(delay * dist / 1000f, 0.1f, 5f)));
            }

            yield return new WaitForSeconds(lightningFlashDuration * Random.Range(0.5f, 1.5f));
            lightningLight.intensity = 0f;

            if (f < flashes - 1)
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }

        isLightningFlashing = false;
        WindSystem.Instance?.TriggerLightning();
    }

    private IEnumerator PlayThunderDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (thunderAudio != null && thunderClips != null && thunderClips.Length > 0)
        {
            thunderAudio.PlayOneShot(thunderClips[Random.Range(0, thunderClips.Length)]);
        }
    }

    public void TriggerLightning() => StartCoroutine(LightningFlash());

    private void HandleWeatherChange(WeatherType weather)
    {
        // Wetter-spezifische Anpassungen
    }

    private void OnDestroy()
    {
        if (WindSystem.Instance != null)
        {
            WindSystem.Instance.OnWeatherChanged -= HandleWeatherChange;
            WindSystem.Instance.OnLightningStrike -= TriggerLightning;
        }
    }
}

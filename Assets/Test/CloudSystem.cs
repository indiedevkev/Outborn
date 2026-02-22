using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// CloudSystem - Dynamische Wolken mit Wind-Bewegung
/// Nutzt Particle Systems für volumetrische Wolken-Optik
/// </summary>
public class CloudSystem : MonoBehaviour
{
    [Header("Wolken Schichten")]
    public bool enableHighClouds = true;    // Cirrus/Schleierwolken
    public bool enableMidClouds = true;     // Cumulus/Haufenwolken  
    public bool enableLowClouds = true;     // Stratus/Schichtwolken

    [Header("Hohe Wolken (Cirrus)")]
    public int highCloudCount = 15;
    public float highCloudHeight = 120f;
    public float highCloudSpread = 300f;
    [Range(0f, 1f)] public float highCloudOpacity = 0.4f;

    [Header("Mittlere Wolken (Cumulus)")]
    public int midCloudCount = 12;
    public float midCloudHeight = 70f;
    public float midCloudSpread = 250f;
    [Range(0f, 1f)] public float midCloudOpacity = 0.7f;

    [Header("Niedrige Wolken (Stratus)")]
    public int lowCloudCount = 8;
    public float lowCloudHeight = 35f;
    public float lowCloudSpread = 200f;
    [Range(0f, 1f)] public float lowCloudOpacity = 0.85f;

    [Header("Wolken Farben")]
    public Color cloudColorDay = new Color(1f, 0.98f, 0.95f);
    public Color cloudColorSunrise = new Color(1f, 0.6f, 0.3f);
    public Color cloudColorSunset = new Color(0.9f, 0.4f, 0.2f);
    public Color cloudColorNight = new Color(0.2f, 0.22f, 0.3f);
    public Color cloudColorStorm = new Color(0.3f, 0.3f, 0.35f);

    [Header("Bewegung")]
    public float cloudScrollSpeed = 0.5f;
    public float turbulenceAmount = 2f;

    private ParticleSystem highCloudPS;
    private ParticleSystem midCloudPS;
    private ParticleSystem lowCloudPS;
    private ParticleSystem.Particle[] highParticles;
    private ParticleSystem.Particle[] midParticles;
    private ParticleSystem.Particle[] lowParticles;
    private Vector2[] cloudNoiseOffsets;
    private float[] cloudNoiseSeeds;
    private Material cloudMaterial;

    private float currentCoverage = 0f;
    private Color currentCloudColor;

    private void Start()
    {
        cloudMaterial = CreateCloudMaterial();
        CreateCloudLayers();
        InitializeClouds();
        currentCloudColor = cloudColorDay;
    }

    private Material CreateCloudMaterial()
    {
        return AtmosphereSetup.CreateParticleMaterial(cloudColorDay, false);
    }

    private ParticleSystem CreateCloudLayer(string name, int count, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, height, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = count + 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = float.MaxValue;
        main.startSize = 60f;

        var emission = ps.emission;
        emission.enabled = false;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        renderer.material = AtmosphereSetup.CreateParticleMaterial(cloudColorDay, false);
        renderer.sortingOrder = -50;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return ps;
    }

    private void CreateCloudLayers()
    {
        highCloudPS = CreateCloudLayer("HighClouds", highCloudCount, highCloudHeight);
        midCloudPS = CreateCloudLayer("MidClouds", midCloudCount, midCloudHeight);
        lowCloudPS = CreateCloudLayer("LowClouds", lowCloudCount, lowCloudHeight);
    }

    private void InitializeClouds()
    {
        int totalClouds = highCloudCount + midCloudCount + lowCloudCount;
        cloudNoiseOffsets = new Vector2[totalClouds];
        cloudNoiseSeeds = new float[totalClouds];
        for (int i = 0; i < totalClouds; i++)
        {
            cloudNoiseOffsets[i] = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f));
            cloudNoiseSeeds[i] = Random.Range(0f, 1000f);
        }

        highParticles = GenerateCloudParticles(highCloudCount, highCloudHeight, highCloudSpread, 80f, 0.3f, 0);
        midParticles = GenerateCloudParticles(midCloudCount, midCloudHeight, midCloudSpread, 50f, 0.5f, highCloudCount);
        lowParticles = GenerateCloudParticles(lowCloudCount, lowCloudHeight, lowCloudSpread, 35f, 0.7f, highCloudCount + midCloudCount);

        highCloudPS.SetParticles(highParticles, highCloudCount);
        midCloudPS.SetParticles(midParticles, midCloudCount);
        lowCloudPS.SetParticles(lowParticles, lowCloudCount);
    }

    private ParticleSystem.Particle[] GenerateCloudParticles(int count, float height, float spread, float baseSize, float opacity, int seedOffset)
    {
        var particles = new ParticleSystem.Particle[count];
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(0f, spread);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, height + Random.Range(-5f, 5f), Mathf.Sin(angle) * dist);

            particles[i] = new ParticleSystem.Particle
            {
                position = pos,
                startSize = baseSize * Random.Range(0.6f, 2f),
                startColor = cloudColorDay * new Color(1f, 1f, 1f, opacity),
                remainingLifetime = float.MaxValue,
                startLifetime = float.MaxValue,
                rotation = Random.Range(0f, 360f),
                velocity = Vector3.zero
            };
        }
        return particles;
    }

    private void Update()
    {
        float targetCoverage = WindSystem.Instance != null ? WindSystem.Instance.CloudCoverage : currentCoverage;
        currentCoverage = Mathf.Lerp(currentCoverage, targetCoverage, Time.deltaTime * 0.3f);

        UpdateCloudColors();
        MoveCloudLayers();
        UpdateCloudVisibility();
    }

    private void UpdateCloudColors()
    {
        float hour = DayNightSystem.Instance != null ? DayNightSystem.Instance.CurrentHour : 12f;
        Color targetColor;

        if (WindSystem.Instance != null && WindSystem.Instance.IsStormy)
            targetColor = cloudColorStorm;
        else if (hour >= 5f && hour < 7f)
            targetColor = Color.Lerp(cloudColorSunrise, cloudColorDay, Mathf.InverseLerp(5f, 7f, hour));
        else if (hour >= 7f && hour < 18f)
            targetColor = cloudColorDay;
        else if (hour >= 18f && hour < 21f)
            targetColor = Color.Lerp(cloudColorDay, cloudColorSunset, Mathf.InverseLerp(18f, 21f, hour));
        else if (hour >= 21f && hour < 22f)
            targetColor = Color.Lerp(cloudColorSunset, cloudColorNight, Mathf.InverseLerp(21f, 22f, hour));
        else
            targetColor = cloudColorNight;

        currentCloudColor = Color.Lerp(currentCloudColor, targetColor, Time.deltaTime * 0.5f);
    }

    private void MoveCloudLayers()
    {
        if (WindSystem.Instance == null) return;

        Vector3 windDir = WindSystem.Instance.WindDirection3D;
        float speed = WindSystem.Instance.WindStrength * cloudScrollSpeed;

        // Verschiedene Schichten bewegen sich mit verschiedener Geschwindigkeit
        float dt = Time.deltaTime;

        ApplyCloudMovement(highParticles, highCloudCount, highCloudPS, windDir, speed * 1.5f, highCloudSpread, highCloudHeight);
        ApplyCloudMovement(midParticles, midCloudCount, midCloudPS, windDir, speed, midCloudSpread, midCloudHeight);
        ApplyCloudMovement(lowParticles, lowCloudCount, lowCloudPS, windDir, speed * 0.7f, lowCloudSpread, lowCloudHeight);
    }

    private void ApplyCloudMovement(ParticleSystem.Particle[] particles, int count, ParticleSystem ps, Vector3 windDir, float speed, float spread, float height)
    {
        if (particles == null) return;
        for (int i = 0; i < count; i++)
        {
            particles[i].position += windDir * speed * Time.deltaTime;

            // Wolken die zu weit weg sind, auf die andere Seite teleportieren
            Vector3 pos = particles[i].position;
            if (Mathf.Abs(pos.x) > spread * 1.2f) pos.x = -Mathf.Sign(pos.x) * spread;
            if (Mathf.Abs(pos.z) > spread * 1.2f) pos.z = -Mathf.Sign(pos.z) * spread;
            particles[i].position = pos;

            // Farbe aktualisieren
            Color c = currentCloudColor;
            particles[i].startColor = c;
        }
        ps.SetParticles(particles, count);
    }

    private void UpdateCloudVisibility()
    {
        highCloudPS.gameObject.SetActive(enableHighClouds && currentCoverage > 0.1f);
        midCloudPS.gameObject.SetActive(enableMidClouds && currentCoverage > 0.3f);
        lowCloudPS.gameObject.SetActive(enableLowClouds && currentCoverage > 0.6f);
    }
}

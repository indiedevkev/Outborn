using UnityEngine;

/// <summary>
/// StarSystem - Generiert prozedural Sterne am Nachthimmel
/// Nutzt ein Particle System das sich mit Tageszeit ein/ausblendet
/// </summary>
public class StarSystem : MonoBehaviour
{
    [Header("Sterne")]
    public int starCount = 2000;
    public float skyRadius = 900f;
    [Range(0f, 1f)] public float starSize = 0.5f;
    public float starSizeVariance = 0.3f;
    public Color starColorCold = new Color(0.7f, 0.8f, 1f);
    public Color starColorWarm = new Color(1f, 0.9f, 0.7f);
    [Range(0f, 1f)] public float twinkleSpeed = 0.5f;
    public float twinkleIntensity = 0.3f;

    [Header("Milchstraße")]
    public bool showMilkyWay = true;
    public int milkyWayStars = 1500;
    [Range(0f, 360f)] public float milkyWayRotation = 45f;
    [Range(0f, 90f)] public float milkyWayTilt = 20f;
    public Color milkyWayColor = new Color(0.3f, 0.35f, 0.5f, 0.4f);

    [Header("Sichtbarkeit")]
    public float fadeStartHour = 20f;   // Sterne beginnen zu erscheinen
    public float fadeEndHour = 22f;     // Vollständig sichtbar
    public float fadeOutStartHour = 5f; // Beginnen zu verschwinden
    public float fadeOutEndHour = 7f;   // Verschwunden

    private ParticleSystem starPS;
    private ParticleSystem milkyWayPS;
    private ParticleSystem.Particle[] stars;
    private ParticleSystem.Particle[] milkyWayParticles;
    private float[] twinkleOffsets;
    private float currentAlpha = 0f;

    private void Start()
    {
        CreateStarParticleSystem();
        if (showMilkyWay) CreateMilkyWaySystem();
        GenerateStars();
    }

    private void CreateStarParticleSystem()
    {
        GameObject psGO = new GameObject("Stars_PS");
        psGO.transform.SetParent(transform);
        psGO.transform.localPosition = Vector3.zero;

        starPS = psGO.AddComponent<ParticleSystem>();
        var main = starPS.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = starCount + 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = float.MaxValue;
        main.startSize = starSize;
        main.startColor = Color.white;

        var emission = starPS.emission;
        emission.enabled = false;

        var renderer = starPS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // Nutze Standard URP Unlit Particle Material
        renderer.material = CreateStarMaterial();
        renderer.sortingOrder = -100;
    }

    private void CreateMilkyWaySystem()
    {
        GameObject psGO = new GameObject("MilkyWay_PS");
        psGO.transform.SetParent(transform);
        psGO.transform.localPosition = Vector3.zero;
        psGO.transform.rotation = Quaternion.Euler(milkyWayTilt, milkyWayRotation, 0f);

        milkyWayPS = psGO.AddComponent<ParticleSystem>();
        var main = milkyWayPS.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = milkyWayStars + 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = float.MaxValue;
        main.startSize = starSize * 0.6f;

        var emission = milkyWayPS.emission;
        emission.enabled = false;

        var renderer = milkyWayPS.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateStarMaterial();
    }

    private Material CreateStarMaterial()
    {
        return AtmosphereSetup.CreateParticleMaterial(Color.white, true);
    }

    private void GenerateStars()
    {
        stars = new ParticleSystem.Particle[starCount];
        twinkleOffsets = new float[starCount];

        for (int i = 0; i < starCount; i++)
        {
            // Gleichmäßig auf Hemisphäre verteilen (nur obere Hälfte)
            float theta = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float phi = Mathf.Acos(UnityEngine.Random.Range(-0.1f, 1f)); // 0 = Zenith

            Vector3 pos = new Vector3(
                Mathf.Sin(phi) * Mathf.Cos(theta),
                Mathf.Cos(phi),
                Mathf.Sin(phi) * Mathf.Sin(theta)
            ) * skyRadius;

            float sizeMult = UnityEngine.Random.value < 0.05f ? 3f : // Sterne großer
                             UnityEngine.Random.value < 0.2f ? 1.5f :
                             UnityEngine.Random.value;

            stars[i] = new ParticleSystem.Particle
            {
                position = pos,
                startSize = (starSize + UnityEngine.Random.Range(-starSizeVariance, starSizeVariance)) * sizeMult,
                startColor = Color.Lerp(starColorCold, starColorWarm, UnityEngine.Random.value),
                remainingLifetime = float.MaxValue,
                startLifetime = float.MaxValue,
                velocity = Vector3.zero
            };

            twinkleOffsets[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        starPS.SetParticles(stars, starCount);

        if (showMilkyWay && milkyWayPS != null)
            GenerateMilkyWay();
    }

    private void GenerateMilkyWay()
    {
        milkyWayParticles = new ParticleSystem.Particle[milkyWayStars];

        for (int i = 0; i < milkyWayStars; i++)
        {
            // Band entlang der Galaxie-Ebene
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float bandSpread = UnityEngine.Random.Range(-0.15f, 0.15f); // Breite des Bandes
            float density = Mathf.Exp(-Mathf.Abs(bandSpread) * 8f); // Dicker in der Mitte

            if (UnityEngine.Random.value > density * 0.8f)
            {
                i--;
                continue;
            }

            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * skyRadius * 0.98f,
                bandSpread * skyRadius,
                Mathf.Sin(angle) * skyRadius * 0.98f
            );

            milkyWayParticles[i] = new ParticleSystem.Particle
            {
                position = pos,
                startSize = starSize * UnityEngine.Random.Range(0.3f, 1f),
                startColor = milkyWayColor * UnityEngine.Random.Range(0.5f, 1.5f),
                remainingLifetime = float.MaxValue,
                startLifetime = float.MaxValue,
                velocity = Vector3.zero
            };
        }
        milkyWayPS.SetParticles(milkyWayParticles, milkyWayStars);
    }

    private void Update()
    {
        UpdateVisibility();
        UpdateTwinkle();
        RotateWithEarth();
    }

    private void UpdateVisibility()
    {
        float h = DayNightSystem.Instance != null ? DayNightSystem.Instance.CurrentHour : 22f;
        float targetAlpha = 0f;

        if (h >= fadeStartHour && h < fadeEndHour)
            targetAlpha = Mathf.InverseLerp(fadeStartHour, fadeEndHour, h);
        else if (h >= fadeEndHour || h < fadeOutStartHour)
            targetAlpha = 1f;
        else if (h >= fadeOutStartHour && h < fadeOutEndHour)
            targetAlpha = 1f - Mathf.InverseLerp(fadeOutStartHour, fadeOutEndHour, h);

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 0.5f);
    }

    private void UpdateTwinkle()
    {
        if (currentAlpha < 0.01f) return;

        for (int i = 0; i < Mathf.Min(starCount, stars.Length); i++)
        {
            float twinkle = 1f + Mathf.Sin(Time.time * twinkleSpeed * 3f + twinkleOffsets[i]) * twinkleIntensity;
            Color c = stars[i].startColor;
            c.a = currentAlpha * twinkle;
            stars[i].startColor = c;
        }
        starPS.SetParticles(stars, starCount);

        if (milkyWayPS != null && milkyWayParticles != null)
        {
            for (int i = 0; i < milkyWayStars; i++)
            {
                Color c = milkyWayParticles[i].startColor;
                c.a = currentAlpha * milkyWayColor.a;
                milkyWayParticles[i].startColor = c;
            }
            milkyWayPS.SetParticles(milkyWayParticles, milkyWayStars);
        }
    }

    private void RotateWithEarth()
    {
        // Sterne rotieren langsam über Nacht (Erdrotation)
        if (!DayNightSystem.Instance) return;
        float h = DayNightSystem.Instance.CurrentHour;
        transform.rotation = Quaternion.Euler(0f, h * 15f, 0f); // 360° / 24h = 15°/h
    }
}

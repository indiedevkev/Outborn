using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AmbientParticleSystem - Alle Ambient-Partikel:
/// - Staub/Sporen (Tag)
/// - Glühwürmchen (Nacht)  
/// - Fallende Blätter
/// - Wind-Gust-Lines (Böen)
/// - Pollen (Sommer-Feeling)
/// </summary>
public class AmbientParticleSystem : MonoBehaviour
{
    [Header("Staub & Sporen")]
    public bool enableDust = true;
    [Range(0f, 200f)] public float dustCount = 50f;
    public Color dustColor = new Color(0.9f, 0.85f, 0.7f, 0.15f);
    public float dustHeight = 1f;
    public float dustSpread = 20f;
    public float dustSize = 0.03f;

    [Header("Pollen")]
    public bool enablePollen = true;
    [Range(0f, 100f)] public float pollenCount = 20f;
    public Color pollenColor = new Color(1f, 0.95f, 0.3f, 0.5f);
    public float pollenActiveStartHour = 9f;
    public float pollenActiveEndHour = 18f;

    [Header("Glühwürmchen")]
    public bool enableFireflies = true;
    [Range(0, 80)] public int fireflyCount = 30;
    public Color fireflyColor = new Color(0.4f, 1f, 0.2f, 1f);
    [ColorUsage(true, true)] public Color fireflyGlowColor = new Color(0.4f, 1f, 0.2f, 1f);
    public float fireflyHeight = 0.5f;
    public float fireflyMaxHeight = 2.5f;
    public float fireflySpread = 15f;
    public float fireflyBlinkSpeed = 2f;
    public float fireflyMoveSpeed = 0.5f;
    public float fireflyActiveStartHour = 20f;
    public float fireflyActiveEndHour = 5f;

    [Header("Fallende Blätter")]
    public bool enableFallingLeaves = true;
    [Range(0f, 5f)] public float leafEmissionRate = 2f;
    public Color[] leafColors = {
        new Color(0.8f, 0.3f, 0.05f),
        new Color(0.9f, 0.6f, 0.05f),
        new Color(0.7f, 0.4f, 0.1f),
        new Color(0.5f, 0.7f, 0.1f)
    };
    public float leafSpawnHeight = 8f;
    public float leafSpread = 25f;
    public float leafFallSpeed = 2f;
    public float leafSize = 0.15f;

    [Header("Wind Gust Lines")]
    public bool enableGustLines = true;
    [Range(0, 30)] public int maxGustLines = 15;
    public Color gustLineColor = new Color(0.8f, 0.85f, 0.9f, 0.3f);
    public float gustLineLength = 3f;
    public float gustLineWidth = 0.02f;
    public float gustLineSpeed = 8f;
    public float gustLineHeight = 0.5f;
    public float gustLineMaxHeight = 4f;

    // Particle Systems
    private ParticleSystem dustPS;
    private ParticleSystem pollenPS;
    private ParticleSystem fireflyPS;
    private ParticleSystem leafPS;

    // Glühwürmchen-Daten
    private ParticleSystem.Particle[] fireflyParticles;
    private Vector3[] fireflyPositions;
    private Vector3[] fireflyTargets;
    private float[] fireflyBlinkOffsets;
    private float[] fireflyBlinkPhases;
    private float currentFireflyAlpha = 0f;

    // Gust Lines
    private List<GustLine> activeGustLines = new List<GustLine>();
    private Material gustLineMaterial;
    private float nextGustLineTime = 0f;

    private class GustLine
    {
        public Vector3 position;
        public Vector3 velocity;
        public float lifetime;
        public float maxLifetime;
        public float height;
        public LineRenderer lineRenderer;
    }

    private void Start()
    {
        CreateAllParticleSystems();
        InitializeFireflies();
        SetupGustLines();

        if (WindSystem.Instance != null)
            WindSystem.Instance.OnGustStart += OnGustStarted;
    }

    private void CreateAllParticleSystems()
    {
        if (enableDust)
            dustPS = CreateAmbientPS("DustPS", dustCount, dustColor, dustSize, dustHeight, dustSpread, true);

        if (enablePollen)
            pollenPS = CreateAmbientPS("PollenPS", pollenCount, pollenColor, 0.05f, 1.5f, 15f, true);

        if (enableFireflies)
        {
            var go = new GameObject("FireflyPS");
            go.transform.SetParent(transform);
            fireflyPS = go.AddComponent<ParticleSystem>();
            var main = fireflyPS.main;
            main.maxParticles = fireflyCount + 10;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = float.MaxValue;
            var em = fireflyPS.emission; em.enabled = false;

            var renderer = fireflyPS.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateAdditiveMaterial(fireflyColor);
        }

        if (enableFallingLeaves)
            leafPS = CreateLeafPS();
    }

    private ParticleSystem CreateAmbientPS(string name, float count, Color color, float size, float height, float spread, bool noisy)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.maxParticles = (int)(count * 2) + 50;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 20f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 1.5f);
        main.startColor = color;
        main.gravityModifier = -0.02f; // leicht nach oben treiben
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = count / 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spread * 2f, height * 0.5f, spread * 2f);
        shape.position = new Vector3(0f, height, 0f);

        if (noisy)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.1f;
        }

        var velOverLife = ps.velocityOverLifetime;
        velOverLife.enabled = true;
        velOverLife.space = ParticleSystemSimulationSpace.World;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateTransparentMaterial(color);

        ps.Play();
        return ps;
    }

    private ParticleSystem CreateLeafPS()
    {
        var go = new GameObject("LeafPS");
        go.transform.SetParent(transform);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = 200;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(leafFallSpeed * 0.5f, leafFallSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(leafSize * 0.5f, leafSize * 1.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = leafColors[0];
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Zufällige Blatt-Farben
        var startColorOverLifetime = ps.colorOverLifetime;

        var emission = ps.emission;
        emission.rateOverTime = leafEmissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(leafSpread * 2f, 1f, leafSpread * 2f);
        shape.position = new Vector3(0f, leafSpawnHeight, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.5f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.15f;

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateTransparentMaterial(leafColors[0]);

        ps.Play();
        return ps;
    }

    private void InitializeFireflies()
    {
        if (!enableFireflies || fireflyPS == null) return;

        fireflyParticles = new ParticleSystem.Particle[fireflyCount];
        fireflyPositions = new Vector3[fireflyCount];
        fireflyTargets = new Vector3[fireflyCount];
        fireflyBlinkOffsets = new float[fireflyCount];
        fireflyBlinkPhases = new float[fireflyCount];

        for (int i = 0; i < fireflyCount; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * fireflySpread;
            fireflyPositions[i] = new Vector3(rnd.x, Random.Range(fireflyHeight, fireflyMaxHeight), rnd.y);
            fireflyTargets[i] = GetNewFireflyTarget(fireflyPositions[i]);
            fireflyBlinkOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            fireflyBlinkPhases[i] = Random.Range(0f, 1f);

            fireflyParticles[i] = new ParticleSystem.Particle
            {
                position = fireflyPositions[i],
                startSize = Random.Range(0.08f, 0.15f),
                startColor = new Color(fireflyColor.r, fireflyColor.g, fireflyColor.b, 0f),
                remainingLifetime = float.MaxValue,
                startLifetime = float.MaxValue,
                velocity = Vector3.zero
            };
        }
        fireflyPS.SetParticles(fireflyParticles, fireflyCount);
    }

    private Vector3 GetNewFireflyTarget(Vector3 current)
    {
        return current + new Vector3(
            Random.Range(-3f, 3f),
            Random.Range(-0.5f, 0.5f),
            Random.Range(-3f, 3f)
        );
    }

    private void SetupGustLines()
    {
        if (!enableGustLines) return;
        gustLineMaterial = AtmosphereSetup.CreateLineMaterial(gustLineColor, true);
    }

    private Material CreateTransparentMaterial(Color col)
    {
        return AtmosphereSetup.CreateParticleMaterial(col, false);
    }

    private Material CreateAdditiveMaterial(Color col)
    {
        return AtmosphereSetup.CreateParticleMaterial(col, true);
    }

    private void Update()
    {
        UpdateDustAndPollen();
        UpdateFireflies();
        UpdateLeaves();
        UpdateGustLines();
    }

    private void UpdateDustAndPollen()
    {
        if (WindSystem.Instance == null) return;

        var velDir = WindSystem.Instance.WindDirection3D * WindSystem.Instance.WindStrength;

        if (dustPS != null)
        {
            var vol = dustPS.velocityOverLifetime;
            vol.x = velDir.x * 0.5f;
            vol.z = velDir.z * 0.5f;
            var em = dustPS.emission;
            em.rateOverTime = enableDust ? dustCount / 10f * (WindSystem.Instance.WindStrength + 0.3f) : 0f;
        }

        if (pollenPS != null)
        {
            float hour = DayNightSystem.Instance != null ? DayNightSystem.Instance.CurrentHour : 12f;
            bool pollenTime = hour >= pollenActiveStartHour && hour < pollenActiveEndHour;
            bool sunny = WindSystem.Instance.currentWeather == WeatherType.Clear ||
                         WindSystem.Instance.currentWeather == WeatherType.Cloudy;
            var em = pollenPS.emission;
            em.rateOverTime = (enablePollen && pollenTime && sunny) ? pollenCount / 15f : 0f;
        }
    }

    private void UpdateFireflies()
    {
        if (!enableFireflies || fireflyPS == null || fireflyParticles == null) return;

        float hour = DayNightSystem.Instance != null ? DayNightSystem.Instance.CurrentHour : 0f;
        bool isNight = hour >= fireflyActiveStartHour || hour < fireflyActiveEndHour;
        bool clearNight = isNight && (WindSystem.Instance == null || !WindSystem.Instance.IsStormy);

        float targetAlpha = clearNight ? 1f : 0f;
        currentFireflyAlpha = Mathf.Lerp(currentFireflyAlpha, targetAlpha, Time.deltaTime * 0.3f);

        for (int i = 0; i < fireflyCount; i++)
        {
            // Sanft zum Ziel bewegen
            fireflyPositions[i] = Vector3.MoveTowards(fireflyPositions[i], fireflyTargets[i],
                fireflyMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(fireflyPositions[i], fireflyTargets[i]) < 0.3f)
                fireflyTargets[i] = GetNewFireflyTarget(fireflyPositions[i]);

            // Höhe begrenzen
            fireflyPositions[i].y = Mathf.Clamp(fireflyPositions[i].y, fireflyHeight, fireflyMaxHeight);

            // Blinken: Puls mit random Phase
            fireflyBlinkPhases[i] += Time.deltaTime * fireflyBlinkSpeed * (0.8f + Mathf.PerlinNoise(i * 0.1f, Time.time * 0.1f) * 0.4f);
            float blink = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(fireflyBlinkPhases[i] + fireflyBlinkOffsets[i])), 3f);

            float finalAlpha = currentFireflyAlpha * blink;

            fireflyParticles[i].position = fireflyPositions[i];
            fireflyParticles[i].startColor = new Color(fireflyColor.r, fireflyColor.g, fireflyColor.b, finalAlpha);
            fireflyParticles[i].startSize = 0.1f + blink * 0.05f;
        }

        fireflyPS.SetParticles(fireflyParticles, fireflyCount);
    }

    private void UpdateLeaves()
    {
        if (leafPS == null || !enableFallingLeaves) return;

        bool isStormy = WindSystem.Instance != null && WindSystem.Instance.IsStormy;
        bool isRaining = WindSystem.Instance != null && WindSystem.Instance.IsRaining;
        var em = leafPS.emission;
        float rate = leafEmissionRate;
        if (isStormy) rate *= 4f;
        else if (isRaining) rate *= 2f;
        em.rateOverTime = rate;

        if (WindSystem.Instance != null)
        {
            var velOverLife = leafPS.velocityOverLifetime;
            var windDir = WindSystem.Instance.WindDirection3D;
            float windStr = WindSystem.Instance.WindStrength;
            velOverLife.x = windDir.x * windStr * 3f;
            velOverLife.z = windDir.z * windStr * 3f;
        }
    }

    private void UpdateGustLines()
    {
        if (!enableGustLines || WindSystem.Instance == null) return;

        float windStr = WindSystem.Instance.WindStrength;
        float gustInt = WindSystem.Instance.GustIntensity;
        float combinedStr = windStr + gustInt * 0.5f;

        // Neue Gust Lines spawnen
        nextGustLineTime -= Time.deltaTime;
        if (nextGustLineTime <= 0f && activeGustLines.Count < maxGustLines && combinedStr > 0.1f)
        {
            SpawnGustLine();
            nextGustLineTime = Random.Range(0.1f, 0.5f) / Mathf.Max(0.1f, combinedStr);
        }

        // Aktive Gust Lines updaten
        for (int i = activeGustLines.Count - 1; i >= 0; i--)
        {
            var gust = activeGustLines[i];
            gust.lifetime += Time.deltaTime;
            if (gust.lifetime >= gust.maxLifetime)
            {
                if (gust.lineRenderer != null)
                    Destroy(gust.lineRenderer.gameObject);
                activeGustLines.RemoveAt(i);
                continue;
            }

            gust.position += gust.velocity * Time.deltaTime;

            float t = gust.lifetime / gust.maxLifetime;
            float alpha = gustLineColor.a * Mathf.Sin(t * Mathf.PI) * Mathf.Min(1f, combinedStr * 2f);

            if (gust.lineRenderer != null)
            {
                gust.lineRenderer.SetPosition(0, gust.position);
                gust.lineRenderer.SetPosition(1, gust.position - gust.velocity.normalized * gustLineLength * (1f - t * 0.5f));

                var startColor = new Color(gustLineColor.r, gustLineColor.g, gustLineColor.b, alpha);
                var endColor = new Color(gustLineColor.r, gustLineColor.g, gustLineColor.b, 0f);
                gust.lineRenderer.startColor = startColor;
                gust.lineRenderer.endColor = endColor;
            }
        }
    }

    private void SpawnGustLine()
    {
        if (WindSystem.Instance == null) return;

        var go = new GameObject("GustLine");
        go.transform.SetParent(transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(gustLineMaterial);
        lr.startWidth = gustLineWidth;
        lr.endWidth = 0f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Vector3 windDir = WindSystem.Instance.WindDirection3D;
        float windStr = WindSystem.Instance.WindStrength;

        // Spawn vor dem Wind, auf zufälliger Höhe
        Vector3 perpendicular = new Vector3(-windDir.z, 0f, windDir.x);
        Vector3 spawnPos = perpendicular * Random.Range(-20f, 20f);
        spawnPos.y = Random.Range(gustLineHeight, gustLineMaxHeight);

        var gust = new GustLine
        {
            position = spawnPos,
            velocity = windDir * (gustLineSpeed * (windStr + WindSystem.Instance.GustIntensity)),
            lifetime = 0f,
            maxLifetime = Random.Range(0.5f, 2f),
            height = spawnPos.y,
            lineRenderer = lr
        };

        lr.SetPosition(0, spawnPos);
        lr.SetPosition(1, spawnPos - windDir * gustLineLength);

        activeGustLines.Add(gust);
    }

    private void OnGustStarted(float intensity)
    {
        // Extra Gust Lines bei Böen
        int extraLines = Mathf.RoundToInt(intensity * 5f);
        for (int i = 0; i < extraLines && activeGustLines.Count < maxGustLines; i++)
            SpawnGustLine();
    }

    private void OnDestroy()
    {
        if (WindSystem.Instance != null)
            WindSystem.Instance.OnGustStart -= OnGustStarted;
    }
}

using UnityEngine;
using System.Collections;

/// <summary>
/// AuroraSystem - Polarlicht / Nordlicht
/// Prozedural animierte Auroras mit mehreren Bändern und Shader
/// </summary>
public class AuroraSystem : MonoBehaviour
{
    [Header("Aurora Toggle")]
    public bool auroraActive = true;
    public float auroraChancePerNight = 0.6f; // 60% Chance pro Nacht

    [Header("Aurora Erscheinung")]
    [Range(1, 5)] public int bandCount = 3;
    public float auroraHeight = 200f;
    public float auroraRadius = 400f;
    public float bandThickness = 40f;
    [Range(8, 64)] public int bandSegments = 32;

    [Header("Farben")]
    public Gradient auroraColorGreen; // Klassisch grüne Aurora
    public Gradient auroraColorBlue;  // Blaue Variante
    public bool useMultiColor = true;

    [Header("Animation")]
    public float waveSpeed = 0.3f;
    public float waveAmplitude = 30f;
    public float waveFrequency = 2f;
    public float flickerSpeed = 1.5f;
    public float flickerIntensity = 0.2f;
    public float breathingSpeed = 0.5f;

    [Header("Intensität")]
    [Range(0f, 3f)] public float maxIntensity = 1.5f;
    public float fadeInDuration = 30f;  // Sekunden zum einblenden
    public float fadeOutDuration = 45f;
    public float minActiveDuration = 120f;
    public float maxActiveDuration = 400f;

    [Header("Zeitfenster")]
    public float auroraStartHour = 21f;
    public float auroraEndHour = 4f;

    private Mesh[] bandMeshes;
    private MeshRenderer[] bandRenderers;
    private MeshFilter[] bandFilters;
    private Material auroraMaterial;
    private float currentIntensity = 0f;
    private bool isAuroraShowing = false;
    private float auroraTimer = 0f;
    private float targetDuration = 0f;
    private Vector3[] waveOffsets;

    private void Start()
    {
        SetupDefaultGradients();
        CreateAuroraMaterial();
        CreateAuroraBands();

        if (DayNightSystem.Instance != null)
        {
            DayNightSystem.Instance.OnNightStart += TryStartAurora;
        }
    }

    private void SetupDefaultGradients()
    {
        if (auroraColorGreen == null)
        {
            auroraColorGreen = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0f, 0.8f, 0.2f), 0f),
                new GradientColorKey(new Color(0f, 1f, 0.5f), 0.3f),
                new GradientColorKey(new Color(0.2f, 0.9f, 0.3f), 0.6f),
                new GradientColorKey(new Color(0.5f, 0.3f, 1f), 1f) // Violett am Ende
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.2f),
                new GradientAlphaKey(0.6f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            };
            auroraColorGreen.SetKeys(colorKeys, alphaKeys);
        }

        if (auroraColorBlue == null)
        {
            auroraColorBlue = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0f, 0.3f, 1f), 0f),
                new GradientColorKey(new Color(0.1f, 0.7f, 1f), 0.4f),
                new GradientColorKey(new Color(0.5f, 0.2f, 0.9f), 1f)
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.7f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            };
            auroraColorBlue.SetKeys(colorKeys, alphaKeys);
        }
    }

    private void CreateAuroraMaterial()
    {
        auroraMaterial = AtmosphereSetup.CreateLineMaterial(Color.green, true);
        auroraMaterial.renderQueue = 3000;
        auroraMaterial.enableInstancing = true;
    }

    private void CreateAuroraBands()
    {
        bandMeshes = new Mesh[bandCount];
        bandRenderers = new MeshRenderer[bandCount];
        bandFilters = new MeshFilter[bandCount];
        waveOffsets = new Vector3[bandCount];

        for (int b = 0; b < bandCount; b++)
        {
            GameObject bandGO = new GameObject($"Aurora_Band_{b}");
            bandGO.transform.SetParent(transform);
            bandGO.transform.position = Vector3.zero;

            bandFilters[b] = bandGO.AddComponent<MeshFilter>();
            bandRenderers[b] = bandGO.AddComponent<MeshRenderer>();
            bandRenderers[b].material = new Material(auroraMaterial);
            bandRenderers[b].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bandRenderers[b].receiveShadows = false;

            // Verschiedene Höhen und Winkel pro Band
            waveOffsets[b] = new Vector3(
                UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                UnityEngine.Random.Range(0f, 100f),
                UnityEngine.Random.Range(0.5f, 1.5f)
            );

            bandMeshes[b] = new Mesh();
            bandFilters[b].mesh = bandMeshes[b];
        }
    }

    private void Update()
    {
        if (!auroraActive) 
        {
            if (currentIntensity > 0f)
                currentIntensity = Mathf.Max(0f, currentIntensity - Time.deltaTime / fadeOutDuration);
            return;
        }

        ManageAuroraLifecycle();

        if (currentIntensity > 0.01f)
        {
            UpdateAllBands();
        }
        else
        {
            // Meshes verstecken
            foreach (var r in bandRenderers)
                if (r) r.enabled = false;
        }
    }

    private void ManageAuroraLifecycle()
    {
        float hour = DayNightSystem.Instance != null ? DayNightSystem.Instance.CurrentHour : 0f;
        bool isNightTime = hour >= auroraStartHour || hour < auroraEndHour;

        if (!isNightTime)
        {
            currentIntensity = Mathf.Max(0f, currentIntensity - Time.deltaTime / fadeOutDuration * 2f);
            isAuroraShowing = false;
            return;
        }

        if (isAuroraShowing)
        {
            auroraTimer += Time.deltaTime;

            if (auroraTimer < fadeInDuration)
                currentIntensity = Mathf.Lerp(0f, maxIntensity, auroraTimer / fadeInDuration);
            else if (auroraTimer < targetDuration - fadeOutDuration)
                currentIntensity = Mathf.Lerp(currentIntensity, maxIntensity, Time.deltaTime);
            else if (auroraTimer < targetDuration)
            {
                float fadeOutT = (auroraTimer - (targetDuration - fadeOutDuration)) / fadeOutDuration;
                currentIntensity = Mathf.Lerp(maxIntensity, 0f, fadeOutT);
            }
            else
            {
                isAuroraShowing = false;
                currentIntensity = 0f;
            }
        }
    }

    public void TryStartAurora()
    {
        if (!auroraActive) return;
        if (UnityEngine.Random.value < auroraChancePerNight)
            StartAurora();
    }

    public void StartAurora()
    {
        isAuroraShowing = true;
        auroraTimer = 0f;
        targetDuration = UnityEngine.Random.Range(minActiveDuration, maxActiveDuration);
    }

    public void StopAurora() => isAuroraShowing = false;

    private void UpdateAllBands()
    {
        for (int b = 0; b < bandCount; b++)
        {
            if (bandRenderers[b]) bandRenderers[b].enabled = true;
            UpdateBandMesh(b);
        }
    }

    private void UpdateBandMesh(int bandIndex)
    {
        int segments = bandSegments;
        int vertCount = (segments + 1) * 2;
        Vector3[] verts = new Vector3[vertCount];
        Color[] colors = new Color[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[segments * 6];

        float bandHeightOffset = bandIndex * (bandThickness * 0.8f);
        float waveOff = waveOffsets[bandIndex].x;
        float timeOff = waveOffsets[bandIndex].y;
        float speedMult = waveOffsets[bandIndex].z;
        float flicker = 1f + Mathf.Sin(Time.time * flickerSpeed + waveOff) * flickerIntensity;
        float breath = 1f + Mathf.Sin(Time.time * breathingSpeed + waveOff * 0.3f) * 0.1f;

        Gradient colorGrad = (useMultiColor && bandIndex % 2 == 1) ? auroraColorBlue : auroraColorGreen;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;

            float wave = Mathf.Sin(angle * waveFrequency + Time.time * waveSpeed * speedMult + timeOff) * waveAmplitude;
            float wave2 = Mathf.Sin(angle * waveFrequency * 0.7f + Time.time * waveSpeed * 0.8f) * waveAmplitude * 0.4f;

            float x = Mathf.Cos(angle) * auroraRadius;
            float z = Mathf.Sin(angle) * auroraRadius;
            float yBase = auroraHeight + bandHeightOffset + wave + wave2;
            float yTop = yBase + bandThickness * breath;

            int vi = i * 2;
            verts[vi] = new Vector3(x, yBase, z);
            verts[vi + 1] = new Vector3(x, yTop, z);

            Color cBottom = colorGrad.Evaluate(0f); cBottom.a *= currentIntensity * flicker;
            Color cMid = colorGrad.Evaluate(0.5f); cMid.a *= currentIntensity * flicker;
            Color cTop = colorGrad.Evaluate(1f); cTop.a *= currentIntensity * flicker * 0.5f;

            colors[vi] = Color.Lerp(cBottom, cMid, t < 0.5f ? t * 2f : (1f - t) * 2f);
            colors[vi + 1] = cTop;

            uvs[vi] = new Vector2(t, 0f);
            uvs[vi + 1] = new Vector2(t, 1f);
        }

        for (int i = 0; i < segments; i++)
        {
            int ti = i * 6;
            int vi = i * 2;
            tris[ti] = vi; tris[ti + 1] = vi + 1; tris[ti + 2] = vi + 2;
            tris[ti + 3] = vi + 1; tris[ti + 4] = vi + 3; tris[ti + 5] = vi + 2;
        }

        Mesh mesh = bandMeshes[bandIndex];
        mesh.Clear();
        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
    }
}

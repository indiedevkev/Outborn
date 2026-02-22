using UnityEngine;

/// <summary>
/// AtmosphereSetup - Erstellt ALLE benötigten Materialien automatisch zur Laufzeit.
/// Dieses Script auf AtmosphereManager legen. Es läuft einmalig in Awake().
/// 
/// Löst das Problem: Shader.Find() schlägt fehl wenn Shader-Namen falsch.
/// Hier werden alle URP-kompatiblen Shader-Namen korrekt gesetzt.
/// </summary>
[DefaultExecutionOrder(-100)] // Läuft vor allen anderen Scripts
public class AtmosphereSetup : MonoBehaviour
{
    [Header("Automatisch erstellt (zur Info)")]
    [TextArea(3, 6)]
    public string setupLog = "Noch nicht initialisiert. Play drücken.";

    [Header("Skybox")]
    [Tooltip("Wird automatisch erstellt und in RenderSettings gesetzt")]
    public bool autoCreateSkybox = true;

    [Header("Referenzen (werden auto-gefunden)")]
    public PrecipitationSystem precipitationSystem;
    public CloudSystem cloudSystem;
    public AmbientParticleSystem ambientParticles;
    public StarSystem starSystem;
    public AuroraSystem auroraSystem;

    private System.Text.StringBuilder log = new System.Text.StringBuilder();

    private void Awake()
    {
        log.Clear();
        log.AppendLine("=== AtmosphereSetup ===");

        // Systeme auto-finden falls nicht zugewiesen
        if (!precipitationSystem) precipitationSystem = FindFirstObjectByType<PrecipitationSystem>();
        if (!cloudSystem)         cloudSystem         = FindFirstObjectByType<CloudSystem>();
        if (!ambientParticles)    ambientParticles    = FindFirstObjectByType<AmbientParticleSystem>();
        if (!starSystem)          starSystem          = FindFirstObjectByType<StarSystem>();
        if (!auroraSystem)        auroraSystem        = FindFirstObjectByType<AuroraSystem>();

        if (autoCreateSkybox) SetupSkybox();
        SetupFog();

        log.AppendLine("✅ Setup abgeschlossen.");
        setupLog = log.ToString();
        Debug.Log(setupLog);
    }

    // ─────────────────────────────────────────────────────────────
    // ÖFFENTLICHE MATERIAL-FACTORY METHODEN
    // Werden von PrecipitationSystem, CloudSystem etc. aufgerufen
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Erstellt ein URP Particle Material (Transparent, Alpha Blend)
    /// </summary>
    public static Material CreateParticleMaterial(Color color, bool additive = false)
    {
        Material mat = null;

        // URP Shader Prioritätsliste - erster der gefunden wird gewinnt
        string[] urpParticleShaders = {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "UI/Default"
        };

        foreach (var shaderName in urpParticleShaders)
        {
            Shader s = Shader.Find(shaderName);
            if (s != null)
            {
                mat = new Material(s);
                Debug.Log($"[AtmosphereSetup] Particle Shader gefunden: {shaderName}");
                break;
            }
        }

        if (mat == null)
        {
            // Absoluter Fallback: Sprites/Default funktioniert immer
            Shader fallback = Shader.Find("Sprites/Default");
            if (fallback == null) fallback = Shader.Find("UI/Default");
            mat = new Material(fallback);
            Debug.LogWarning("[AtmosphereSetup] Kein URP Particle Shader gefunden! Verwende Sprites/Default Fallback.");
        }

        // Transparenz-Einstellungen
        mat.SetFloat("_Surface", 1f); // Transparent
        if (additive)
        {
            mat.SetFloat("_Blend", 4f); // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        else
        {
            mat.SetFloat("_Blend", 0f); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = additive ? 3001 : 3000;
        mat.color = color;
        mat.enableInstancing = true;

        return mat;
    }

    /// <summary>
    /// Erstellt ein URP Line Renderer Material (für Gust Lines und Aurora)
    /// </summary>
    public static Material CreateLineMaterial(Color color, bool additive = true)
    {
        string[] lineShaders = {
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "Unlit/Color"
        };

        Material mat = null;
        foreach (var sn in lineShaders)
        {
            Shader s = Shader.Find(sn);
            if (s != null) { mat = new Material(s); break; }
        }

        mat ??= new Material(Shader.Find("Sprites/Default"));

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        mat.SetInt("_ZWrite", 0);
        mat.SetFloat("_Surface", 1f);
        mat.renderQueue = 3002;
        mat.color = color;
        return mat;
    }

    /// <summary>
    /// Erstellt das Skybox Material und weist es RenderSettings zu
    /// </summary>
    private void SetupSkybox()
    {
        // Bereits ein Skybox Material gesetzt?
        if (RenderSettings.skybox != null && RenderSettings.skybox.shader.name.Contains("Procedural"))
        {
            log.AppendLine("✅ Skybox bereits konfiguriert: " + RenderSettings.skybox.shader.name);

            // SkyboxController damit verknüpfen
            var skyCtrl = FindFirstObjectByType<SkyboxController>();
            if (skyCtrl != null && skyCtrl.skyboxMaterial == null)
            {
                skyCtrl.skyboxMaterial = RenderSettings.skybox;
                log.AppendLine("   → SkyboxController verknüpft");
            }
            return;
        }

        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader == null)
        {
            log.AppendLine("❌ Skybox/Procedural Shader nicht gefunden!");
            log.AppendLine("   → Bitte manuell: Create → Material → Skybox/Procedural");
            return;
        }

        Material skyMat = new Material(skyShader);
        skyMat.name = "AutoSkyboxMaterial";

        // Gute Startwerte für Tag
        skyMat.SetFloat("_SunSize", 0.04f);
        skyMat.SetFloat("_SunSizeConvergence", 5f);
        skyMat.SetFloat("_AtmosphereThickness", 1.1f);
        skyMat.SetColor("_SkyTint", new Color(0.38f, 0.62f, 0.88f));
        skyMat.SetColor("_GroundColor", new Color(0.37f, 0.35f, 0.34f));
        skyMat.SetFloat("_Exposure", 1.3f);

        RenderSettings.skybox = skyMat;
        DynamicGI.UpdateEnvironment();

        // SkyboxController verknüpfen
        var ctrl = FindFirstObjectByType<SkyboxController>();
        if (ctrl != null)
        {
            ctrl.skyboxMaterial = skyMat;
            log.AppendLine("✅ Skybox erstellt + SkyboxController verknüpft");
        }
        else
        {
            log.AppendLine("✅ Skybox erstellt (kein SkyboxController gefunden)");
        }
    }

    /// <summary>
    /// Fog Basis-Einstellungen
    /// </summary>
    private void SetupFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        if (RenderSettings.fogDensity < 0.001f)
            RenderSettings.fogDensity = 0.003f;

        RenderSettings.fogColor = new Color(0.6f, 0.7f, 0.85f);
        log.AppendLine("✅ Fog konfiguriert (Exponential Squared, Density: 0.003)");
    }

    // ─────────────────────────────────────────────────────────────
    // MATERIAL HELPER - statisch aufrufbar von jedem anderen Script
    // ─────────────────────────────────────────────────────────────

    public static Material GetOrCreateParticleMat(ref Material existing, Color color, bool additive = false)
    {
        if (existing == null)
            existing = CreateParticleMaterial(color, additive);
        return existing;
    }
}

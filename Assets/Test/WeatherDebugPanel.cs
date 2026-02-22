using UnityEngine;
using UnityEngine.InputSystem;  // Neues Input System

/// <summary>
/// WeatherDebugPanel - Live-Steuerung aller Systeme
/// F1 = Panel an/aus | F2 = Größer | F3 = Kleiner
/// Nutzt Unity New Input System (kein legacy Input.GetKeyDown)
/// </summary>
public class WeatherDebugPanel : MonoBehaviour
{
    [Header("Panel")]
    public bool showPanel = true;
    [Range(0.6f, 3f)] public float uiScale = 1.5f;
    [Range(0f, 1f)]   public float panelOpacity = 0.92f;

    private Rect panelRect = new Rect(15, 15, 440, 860);
    private Vector2 scrollPos;
    private bool isResizing = false;
    private Vector2 resizeStartMouse;
    private Vector2 resizeStartSize;

    private GUIStyle _windowStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _subLabelStyle;
    private GUIStyle _valueStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _toggleStyle;
    private bool _stylesReady = false;

    private void Update()
    {
        // Neues Input System: Keyboard.current statt Input.GetKeyDown
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f1Key.wasPressedThisFrame) showPanel = !showPanel;
        if (kb.f2Key.wasPressedThisFrame) uiScale = Mathf.Min(3f,   uiScale + 0.1f);
        if (kb.f3Key.wasPressedThisFrame) uiScale = Mathf.Max(0.6f, uiScale - 0.1f);
    }

    private void OnGUI()
    {
        if (!showPanel) return;
        BuildStyles();

        Matrix4x4 prev = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * uiScale, Vector2.zero);

        float sw = Screen.width  / uiScale;
        float sh = Screen.height / uiScale;
        panelRect.x = Mathf.Clamp(panelRect.x, 0f, sw - panelRect.width);
        panelRect.y = Mathf.Clamp(panelRect.y, 0f, sh - panelRect.height);

        Color bgOld = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.07f, 0.09f, 0.13f, panelOpacity);
        panelRect = GUI.Window(42, panelRect, DrawPanel, "  🌤  ATMOSPHÄREN DEBUG", _windowStyle);
        GUI.backgroundColor = bgOld;
        GUI.matrix = prev;
    }

    private void DrawPanel(int id)
    {
        Rect rHandle = new Rect(panelRect.width - 20, panelRect.height - 20, 18, 18);
        GUI.Label(rHandle, "⇲", _subLabelStyle);
        HandleResize(rHandle);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(panelRect.height - 34));

        Hint("[F1] ein/aus  |  [F2] größer  |  [F3] kleiner");
        Space();

        // ── TAGESZEIT ────────────────────────────────────────────
        Section("⏰  TAGESZEIT");
        var dns = DayNightSystem.Instance;
        if (dns != null)
        {
            Row($"🕐  {dns.GetTimeString()}",
                dns.IsDaytime ? "☀  Tag" : "🌙  Nacht",
                $"Sonne: {dns.GetSunAltitude():F2}");
            SliderRow("Zeit",      ref dns.currentHour, 0f,   24f,  "h", 40f);
            SliderRow("Zeitskala", ref dns.timeScale,   0.1f, 200f, "x", 40f);
            GUILayout.BeginHorizontal();
            if (Btn("🌅 6:00"))   dns.SetTime(6f);
            if (Btn("☀ 12:00"))  dns.SetTime(12f);
            if (Btn("🌇 19:00")) dns.SetTime(19f);
            if (Btn("🌙 23:00")) dns.SetTime(23f);
            GUILayout.EndHorizontal();
            dns.pauseTime = Toggle(dns.pauseTime, "⏸  Zeit pausieren");
        }
        else Warn("DayNightSystem nicht gefunden");

        // ── WIND ─────────────────────────────────────────────────
        Space(); Section("🌬  WIND");
        var wind = WindSystem.Instance;
        if (wind != null)
        {
            SliderRow("Stärke",   ref wind.windStrength,  0f, 1f,   "",  35f);
            SliderRow("Richtung", ref wind.windDirection, 0f, 360f, "°", 35f);
            Hint($"Effektiv: {wind.WindStrength:F2}  |  Böe: {wind.GustIntensity:F2}");
            wind.randomizeWind = Toggle(wind.randomizeWind, "🔀  Auto Wind Variation");
            wind.enableGusts   = Toggle(wind.enableGusts,   "💨  Böen aktiviert");
            if (Btn("🌪  Böe manuell auslösen")) wind.TriggerGust();
        }
        else Warn("WindSystem nicht gefunden");

        // ── WETTER ───────────────────────────────────────────────
        Space(); Section("🌦  WETTER");
        if (wind != null)
        {
            Hint($"Aktuell: {wind.currentWeather}  |  Wolken: {wind.CloudCoverage:F2}");
            GUILayout.BeginHorizontal();
            if (Btn("☀ Klar"))    wind.SetWeather(WeatherType.Clear);
            if (Btn("☁ Bewölkt")) wind.SetWeather(WeatherType.Cloudy);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Btn("🌧 Regen"))  wind.SetWeather(WeatherType.Rain);
            if (Btn("⛈ Sturm"))  wind.SetWeather(WeatherType.Storm);
            if (Btn("❄ Schnee")) wind.SetWeather(WeatherType.Snow);
            GUILayout.EndHorizontal();
        }

        // ── AURORA ───────────────────────────────────────────────
        Space(); Section("🌌  AURORA / POLARLICHTER");
        var aurora = FindFirstObjectByType<AuroraSystem>();
        if (aurora != null)
        {
            aurora.auroraActive = Toggle(aurora.auroraActive, "Aurora aktiv");
            GUILayout.BeginHorizontal();
            if (Btn("▶ Starten")) aurora.StartAurora();
            if (Btn("■ Stoppen")) aurora.StopAurora();
            GUILayout.EndHorizontal();
            SliderRow("Intensität",  ref aurora.maxIntensity, 0f, 3f, "", 30f);
            SliderRow("Wellenspeed", ref aurora.waveSpeed,    0f, 2f, "", 30f);
            SliderRow("Flimmern",    ref aurora.flickerSpeed, 0f, 5f, "", 30f);
            aurora.bandCount     = SliderRowInt("Bänder",     aurora.bandCount,    1, 5);
            aurora.useMultiColor = Toggle(aurora.useMultiColor, "Mehrfarbig (Grün + Blau)");
        }
        else Warn("AuroraSystem nicht gefunden");

        // ── PARTIKEL ─────────────────────────────────────────────
        Space(); Section("✨  AMBIENT PARTIKEL");
        var ap = FindFirstObjectByType<AmbientParticleSystem>();
        if (ap != null)
        {
            ap.enableDust          = Toggle(ap.enableDust,          "💨  Staub / Sporen");
            ap.enablePollen        = Toggle(ap.enablePollen,        "🌿  Pollen (tagsüber)");
            ap.enableFireflies     = Toggle(ap.enableFireflies,     "🪲  Glühwürmchen (nachts)");
            ap.enableFallingLeaves = Toggle(ap.enableFallingLeaves, "🍂  Fallende Blätter");
            ap.enableGustLines     = Toggle(ap.enableGustLines,     "〰  Windböen-Linien");
            SliderRow("Blatt Rate",  ref ap.leafEmissionRate, 0f, 10f, "/s", 30f);
            ap.fireflyCount = SliderRowInt("Glühwürmchen",   ap.fireflyCount, 0, 80);
            ap.maxGustLines = SliderRowInt("Max Gust Lines", ap.maxGustLines, 0, 30);
        }

        // ── STERNE ───────────────────────────────────────────────
        Space(); Section("⭐  STERNE");
        var stars = FindFirstObjectByType<StarSystem>();
        if (stars != null)
        {
            SliderRow("Twinkle Speed", ref stars.twinkleSpeed,     0f, 1f, "", 35f);
            SliderRow("Twinkle Int.",  ref stars.twinkleIntensity, 0f, 1f, "", 35f);
            stars.showMilkyWay = Toggle(stars.showMilkyWay, "🌌  Milchstraße");
        }

        // ── SKYBOX ───────────────────────────────────────────────
        Space(); Section("🌅  SKYBOX");
        var sky = FindFirstObjectByType<SkyboxController>();
        if (sky != null)
            SliderRow("Horizont Glow", ref sky.horizonGlowDuration, 0f, 3f, "h", 30f);
        else
            Hint("SkyboxController nicht in Szene");

        // ── BLITZE ───────────────────────────────────────────────
        Space(); Section("⚡  BLITZE & REGEN");
        var precip = FindFirstObjectByType<PrecipitationSystem>();
        if (precip != null)
        {
            precip.enableLightning = Toggle(precip.enableLightning, "⚡  Blitze aktiv");
            if (Btn("⚡  Blitz jetzt auslösen")) precip.TriggerLightning();
            SliderRow("Min Intervall", ref precip.lightningMinInterval, 1f, 60f, "s", 30f);
            SliderRow("Max Intervall", ref precip.lightningMaxInterval, 1f, 60f, "s", 30f);
        }

        // ── PRESETS ──────────────────────────────────────────────
        Space(); Section("🎬  SZENEN PRESETS");
        GUILayout.BeginHorizontal();
        if (Btn("🌅 Sonnenaufgang"))  Preset_Sunrise();
        if (Btn("☀ Mittagssonne"))   Preset_Noon();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Btn("🌇 Golden Hour"))   Preset_GoldenHour();
        if (Btn("🌌 Klare Nacht"))  Preset_ClearNight();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Btn("⛈ Sturmnacht"))    Preset_Stormy();
        if (Btn("❄ Schneenacht"))   Preset_SnowNight();
        GUILayout.EndHorizontal();
        if (Btn("🌌✨ Aurora Nacht")) Preset_AuroraNight();

        GUILayout.Space(12);
        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, panelRect.width - 25, 22));
    }

    // ── PRESETS ──────────────────────────────────────────────────
    void Preset_Sunrise()    { DayNightSystem.Instance?.SetTime(6.2f);  WindSystem.Instance?.SetWeather(WeatherType.Clear); }
    void Preset_Noon()       { DayNightSystem.Instance?.SetTime(12f);   WindSystem.Instance?.SetWeather(WeatherType.Clear); if (WindSystem.Instance) WindSystem.Instance.windStrength = 0.1f; }
    void Preset_GoldenHour() { DayNightSystem.Instance?.SetTime(19.5f); WindSystem.Instance?.SetWeather(WeatherType.Clear); if (WindSystem.Instance) WindSystem.Instance.windStrength = 0.15f; }
    void Preset_ClearNight() { DayNightSystem.Instance?.SetTime(1f);    WindSystem.Instance?.SetWeather(WeatherType.Clear); FindFirstObjectByType<AuroraSystem>()?.StopAurora(); }
    void Preset_Stormy()     { DayNightSystem.Instance?.SetTime(2f);    WindSystem.Instance?.SetWeather(WeatherType.Storm); }
    void Preset_SnowNight()  { DayNightSystem.Instance?.SetTime(21f);   WindSystem.Instance?.SetWeather(WeatherType.Snow); }
    void Preset_AuroraNight()
    {
        DayNightSystem.Instance?.SetTime(0f);
        WindSystem.Instance?.SetWeather(WeatherType.Clear);
        if (WindSystem.Instance) WindSystem.Instance.windStrength = 0.05f;
        FindFirstObjectByType<AuroraSystem>()?.StartAurora();
    }

    // ── RESIZE ───────────────────────────────────────────────────
    private void HandleResize(Rect handle)
    {
        Vector2 mouse = Event.current.mousePosition;
        if (Event.current.type == EventType.MouseDown && handle.Contains(mouse))
        {
            isResizing = true;
            resizeStartMouse = mouse;
            resizeStartSize  = new Vector2(panelRect.width, panelRect.height);
            Event.current.Use();
        }
        if (isResizing)
        {
            if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove)
            {
                Vector2 d = mouse - resizeStartMouse;
                panelRect.width  = Mathf.Max(300f, resizeStartSize.x + d.x);
                panelRect.height = Mathf.Max(200f, resizeStartSize.y + d.y);
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp) { isResizing = false; Event.current.Use(); }
        }
    }

    // ── HELPER ───────────────────────────────────────────────────
    void Section(string t) { GUILayout.Space(2); GUILayout.Label(t, _headerStyle); GUI.color = new Color(0.4f, 0.6f, 1f, 0.4f); GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true)); GUI.color = Color.white; GUILayout.Space(2); }
    void Hint(string t)    => GUILayout.Label(t, _subLabelStyle);
    void Warn(string t)    { GUI.color = new Color(1f, 0.4f, 0.3f); GUILayout.Label("⚠  " + t, _subLabelStyle); GUI.color = Color.white; }
    void Row(params string[] vals) { GUILayout.BeginHorizontal(); foreach (var v in vals) GUILayout.Label(v, _valueStyle); GUILayout.EndHorizontal(); }
    void Space()           => GUILayout.Space(6);
    bool Btn(string l)     => GUILayout.Button(l, _buttonStyle);
    bool Toggle(bool v, string l) => GUILayout.Toggle(v, l, _toggleStyle);

    void SliderRow(string label, ref float val, float min, float max, string unit, float vw)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", _subLabelStyle, GUILayout.Width(130));
        val = GUILayout.HorizontalSlider(val, min, max);
        GUILayout.Label($"{val:F2}{unit}", _valueStyle, GUILayout.Width(vw));
        GUILayout.EndHorizontal();
    }

    int SliderRowInt(string label, int val, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", _subLabelStyle, GUILayout.Width(130));
        int r = Mathf.RoundToInt(GUILayout.HorizontalSlider(val, min, max));
        GUILayout.Label($"{r}", _valueStyle, GUILayout.Width(25));
        GUILayout.EndHorizontal();
        return r;
    }

    // ── STYLES ───────────────────────────────────────────────────
    private void BuildStyles()
    {
        if (_stylesReady) return;
        _windowStyle = new GUIStyle(GUI.skin.window);
        _windowStyle.normal.textColor = new Color(0.75f, 0.88f, 1f);
        _windowStyle.fontSize = 13; _windowStyle.fontStyle = FontStyle.Bold;

        _headerStyle = new GUIStyle(GUI.skin.label);
        _headerStyle.fontSize = 13; _headerStyle.fontStyle = FontStyle.Bold;
        _headerStyle.normal.textColor = new Color(0.75f, 0.9f, 1f);

        _subLabelStyle = new GUIStyle(GUI.skin.label);
        _subLabelStyle.fontSize = 11;
        _subLabelStyle.normal.textColor = new Color(0.72f, 0.75f, 0.82f);

        _valueStyle = new GUIStyle(GUI.skin.label);
        _valueStyle.fontSize = 11; _valueStyle.fontStyle = FontStyle.Bold;
        _valueStyle.normal.textColor = new Color(0.95f, 0.95f, 0.7f);

        _buttonStyle = new GUIStyle(GUI.skin.button);
        _buttonStyle.fontSize = 11;
        _buttonStyle.normal.textColor = Color.white;
        _buttonStyle.hover.textColor  = new Color(0.5f, 0.9f, 1f);

        _toggleStyle = new GUIStyle(GUI.skin.toggle);
        _toggleStyle.fontSize = 11;
        _toggleStyle.normal.textColor = new Color(0.82f, 0.85f, 0.9f);
        _stylesReady = true;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Einfaches Baumenü als Leiste unten:
/// - B öffnet / schließt das Menü (über BuildingTest)
/// - Buttons wählen den aktuellen Bautyp (ruft BuildingPlacer.EnterBuildMode(index) auf)
/// Das Spiel läuft im Hintergrund weiter.
/// </summary>
public class BuildMenuUI : MonoBehaviour
{
    [Header("Referenzen")]
    [SerializeField] private BuildingPlacer buildingPlacer;

    [Header("Optional: eigene UI-Referenzen")]
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private RectTransform contentParent;

    [Header("Layout")]
    [SerializeField] private float barHeight = 80f;
    [SerializeField] private float barPadding = 10f;
    [SerializeField] private float buttonSize = 64f;
    [SerializeField] private float buttonSpacing = 8f;
    [SerializeField] private int labelFontSize = 12;

    [Header("Optik")]
    [SerializeField] private bool subtleBackground = true;
    [SerializeField] private float backgroundAlpha = 0.8f;
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
    [SerializeField] private Color normalColor = new Color(0.2f, 0.22f, 0.26f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.35f, 0.4f, 0.5f, 1f);

    [Header("Icons (optional)")]
    [Tooltip("Standard-Sprite für Buttons, falls kein eigenes Icon gesetzt ist.")]
    [SerializeField] private Sprite defaultButtonSprite;
    [Tooltip("Optionale Icons pro Gebäude, Länge sollte zu buildingPrefabs im BuildingPlacer passen.")]
    [SerializeField] private Sprite[] buildingIcons;

    private Canvas _canvas;
    private readonly List<Button> _buttons = new List<Button>();
    private int _currentIndex = 0;
    private bool _visible = false;

    void Start()
    {
        if (buildingPlacer == null)
        {
            buildingPlacer = FindFirstObjectByType<BuildingPlacer>();
        }

        if (buildingPlacer == null)
        {
            Debug.LogWarning("[BuildMenuUI] Kein BuildingPlacer gefunden.");
            enabled = false;
            return;
        }

        EnsureUI();
        BuildButtons();
        SetVisible(false);
    }

    private void EnsureUI()
    {
        if (barRoot != null && contentParent != null)
            return;

        var existing = FindFirstObjectByType<Canvas>();
        if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _canvas = existing;
        }
        else
        {
            var canvasGo = new GameObject("UIRootCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (barRoot == null)
        {
            var barGo = new GameObject("BuildBar");
            barGo.transform.SetParent(_canvas.transform, false);

            var rect = barGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, barPadding);
            rect.sizeDelta = new Vector2(600f, barHeight);
            barRoot = rect;

            if (subtleBackground)
            {
                var image = barGo.AddComponent<Image>();
                var col = backgroundColor;
                col.a = backgroundAlpha;
                image.color = col;
            }
        }

        if (contentParent == null)
        {
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(barRoot, false);

            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(1f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.offsetMin = new Vector2(barPadding, -buttonSize / 2f);
            contentRect.offsetMax = new Vector2(-barPadding, buttonSize / 2f);

            var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = buttonSpacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            contentParent = contentRect;
        }
    }

    private void BuildButtons()
    {
        _buttons.Clear();

        // buildingPrefabs ist im BuildingPlacer privat; wir gehen davon aus,
        // dass es im Inspector befüllt ist und nur über EnterBuildMode(index) angesprochen wird.
        // Wir verwenden hier einfach Indizes [0..N-1] und greifen auf Namen über die Prefabs zu.
        var prefabsField = typeof(BuildingPlacer).GetField("buildingPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (prefabsField == null)
        {
            Debug.LogWarning("[BuildMenuUI] buildingPrefabs-Feld in BuildingPlacer nicht gefunden.");
            return;
        }

        var prefabs = prefabsField.GetValue(buildingPlacer) as GameObject[];
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[BuildMenuUI] Keine buildingPrefabs im BuildingPlacer gesetzt.");
            return;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            var prefab = prefabs[i];
            var button = CreateButtonFor(prefab, i);
            _buttons.Add(button);
        }

        UpdateButtonHighlight();
    }

    private Button CreateButtonFor(GameObject prefab, int index)
    {
        var btnGo = new GameObject($"BuildButton_{index}");
        btnGo.transform.SetParent(contentParent, false);

        var rect = btnGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(buttonSize, buttonSize);

        var image = btnGo.AddComponent<Image>();
        image.sprite = GetIconFor(index);
        image.color = normalColor;
        image.type = Image.Type.Sliced;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = image;

        // Text-Label obenauf (Prefab-Name oder Nummer)
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = labelFontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;

        string niceName = prefab != null ? prefab.name : $"B{index + 1}";
        label.text = niceName;

        int capturedIndex = index;
        btn.onClick.AddListener(() => OnBuildButtonClicked(capturedIndex));

        return btn;
    }

    private Sprite GetIconFor(int index)
    {
        if (buildingIcons != null && index >= 0 && index < buildingIcons.Length && buildingIcons[index] != null)
            return buildingIcons[index];
        return defaultButtonSprite;
    }

    private void OnBuildButtonClicked(int index)
    {
        _currentIndex = index;
        buildingPlacer.EnterBuildMode(index);
        UpdateButtonHighlight();
    }

    private void UpdateButtonHighlight()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] == null) continue;
            var img = _buttons[i].targetGraphic as Image;
            if (img == null) continue;
            img.color = (i == _currentIndex) ? selectedColor : normalColor;
        }
    }

    /// <summary>
    /// Von außen (z.B. BuildingTest mit B-Taste) aufrufen,
    /// um Baumenü und BuildMode gemeinsam ein/auszuschalten.
    /// </summary>
    public void ToggleMenu()
    {
        if (_visible)
        {
            SetVisible(false);
            buildingPlacer.CancelBuildMode();
        }
        else
        {
            SetVisible(true);
            buildingPlacer.EnterBuildMode(_currentIndex);
        }
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        if (barRoot != null)
        {
            barRoot.gameObject.SetActive(visible);
        }
    }
}


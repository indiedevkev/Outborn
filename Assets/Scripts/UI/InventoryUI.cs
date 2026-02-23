using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Outborn.Inventory;

/// <summary>
/// Zeigt das Kolonie-Inventar oben links als schmalen Streifen (RimWorld-Style):
/// pro Ressource eine Zeile mit Icon links und Menge rechts.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Optional: eigene Referenzen")]
    [SerializeField] private RectTransform stripRoot;
    [SerializeField] private RectTransform contentParent;

    [Header("Layout")]
    [SerializeField] private float stripWidth = 90f;
    [SerializeField] private float rowHeight = 28f;
    [SerializeField] private float iconSize = 24f;
    [SerializeField] private float padding = 8f;
    [SerializeField] private int fontSize = 14;
    [Tooltip("Hintergrund dezent (wie im Screenshot) oder aus")]
    [SerializeField] private bool subtleBackground = true;
    [SerializeField] private float backgroundAlpha = 0.75f;

    [Header("Placeholder-Farben für Icons (später durch Sprites ersetzen)")]
    [SerializeField] private Color woodColor = new Color(0.6f, 0.35f, 0.15f);
    [SerializeField] private Color stoneColor = new Color(0.5f, 0.5f, 0.55f);
    [SerializeField] private Color steelColor = new Color(0.6f, 0.65f, 0.7f);
    [SerializeField] private Color foodColor = new Color(0.85f, 0.75f, 0.3f);

    private static readonly ResourceType[] ResourceOrder = { ResourceType.Wood, ResourceType.Stone, ResourceType.Steel, ResourceType.Food };

    [Header("Test-Button")]
    [SerializeField] private bool showTestButton = true;
    [SerializeField] private string testButtonLabel = "+10 Test";

    private Canvas _canvas;
    private ColonyInventory _inventory;
    private readonly List<(Image icon, TMP_Text countLabel)> _rows = new List<(Image, TMP_Text)>();

    void Start()
    {
        _inventory = ColonyInventory.Instance;
        if (_inventory == null)
        {
            Debug.LogWarning("[InventoryUI] ColonyInventory.Instance nicht gefunden.");
            return;
        }

        EnsureUI();
        BuildRows();
        if (showTestButton) CreateTestButton();
        _inventory.OnInventoryChanged += RefreshCounts;
        RefreshCounts();
    }

    void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged -= RefreshCounts;
    }

    private void EnsureUI()
    {
        if (stripRoot != null && contentParent != null)
            return;

        var existing = FindFirstObjectByType<Canvas>();
        if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
            _canvas = existing;
        else
        {
            var canvasGo = new GameObject("InventoryCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (stripRoot == null)
        {
            var stripGo = new GameObject("InventoryStrip");
            stripGo.transform.SetParent(_canvas.transform, false);

            var rect = stripGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(padding, -padding);
            int rowCount = ResourceOrder.Length;
            float totalHeight = padding * 2f + rowCount * rowHeight;
            if (showTestButton) totalHeight += rowHeight + 4f;
            rect.sizeDelta = new Vector2(stripWidth, totalHeight);
            stripRoot = rect;

            if (subtleBackground)
            {
                var image = stripGo.AddComponent<Image>();
                image.color = new Color(0.08f, 0.08f, 0.1f, backgroundAlpha);
            }
        }

        if (contentParent == null)
        {
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(stripRoot, false);

            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            contentParent = contentRect;
        }
    }

    private void BuildRows()
    {
        foreach (var type in ResourceOrder)
        {
            var row = CreateRow(type);
            _rows.Add(row);
        }
    }

    private (Image icon, TMP_Text countLabel) CreateRow(ResourceType type)
    {
        var rowGo = new GameObject($"Row_{type}");
        rowGo.transform.SetParent(contentParent, false);

        var rowRect = rowGo.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, rowHeight);

        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        // Icon (Placeholder – später Sprite zuweisen)
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(rowGo.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        var iconImage = iconGo.AddComponent<Image>();
        iconImage.color = GetColorFor(type);

        // Menge rechts
        var labelGo = new GameObject("Count");
        labelGo.transform.SetParent(rowGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(48f, rowHeight);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineRight;

        return (iconImage, label);
    }

    private Color GetColorFor(ResourceType type)
    {
        return type switch
        {
            ResourceType.Wood => woodColor,
            ResourceType.Stone => stoneColor,
            ResourceType.Steel => steelColor,
            ResourceType.Food => foodColor,
            _ => Color.gray
        };
    }

    private void RefreshCounts()
    {
        if (_inventory == null) return;
        for (int i = 0; i < ResourceOrder.Length && i < _rows.Count; i++)
        {
            int count = _inventory.GetCount(ResourceOrder[i]);
            _rows[i].countLabel.text = count.ToString();
        }
    }

    private void CreateTestButton()
    {
        if (contentParent == null) return;

        var btnGo = new GameObject("TestButton");
        btnGo.transform.SetParent(contentParent, false);

        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(0f, rowHeight);

        var btn = btnGo.AddComponent<Button>();
        var btnImage = btnGo.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
        btn.targetGraphic = btnImage;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = testButtonLabel;
        label.fontSize = Mathf.Max(10, fontSize - 2);
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;

        btn.onClick.AddListener(AddTestResources);
    }

    /// <summary> Test-Button oder Debug: +10 Wood/Stone/Food, +5 Steel. </summary>
    public void AddTestResources()
    {
        if (ColonyInventory.Instance == null) return;
        ColonyInventory.Instance.Add(ResourceType.Wood, 10);
        ColonyInventory.Instance.Add(ResourceType.Stone, 10);
        ColonyInventory.Instance.Add(ResourceType.Steel, 5);
        ColonyInventory.Instance.Add(ResourceType.Food, 10);
    }
}

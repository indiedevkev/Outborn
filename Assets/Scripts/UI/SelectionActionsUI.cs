using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Zeigt unten (oder an konfigurierbarer Stelle) Aktionen für die aktuelle Auswahl:
/// Baum ausgewählt → "[P] Holz hacken"
/// Stein ausgewählt → "[V] Minen"
/// Optional: Klickbare Buttons für dieselben Aktionen.
/// </summary>
public class SelectionActionsUI : MonoBehaviour
{
    [Header("Referenzen")]
    [SerializeField] private SelectionManager selectionManager;

    [Header("Optional: eigenes Panel/Label")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Button chopButton;
    [SerializeField] private Button mineButton;

    [Header("Layout (bei Auto-Erstellung)")]
    [SerializeField] private float panelWidth = 320f;
    [SerializeField] private float panelHeight = 56f;
    [SerializeField] private float padding = 12f;
    [SerializeField] private int fontSize = 14;

    private Canvas _canvas;
    private bool _hasTree;
    private bool _hasStone;

    void Start()
    {
        if (selectionManager == null)
            selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null)
        {
            enabled = false;
            return;
        }
        EnsureUI();
        RefreshVisibility();
    }

    void Update()
    {
        RefreshState();
    }

    private void EnsureUI()
    {
        if (panelRoot != null && labelText != null) return;

        var existing = FindFirstObjectByType<Canvas>();
        if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
            _canvas = existing;
        else
        {
            var go = new GameObject("SelectionActionsCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();
        }

        if (panelRoot == null)
        {
            var panelGo = new GameObject("SelectionActionsPanel");
            panelGo.transform.SetParent(_canvas.transform, false);
            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 100f);
            rect.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelRoot = rect;
            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        }

        if (labelText == null)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(panelRoot, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(padding, padding);
            labelRect.offsetMax = new Vector2(-padding, -padding);
            labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = fontSize;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void RefreshState()
    {
        _hasTree = false;
        _hasStone = false;
        if (selectionManager == null) return;
        foreach (var sel in selectionManager.GetSelectedObjects())
        {
            var mb = sel as MonoBehaviour;
            if (mb == null) continue;
            if (mb.GetComponent<ChoppableTree>() != null) _hasTree = true;
            if (mb.GetComponent<MineableStone>() != null) _hasStone = true;
        }
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (panelRoot == null) return;

        bool show = _hasTree || _hasStone;
        panelRoot.gameObject.SetActive(show);

        if (!show) return;

        var sb = new StringBuilder();
        if (_hasTree) sb.Append("[P] Holz hacken");
        if (_hasTree && _hasStone) sb.Append("  ·  ");
        if (_hasStone) sb.Append("[V] Minen");

        if (labelText != null)
            labelText.text = sb.ToString();

        if (chopButton != null) chopButton.gameObject.SetActive(_hasTree);
        if (mineButton != null) mineButton.gameObject.SetActive(_hasStone);
    }

    /// <summary> Von Button aufrufbar: gleiche Wirkung wie P. </summary>
    public void OnChopClicked()
    {
        if (selectionManager == null) return;
        foreach (var sel in selectionManager.GetSelectedObjects())
        {
            var tree = (sel as MonoBehaviour)?.GetComponent<ChoppableTree>();
            if (tree == null || tree.HasChopJob) continue;
            tree.SetJobCreated(true);
            JobManager.Instance?.AddJob(new HarvestJob(tree));
            break;
        }
    }

    /// <summary> Von Button aufrufbar: gleiche Wirkung wie V. </summary>
    public void OnMineClicked()
    {
        if (selectionManager == null) return;
        foreach (var sel in selectionManager.GetSelectedObjects())
        {
            var stone = (sel as MonoBehaviour)?.GetComponent<MineableStone>();
            if (stone == null || stone.HasMineJob) continue;
            stone.SetJobCreated(true);
            JobManager.Instance?.AddJob(new MineJob(stone));
            break;
        }
    }
}

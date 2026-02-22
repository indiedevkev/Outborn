using UnityEngine;
using UnityEngine.UI;  // ← NEU!
using TMPro;           // ← NEU!

public class BuildJob : Job
{
    public GameObject buildingPrefab;
    public Vector3Int gridPosition;
    public Quaternion buildingRotation;  // ← NEU: Rotation speichern!
    
    private GameObject constructionSite;
    private ConstructionSite constructionScript;

    public BuildJob(Vector3 location, Vector3Int gridPos, GameObject prefab, Quaternion rotation, float buildTime = 5f) 
        : base(JobType.Build, location, buildTime)
    {
        buildingPrefab = prefab;
        gridPosition = gridPos;
        buildingRotation = rotation;  // ← NEU!
        priority = JobPriority.Normal;
    }

    public override void StartJob()
    {
        base.StartJob();
        
        // Create construction site visual
        CreateConstructionSite();
    }

void CreateConstructionSite()
{
    if (buildingPrefab == null) return;
    
    // Create construction site parent
    constructionSite = new GameObject("ConstructionSite");
    constructionSite.transform.position = workLocation;
    constructionSite.transform.rotation = buildingRotation;
    
    // Create ghost building FIRST
    GameObject ghost = Object.Instantiate(buildingPrefab, constructionSite.transform);
    ghost.name = "Ghost";
    ghost.transform.localPosition = Vector3.zero;
    ghost.transform.localRotation = Quaternion.identity;
    
    // Remove colliders from ghost
    Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
    foreach (Collider col in colliders)
    {
        Object.Destroy(col);
    }
    
    // Create progress bar UI
    GameObject progressBar = CreateProgressBar(constructionSite.transform);
    Canvas canvas = progressBar.GetComponent<Canvas>();
    Image fillImage = progressBar.transform.Find("Background/Fill").GetComponent<Image>();
    TextMeshProUGUI text = progressBar.transform.Find("Text").GetComponent<TextMeshProUGUI>();
    
    // NOW add ConstructionSite script and set references!
    constructionScript = constructionSite.AddComponent<ConstructionSite>();
    
    // Use reflection to set private fields
    var ghostField = typeof(ConstructionSite).GetField("ghostBuilding", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    ghostField?.SetValue(constructionScript, ghost);
    
    var canvasField = typeof(ConstructionSite).GetField("progressCanvas", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    canvasField?.SetValue(constructionScript, canvas);
    
    var fillField = typeof(ConstructionSite).GetField("progressBarFill", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    fillField?.SetValue(constructionScript, fillImage);
    
    var textField = typeof(ConstructionSite).GetField("progressText", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    textField?.SetValue(constructionScript, text);
    
    // ← NEU! Initialize mit 0%!
    constructionScript.SetProgress(0f);
    
    Debug.Log($"Construction site created at {workLocation} with 0% progress!");
}

GameObject CreateProgressBar(Transform parent)
{
    // Create Canvas
    GameObject canvasObj = new GameObject("ProgressCanvas");
    canvasObj.transform.SetParent(parent);
    canvasObj.transform.localPosition = Vector3.up * 3f;
    canvasObj.transform.localRotation = Quaternion.identity;
    canvasObj.transform.localScale = Vector3.one * 0.01f;  // ← WICHTIG! Viel kleiner!
    
    Canvas canvas = canvasObj.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;
    
    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
    scaler.dynamicPixelsPerUnit = 10;
    
    RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
    canvasRect.sizeDelta = new Vector2(200, 40);
    
    // Background
    GameObject bgObj = new GameObject("Background");
    bgObj.transform.SetParent(canvasObj.transform, false);
    
    Image bgImage = bgObj.AddComponent<Image>();
    bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    RectTransform bgRect = bgObj.GetComponent<RectTransform>();
    bgRect.anchorMin = new Vector2(0.5f, 0.5f);
    bgRect.anchorMax = new Vector2(0.5f, 0.5f);
    bgRect.pivot = new Vector2(0.5f, 0.5f);
    bgRect.sizeDelta = new Vector2(180, 30);
    bgRect.anchoredPosition = Vector2.zero;
    
    // Fill
    GameObject fillObj = new GameObject("Fill");
    fillObj.transform.SetParent(bgObj.transform, false);
    
    Image fillImage = fillObj.AddComponent<Image>();
    fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
    fillImage.type = Image.Type.Filled;
    fillImage.fillMethod = Image.FillMethod.Horizontal;
    fillImage.fillOrigin = 0; // Left to right
    fillImage.fillAmount = 0f;
    
    RectTransform fillRect = fillObj.GetComponent<RectTransform>();
    fillRect.anchorMin = new Vector2(0.5f, 0.5f);
    fillRect.anchorMax = new Vector2(0.5f, 0.5f);
    fillRect.pivot = new Vector2(0.5f, 0.5f);
    fillRect.sizeDelta = new Vector2(170, 20);
    fillRect.anchoredPosition = Vector2.zero;
    
    // Text
    GameObject textObj = new GameObject("Text");
    textObj.transform.SetParent(canvasObj.transform, false);
    
    TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
    text.text = "0%";
    text.fontSize = 24;
    text.alignment = TextAlignmentOptions.Center;
    text.color = Color.white;
    text.fontStyle = FontStyles.Bold;
    
    RectTransform textRect = textObj.GetComponent<RectTransform>();
    textRect.anchorMin = new Vector2(0.5f, 0.5f);
    textRect.anchorMax = new Vector2(0.5f, 0.5f);
    textRect.pivot = new Vector2(0.5f, 0.5f);
    textRect.sizeDelta = new Vector2(180, 30);
    textRect.anchoredPosition = Vector2.zero;
    
    Debug.Log($"Progress bar created at {canvasObj.transform.position}");
    
    return canvasObj;
}

    public override void DoWork(float amount)
    {
        base.DoWork(amount);
        
        // Update construction visual progress
        if (constructionScript != null)
        {
            constructionScript.SetProgress(GetProgress());
        }
    }

    public override void CompleteJob()
    {
        base.CompleteJob();
        
        // Spawn actual building
        if (buildingPrefab != null)
        {
            GameObject building = Object.Instantiate(buildingPrefab, workLocation, buildingRotation);  // ← Use rotation!
            Debug.Log($"Building constructed at {workLocation}");
        }
        
        // Remove construction site
        if (constructionSite != null)
        {
            Object.Destroy(constructionSite);
        }
        
        // Mark grid as occupied
        GridManager gridManager = Object.FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.SetCellOccupied(gridPosition, true);
        }
    }

    public override void CancelJob()
    {
        base.CancelJob();
        
        // Remove construction site
        if (constructionSite != null)
        {
            Object.Destroy(constructionSite);
        }
        
        // Free grid cell
        GridManager gridManager = Object.FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.SetCellOccupied(gridPosition, false);
        }
    }
}
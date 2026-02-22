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
    
    Debug.Log("=== START CreateConstructionSite ===");
    
    // Create construction site parent
    constructionSite = new GameObject("ConstructionSite");
    constructionSite.transform.position = workLocation;
    constructionSite.transform.rotation = buildingRotation;
    
    Debug.Log("✓ ConstructionSite GameObject created");
    
    // Create ghost building
    GameObject ghost = Object.Instantiate(buildingPrefab, constructionSite.transform);
    ghost.name = "Ghost";
    ghost.transform.localPosition = Vector3.zero;
    ghost.transform.localRotation = Quaternion.identity;
    
    Debug.Log($"✓ Ghost created: {ghost.name}");
    
    // Remove colliders from ghost
    Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
    foreach (Collider col in colliders)
    {
        Object.Destroy(col);
    }
    
    Debug.Log($"✓ Removed {colliders.Length} colliders from ghost");
    
    // Create progress bar UI
    GameObject progressBar = CreateProgressBar(constructionSite.transform);
    Debug.Log($"✓ Progress bar created: {progressBar.name}");
    
    Canvas canvas = progressBar.GetComponent<Canvas>();
    Debug.Log($"✓ Canvas: {(canvas != null ? "Found" : "NULL!")}");
    
    Transform fillTransform = progressBar.transform.Find("Background/Fill");
    Debug.Log($"✓ Fill Transform: {(fillTransform != null ? "Found" : "NULL!")}");
    
    Image fillImage = fillTransform?.GetComponent<Image>();
    Debug.Log($"✓ Fill Image: {(fillImage != null ? $"Found (type={fillImage.type}, fillAmount={fillImage.fillAmount})" : "NULL!")}");
    
    Transform textTransform = progressBar.transform.Find("Text");
    Debug.Log($"✓ Text Transform: {(textTransform != null ? "Found" : "NULL!")}");
    
    TextMeshProUGUI text = textTransform?.GetComponent<TextMeshProUGUI>();
    Debug.Log($"✓ Text Component: {(text != null ? $"Found (text='{text.text}')" : "NULL!")}");
    
    // Add ConstructionSite script
    constructionScript = constructionSite.AddComponent<ConstructionSite>();
    Debug.Log("✓ ConstructionSite script added");
    
    // Call Setup
    if (constructionScript != null && fillImage != null && text != null)
    {
        constructionScript.Setup(ghost, canvas, fillImage, text);
        Debug.Log("✓ Setup() called successfully!");
    }
    else
    {
        Debug.LogError($"❌ Setup FAILED! Script={constructionScript != null}, Fill={fillImage != null}, Text={text != null}");
    }
    
    Debug.Log("=== END CreateConstructionSite ===");
}

GameObject CreateProgressBar(Transform parent)
{
    // Create Canvas
    GameObject canvasObj = new GameObject("ProgressCanvas");
    canvasObj.transform.SetParent(parent);
    canvasObj.transform.localPosition = Vector3.up * 3f;
    canvasObj.transform.localRotation = Quaternion.identity;
    canvasObj.transform.localScale = Vector3.one * 0.01f;
    
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
    
    // Fill - ← WICHTIG! Hier ist der Fix!
    GameObject fillObj = new GameObject("Fill");
    fillObj.transform.SetParent(bgObj.transform, false);
    
    Image fillImage = fillObj.AddComponent<Image>();
    fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
    
    // ← KRITISCH! In der richtigen Reihenfolge setzen!
    fillImage.type = Image.Type.Filled;
    fillImage.fillMethod = Image.FillMethod.Horizontal;
    fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;  // ← Von links nach rechts!
    fillImage.fillAmount = 0f;  // ← Start bei 0!
    
    RectTransform fillRect = fillObj.GetComponent<RectTransform>();
    fillRect.anchorMin = Vector2.zero;  // ← WICHTIG! Links unten
    fillRect.anchorMax = Vector2.one;   // ← WICHTIG! Rechts oben (fill parent!)
    fillRect.pivot = new Vector2(0.5f, 0.5f);
    fillRect.sizeDelta = Vector2.zero;  // ← Full size of parent!
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
    
    Debug.Log($"Progress bar created with Fill type: {fillImage.type}, fillAmount: {fillImage.fillAmount}");
    
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
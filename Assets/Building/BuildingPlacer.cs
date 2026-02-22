using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject[] buildingPrefabs;
    
    [Header("Settings")]
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Color validPlacementColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float buildTime = 5f;  // ← NEU!
    
    [Header("Input")]
    [SerializeField] private bool buildMode = false;
    
    private GameObject currentPreview;
    private GameObject currentBuildingPrefab;
    private int currentBuildingIndex = 0;
    private bool canPlace = false;
    private Material[] originalMaterials;
    private Renderer[] previewRenderers;

    void Awake()
    {
        if (gridManager == null)
            gridManager = Object.FindFirstObjectByType<GridManager>();
    }

    void Update()
    {
        if (buildMode && currentBuildingPrefab != null)
        {
            UpdatePreview();
        }
    }

    // Input System Callbacks
    public void OnPlace(InputAction.CallbackContext context)
    {
        if (context.performed && buildMode && canPlace)
        {
            PlaceBuilding();
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed && buildMode)
        {
            CancelBuildMode();
        }
    }

    public void OnRotateBuilding(InputAction.CallbackContext context)
    {
        if (context.performed && buildMode && currentPreview != null)
        {
            currentPreview.transform.Rotate(Vector3.up, 90f);
        }
    }

    public void OnNextBuilding(InputAction.CallbackContext context)
    {
        if (context.performed && buildMode)
        {
            CycleBuilding(1);
        }
    }

    public void OnPreviousBuilding(InputAction.CallbackContext context)
    {
        if (context.performed && buildMode)
        {
            CycleBuilding(-1);
        }
    }

    void UpdatePreview()
    {
        if (currentPreview == null) return;

        Vector3Int cellPos = gridManager.GetCurrentHoverCell();
        Vector3 worldPos = gridManager.CellToWorld(cellPos);
        
        currentPreview.transform.position = worldPos;

        // Check if placement is valid
        canPlace = gridManager.IsValidCell(cellPos) && !gridManager.IsCellOccupied(cellPos);

        // Update preview color
        Color previewColor = canPlace ? validPlacementColor : invalidPlacementColor;
        UpdatePreviewColor(previewColor);
    }

    void UpdatePreviewColor(Color color)
    {
        if (previewRenderers == null) return;

        foreach (Renderer renderer in previewRenderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = color;
            }
        }
    }

    void PlaceBuilding()  // ← KOMPLETT NEU!
    {
        Vector3Int cellPos = gridManager.GetCurrentHoverCell();
        
        if (!gridManager.IsValidCell(cellPos) || gridManager.IsCellOccupied(cellPos))
        {
            Debug.LogWarning("Cannot place building here!");
            return;
        }

        // CREATE BUILD JOB instead of instant building!
        if (JobManager.Instance != null)
        {
            BuildJob buildJob = new BuildJob(
                currentPreview.transform.position,  // Use preview position (includes rotation!)
                cellPos,
                currentBuildingPrefab,
                buildTime
            );
            
            JobManager.Instance.AddJob(buildJob);
            gridManager.SetCellOccupied(cellPos, true);
            
            Debug.Log($"Build job created for {currentBuildingPrefab.name} at {cellPos}");
        }
        else
        {
            Debug.LogError("JobManager not found! Create JobManager GameObject in scene.");
        }
    }

    void CycleBuilding(int direction)
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        currentBuildingIndex += direction;
        
        if (currentBuildingIndex >= buildingPrefabs.Length)
            currentBuildingIndex = 0;
        else if (currentBuildingIndex < 0)
            currentBuildingIndex = buildingPrefabs.Length - 1;

        SetCurrentBuilding(buildingPrefabs[currentBuildingIndex]);
    }

    public void EnterBuildMode(GameObject buildingPrefab)
    {
        buildMode = true;
        SetCurrentBuilding(buildingPrefab);
        Debug.Log("Entered Build Mode");
    }

    public void EnterBuildMode(int buildingIndex = 0)
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0)
        {
            Debug.LogWarning("No building prefabs assigned!");
            return;
        }

        currentBuildingIndex = Mathf.Clamp(buildingIndex, 0, buildingPrefabs.Length - 1);
        EnterBuildMode(buildingPrefabs[currentBuildingIndex]);
    }

    void SetCurrentBuilding(GameObject prefab)
    {
        // Destroy old preview
        if (currentPreview != null)
            Destroy(currentPreview);

        currentBuildingPrefab = prefab;

        // Create preview
        currentPreview = Instantiate(prefab);
        currentPreview.name = "BuildingPreview";

        // Get all renderers
        previewRenderers = currentPreview.GetComponentsInChildren<Renderer>();

        // Setup preview materials
        foreach (Renderer renderer in previewRenderers)
        {
            Material[] mats = new Material[renderer.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats[i].color = validPlacementColor;
                mats[i].SetFloat("_Surface", 1); // Transparent
                mats[i].SetFloat("_Blend", 0); // Alpha
                mats[i].SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mats[i].SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mats[i].SetFloat("_ZWrite", 0);
                mats[i].renderQueue = 3000;
            }
            renderer.materials = mats;
        }

        // Remove colliders from preview
        Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            Destroy(col);
        }
    }

    public void CancelBuildMode()
    {
        buildMode = false;
        
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        Debug.Log("Exited Build Mode");
    }

    public void ToggleBuildMode()
    {
        if (buildMode)
            CancelBuildMode();
        else
            EnterBuildMode(currentBuildingIndex);
    }
}
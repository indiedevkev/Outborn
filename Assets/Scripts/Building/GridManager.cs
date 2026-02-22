using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private float cellSize = 1f;

    [Header("Visual Settings")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Material gridMaterial;

    [Header("Hover Settings")]
    [SerializeField] private GameObject hoverIndicator;
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    private Camera mainCamera;
    private Vector3Int currentHoverCell;
    private GameObject hoverInstance;

    // Grid data - später für Gebäude/Blockierung
    private bool[,] occupiedCells;

    void Awake()
    {
        occupiedCells = new bool[gridWidth, gridHeight];
        mainCamera = Camera.main;

        // Create hover indicator if none assigned
        if (hoverIndicator == null)
        {
            CreateDefaultHoverIndicator();
        }

        // Instantiate hover indicator
        hoverInstance = Instantiate(hoverIndicator);
        hoverInstance.SetActive(false);
    }

    void Update()
    {
        UpdateHoverIndicator();
    }

    void UpdateHoverIndicator()
    {
        Ray ray = mainCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());

        // Create a virtual ground plane at Y=0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            // Get world position where ray hits the plane
            Vector3 worldPos = ray.GetPoint(distance);
            Vector3Int cellPos = WorldToCell(worldPos);

            if (IsValidCell(cellPos))
            {
                currentHoverCell = cellPos;
                Vector3 cellWorldPos = CellToWorld(cellPos);

                hoverInstance.transform.position = cellWorldPos;
                hoverInstance.SetActive(true);

                // Change color based on availability
                Renderer renderer = hoverInstance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = IsCellOccupied(cellPos) ? invalidColor : validColor;
                }
            }
            else
            {
                hoverInstance.SetActive(false);
            }
        }
        else
        {
            hoverInstance.SetActive(false);
        }
    }

    void CreateDefaultHoverIndicator()
    {
        hoverIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hoverIndicator.transform.localScale = new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f);

        // Create transparent material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = validColor;
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0); // Alpha
        mat.SetFloat("_AlphaClip", 0);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;

        hoverIndicator.GetComponent<Renderer>().material = mat;

        // Remove collider so it doesn't interfere with raycasts
        Destroy(hoverIndicator.GetComponent<Collider>());

        hoverIndicator.SetActive(false);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        // Center the grid around (0,0)
        int x = Mathf.FloorToInt((worldPos.x / cellSize) + (gridWidth / 2));
        int z = Mathf.FloorToInt((worldPos.z / cellSize) + (gridHeight / 2));

        return new Vector3Int(x, 0, z);
    }

    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        // Center the grid around (0,0)
        float x = (cellPos.x - (gridWidth / 2)) * cellSize + (cellSize / 2f);
        float z = (cellPos.z - (gridHeight / 2)) * cellSize + (cellSize / 2f);

        return new Vector3(x, 0f, z);
    }

    public bool IsValidCell(Vector3Int cellPos)
    {
        return cellPos.x >= 0 && cellPos.x < gridWidth &&
               cellPos.z >= 0 && cellPos.z < gridHeight;
    }

    public bool IsCellOccupied(Vector3Int cellPos)
    {
        if (!IsValidCell(cellPos)) return true;
        return occupiedCells[cellPos.x, cellPos.z];
    }

    public void SetCellOccupied(Vector3Int cellPos, bool occupied)
    {
        if (IsValidCell(cellPos))
        {
            occupiedCells[cellPos.x, cellPos.z] = occupied;
        }
    }

    public Vector3Int GetCurrentHoverCell()
    {
        return currentHoverCell;
    }

    // Visualize grid in Scene view
    void OnDrawGizmos()
    {
        if (!showGrid) return;

        Gizmos.color = gridColor;

        // Draw grid lines
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(x * cellSize, 0, 0);
            Vector3 end = new Vector3(x * cellSize, 0, gridHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }

        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = new Vector3(0, 0, z * cellSize);
            Vector3 end = new Vector3(gridWidth * cellSize, 0, z * cellSize);
            Gizmos.DrawLine(start, end);
        }
    }

}
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PawnCommandHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private Camera mainCamera;

    [Header("Command Feedback")]
    [SerializeField] private GameObject moveMarkerPrefab;
    [SerializeField] private float markerLifetime = 1f;

    private GameObject currentMarker;

    void Awake()
    {
        if (selectionManager == null)
            selectionManager = GameObject.FindFirstObjectByType<SelectionManager>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        // Right click to give move command
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleMoveCommand();
        }
    }

   void HandleMoveCommand()
{
    // Get selected objects
    List<ISelectable> selected = selectionManager.GetSelectedObjects();
    
    Debug.Log($"Selected objects count: {selected.Count}");
    
    if (selected.Count == 0)
    {
        Debug.Log("No objects selected");
        return;
    }
    
    // Raycast to get ground position
    Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
    
    if (Physics.Raycast(ray, out RaycastHit hit))
    {
        Vector3 destination = hit.point;
        
        Debug.Log($"Move command to {destination}");
        
        // Command all selected pawns to move
        int pawnCount = 0;
        foreach (ISelectable selectable in selected)
        {
            // Cast to MonoBehaviour first
            MonoBehaviour mono = selectable as MonoBehaviour;
            if (mono != null)
            {
                // Try to get Pawn component
                Pawn pawn = mono.GetComponent<Pawn>();
                if (pawn != null)
                {
                    // Offset position slightly for multiple pawns
                    Vector3 offset = GetFormationOffset(pawnCount, selected.Count);
                    pawn.MoveTo(destination + offset);
                    pawnCount++;
                    Debug.Log($"Commanding {pawn.GetPawnName()} to move");
                }
                else
                {
                    Debug.LogWarning($"Selected object {mono.name} has no Pawn component!");
                }
            }
        }
        
        // Show visual feedback
        if (pawnCount > 0)
        {
            ShowMoveMarker(destination);
        }
        
        Debug.Log($"Commanded {pawnCount} pawns to move");
    }
    else
    {
        Debug.Log("Raycast hit nothing!");
    }
}

    Vector3 GetFormationOffset(int index, int totalCount)
    {
        if (totalCount == 1) return Vector3.zero;

        // Simple circle formation
        float angle = (360f / totalCount) * index;
        float radius = 1f + (totalCount * 0.2f);

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        return new Vector3(x, 0, z);
    }

    void ShowMoveMarker(Vector3 position)
    {
        // Destroy old marker
        if (currentMarker != null)
        {
            Destroy(currentMarker);
        }

        // Create marker
        if (moveMarkerPrefab != null)
        {
            currentMarker = Instantiate(moveMarkerPrefab, position, Quaternion.identity);
        }
        else
        {
            // Default marker
            currentMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            currentMarker.transform.position = position;
            currentMarker.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = Color.green;
            currentMarker.GetComponent<Renderer>().material = mat;

            Destroy(currentMarker.GetComponent<Collider>());
        }

        // Auto destroy after lifetime
        Destroy(currentMarker, markerLifetime);
    }
}
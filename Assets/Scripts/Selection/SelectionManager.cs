using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private bool allowMultiSelect = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color selectionBoxColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color selectionBoxBorderColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private float borderWidth = 2f;
    
    private Camera mainCamera;
    private Vector2 dragStartPos;
    private Vector2 dragCurrentPos;
    private bool isDragging = false;
    
    private List<ISelectable> selectedObjects = new List<ISelectable>();
    
    // For GUI drawing
    private Rect selectionBox;
    private Texture2D whiteTexture;

    void Awake()
    {
        mainCamera = Camera.main;
        
        // Create white texture for GUI
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    void Update()
    {
        HandleSelection();
    }

    void HandleSelection()
    {
        // Check for mouse button down
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            dragStartPos = Mouse.current.position.ReadValue();
            dragCurrentPos = dragStartPos;
            isDragging = false;
            
            // Clear selection if not holding shift
            if (!Keyboard.current.leftShiftKey.isPressed && !Keyboard.current.rightShiftKey.isPressed)
            {
                ClearSelection();
            }
        }
        
        // Check for mouse button held
        if (Mouse.current.leftButton.isPressed)
        {
            dragCurrentPos = Mouse.current.position.ReadValue();
            
            // Start dragging if moved enough
            if (!isDragging)
            {
                float dragDistance = Vector2.Distance(dragStartPos, dragCurrentPos);
                if (dragDistance > 5f)
                {
                    isDragging = true;
                    Debug.Log("Started dragging!");
                }
            }
        }
        
        // Check for mouse button released
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            dragCurrentPos = Mouse.current.position.ReadValue();
            
            // Check if it was a click or drag
            float dragDistance = Vector2.Distance(dragStartPos, dragCurrentPos);
            
            Debug.Log($"Released! Distance: {dragDistance}, isDragging: {isDragging}");
            
            if (dragDistance < 5f) // Small threshold for click
            {
                HandleSingleClick();
            }
            else if (isDragging)
            {
                HandleDragSelect();
            }
            
            isDragging = false;
        }
    }

    void HandleSingleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(dragStartPos);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableLayer))
        {
            ISelectable selectable = hit.collider.GetComponent<ISelectable>();
            
            if (selectable != null)
            {
                // If shift is held, add to selection, otherwise replace
                bool addToSelection = Keyboard.current.leftShiftKey.isPressed || 
                                     Keyboard.current.rightShiftKey.isPressed;
                
                if (addToSelection)
                {
                    if (selectedObjects.Contains(selectable))
                    {
                        DeselectObject(selectable);
                    }
                    else
                    {
                        SelectObject(selectable);
                    }
                }
                else
                {
                    ClearSelection();
                    SelectObject(selectable);
                }
            }
        }
    }

    void HandleDragSelect()
    {
        if (!allowMultiSelect) return;
        
        Debug.Log("HandleDragSelect called!");
        
        // Get selection rectangle
        Rect selectionRect = GetSelectionRect();
        
        Debug.Log($"Selection Rect: {selectionRect}");
        
        // Find all selectable objects
        SelectableObject[] allSelectables = GameObject.FindObjectsByType<SelectableObject>(FindObjectsSortMode.None);
        
        Debug.Log($"Found {allSelectables.Length} selectable objects");
        
        int selectedCount = 0;
        
        foreach (SelectableObject selectable in allSelectables)
        {
            // Get screen position of object
            Vector3 worldPos = selectable.transform.position;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            
            // Check if behind camera
            if (screenPos.z < 0)
            {
                Debug.Log($"{selectable.name} is behind camera");
                continue;
            }
            
            // Check if in selection rectangle (screenPos.y is already in correct screen space)
            if (selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
            {
                Debug.Log($"{selectable.name} is IN selection rect! ScreenPos: {screenPos}");
                
                if (!selectedObjects.Contains(selectable))
                {
                    SelectObject(selectable);
                    selectedCount++;
                }
            }
            else
            {
                Debug.Log($"{selectable.name} is NOT in selection rect. ScreenPos: {screenPos}");
            }
        }
        
        Debug.Log($"Selected {selectedCount} objects via drag");
    }

    Rect GetSelectionRect()
    {
        // dragStartPos and dragCurrentPos are already in correct screen space (Y = 0 at bottom)
        Vector2 min = new Vector2(
            Mathf.Min(dragStartPos.x, dragCurrentPos.x),
            Mathf.Min(dragStartPos.y, dragCurrentPos.y)
        );
        
        Vector2 max = new Vector2(
            Mathf.Max(dragStartPos.x, dragCurrentPos.x),
            Mathf.Max(dragStartPos.y, dragCurrentPos.y)
        );
        
        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    void SelectObject(ISelectable selectable)
    {
        selectedObjects.Add(selectable);
        selectable.OnSelected();
        Debug.Log($"Selected: {(selectable as MonoBehaviour).gameObject.name}");
    }

    void DeselectObject(ISelectable selectable)
    {
        selectedObjects.Remove(selectable);
        selectable.OnDeselected();
        Debug.Log($"Deselected: {(selectable as MonoBehaviour).gameObject.name}");
    }

    public void ClearSelection()
    {
        foreach (ISelectable selectable in selectedObjects)
        {
            selectable.OnDeselected();
        }
        selectedObjects.Clear();
    }

    public List<ISelectable> GetSelectedObjects()
    {
        return new List<ISelectable>(selectedObjects);
    }

    public int GetSelectedCount()
    {
        return selectedObjects.Count;
    }

    // Draw selection box
    void OnGUI()
    {
        if (isDragging)
        {
            // Calculate selection box in screen space (GUI uses Y = 0 at top)
            Vector2 start = new Vector2(dragStartPos.x, Screen.height - dragStartPos.y);
            Vector2 end = new Vector2(dragCurrentPos.x, Screen.height - dragCurrentPos.y);
            
            selectionBox = new Rect(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Abs(start.x - end.x),
                Mathf.Abs(start.y - end.y)
            );
            
            // Draw filled box
            GUI.color = selectionBoxColor;
            GUI.DrawTexture(selectionBox, whiteTexture);
            
            // Draw border
            GUI.color = selectionBoxBorderColor;
            
            // Top
            GUI.DrawTexture(new Rect(selectionBox.x, selectionBox.y, selectionBox.width, borderWidth), whiteTexture);
            // Bottom
            GUI.DrawTexture(new Rect(selectionBox.x, selectionBox.y + selectionBox.height - borderWidth, selectionBox.width, borderWidth), whiteTexture);
            // Left
            GUI.DrawTexture(new Rect(selectionBox.x, selectionBox.y, borderWidth, selectionBox.height), whiteTexture);
            // Right
            GUI.DrawTexture(new Rect(selectionBox.x + selectionBox.width - borderWidth, selectionBox.y, borderWidth, selectionBox.height), whiteTexture);
            
            GUI.color = Color.white;
        }
    }
}
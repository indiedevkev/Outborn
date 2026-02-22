using UnityEngine;

public class SelectableObject : MonoBehaviour, ISelectable
{
    [Header("Visual Feedback")]
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private float indicatorHeightOffset = 0.1f;
    
    private bool isSelected = false;
    private Renderer objectRenderer;
    private Color originalColor;
    private GameObject indicatorInstance;

    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }
    }

    public void OnSelected()
    {
        isSelected = true;
        
        // Create selection indicator if it doesn't exist
        if (indicatorInstance == null)
        {
            CreateSelectionIndicator();
        }
        
        if (indicatorInstance != null)
        {
            indicatorInstance.SetActive(true);
        }
        
        // Optional: Change color
        if (objectRenderer != null)
        {
            objectRenderer.material.color = selectedColor;
        }
    }

    public void OnDeselected()
    {
        isSelected = false;
        
        if (indicatorInstance != null)
        {
            indicatorInstance.SetActive(false);
        }
        
        // Restore original color
        if (objectRenderer != null)
        {
            objectRenderer.material.color = originalColor;
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    void CreateSelectionIndicator()
    {
        if (selectionIndicator != null)
        {
            indicatorInstance = Instantiate(selectionIndicator, transform);
            indicatorInstance.transform.localPosition = Vector3.up * indicatorHeightOffset;
        }
        else
        {
            // Create default indicator (ring/circle)
            indicatorInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicatorInstance.name = "SelectionIndicator";
            indicatorInstance.transform.SetParent(transform);
            
            // Position and scale
            indicatorInstance.transform.localPosition = Vector3.zero;
            indicatorInstance.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
            
            // Material
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = selectedColor;
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            indicatorInstance.GetComponent<Renderer>().material = mat;
            
            // Remove collider
            Destroy(indicatorInstance.GetComponent<Collider>());
        }
        
        indicatorInstance.SetActive(false);
    }
}
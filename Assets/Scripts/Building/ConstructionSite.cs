using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConstructionSite : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject ghostBuilding;
    [SerializeField] private Canvas progressCanvas;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI progressText;
    
    [Header("Settings")]
    [SerializeField] private Color constructionColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 0.9f;
    [SerializeField] private Vector3 progressBarOffset = new Vector3(0, 3f, 0);
    
    [Header("Animation")]
   [SerializeField] private float buildTime = 5f;  // ← Match with BuildJob!

    
    private Renderer[] ghostRenderers;
    private Material[] ghostMaterials;
    private float currentProgress = 0f;
    private float displayedProgress = 0f;
    private Camera mainCamera;

    // ← NEU! Public Setup Methode!
    public void Setup(GameObject ghost, Canvas canvas, Image fill, TextMeshProUGUI text)
    {
        ghostBuilding = ghost;
        progressCanvas = canvas;
        progressBarFill = fill;
        progressText = text;
        
        mainCamera = Camera.main;
        
        if (progressCanvas != null)
        {
            progressCanvas.worldCamera = mainCamera;
        }
        
        // Initialize fill to 0
        displayedProgress = 0f;
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 0f;
        }
        
        SetupGhostMaterials();
        SetProgress(0f);
        
        Debug.Log("ConstructionSite Setup complete!");
    }

void Update()
{
    // Progress bar always faces camera
    if (progressCanvas != null && mainCamera != null)
    {
        progressCanvas.transform.rotation = Quaternion.LookRotation(
            progressCanvas.transform.position - mainCamera.transform.position
        );
    }
    
    // Smooth fill
    if (progressBarFill != null)
    {
        // Linear fill based on build time
        if (displayedProgress < currentProgress)
        {
            float fillRate = 1f / 5f;  // 5 seconds to fill
            displayedProgress += Time.deltaTime * fillRate;
            displayedProgress = Mathf.Min(displayedProgress, currentProgress);
            progressBarFill.fillAmount = displayedProgress;
            
            // DEBUG - entferne das nach Test!
            Debug.Log($"Fill: {displayedProgress:F2} / {currentProgress:F2} | fillAmount: {progressBarFill.fillAmount:F2}");
        }
    }
}

    void SetupGhostMaterials()
    {
        if (ghostBuilding == null)
        {
            Debug.LogWarning("Ghost building is null!");
            return;
        }
        
        ghostRenderers = ghostBuilding.GetComponentsInChildren<Renderer>();
        
        if (ghostRenderers.Length == 0)
        {
            Debug.LogWarning("No renderers found on ghost building!");
            return;
        }
        
        ghostMaterials = new Material[ghostRenderers.Length];
        
        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Material originalMat = ghostRenderers[i].sharedMaterial;
            Material ghostMat = new Material(originalMat);
            
            // Try to enable transparency
            if (ghostMat.HasProperty("_Mode"))
            {
                ghostMat.SetFloat("_Mode", 3);
                ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetInt("_ZWrite", 0);
                ghostMat.DisableKeyword("_ALPHATEST_ON");
                ghostMat.EnableKeyword("_ALPHABLEND_ON");
                ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ghostMat.renderQueue = 3000;
            }
            else if (ghostMat.HasProperty("_Surface"))
            {
                ghostMat.SetFloat("_Surface", 1);
                ghostMat.SetFloat("_Blend", 0);
                ghostMat.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetFloat("_ZWrite", 0);
                ghostMat.renderQueue = 3000;
                ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                ghostMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            
            Color color = constructionColor;
            color.a = minAlpha;
            
            if (ghostMat.HasProperty("_Color"))
            {
                ghostMat.SetColor("_Color", color);
            }
            if (ghostMat.HasProperty("_BaseColor"))
            {
                ghostMat.SetColor("_BaseColor", color);
            }
            
            ghostRenderers[i].material = ghostMat;
            ghostMaterials[i] = ghostMat;
        }
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, currentProgress);
        UpdateGhostAlpha(alpha);
        
        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
        }
    }

    void UpdateGhostAlpha(float alpha)
    {
        if (ghostMaterials == null) return;
        
        foreach (Material mat in ghostMaterials)
        {
            if (mat != null)
            {
                Color color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
        }
    }

    public void CompleteBuild()
    {
        Destroy(gameObject);
    }
}
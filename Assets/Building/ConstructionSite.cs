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

    private Renderer[] ghostRenderers;
    private Material[] ghostMaterials;
    private float currentProgress = 0f;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        SetupGhostMaterials();

        if (progressCanvas != null)
        {
            progressCanvas.worldCamera = mainCamera;
        }
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
            // Get original material or create new one
            Material originalMat = ghostRenderers[i].sharedMaterial;

            // Create a copy with transparency
            Material ghostMat = new Material(originalMat);

            // Try to enable transparency (works with Standard and URP Lit)
            if (ghostMat.HasProperty("_Mode"))
            {
                // Standard shader
                ghostMat.SetFloat("_Mode", 3); // Transparent mode
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
                // URP Lit shader
                ghostMat.SetFloat("_Surface", 1); // Transparent
                ghostMat.SetFloat("_Blend", 0); // Alpha
                ghostMat.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetFloat("_ZWrite", 0);
                ghostMat.renderQueue = 3000;
                ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                ghostMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            // Set initial color with transparency
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

            Debug.Log($"Setup ghost material for {ghostRenderers[i].name}");
        }
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);

        // Update ghost transparency (more solid as it builds)
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, currentProgress);
        UpdateGhostAlpha(alpha);

        // Update progress bar
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = currentProgress;
        }

        // Update progress text
        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
        }
    }

    void UpdateGhostAlpha(float alpha)
    {
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
        // Spawn real building (handled by BuildJob)
        // Destroy this construction site
        Destroy(gameObject);
    }
}
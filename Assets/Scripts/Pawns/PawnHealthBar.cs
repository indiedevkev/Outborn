using UnityEngine;
using UnityEngine.UI;

public class PawnHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform fillTransform;  // ← Changed!
    [SerializeField] private Image fillImage;
    [SerializeField] private Canvas canvas;
    
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0);
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color hurtColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private float maxWidth = 90f;  // ← New!
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private bool hideWhenFar = true;
    [SerializeField] private float maxVisibleDistance = 30f;
    
    private Transform target;
    private Camera mainCamera;
    private float currentHealth = 100f;
    private float maxHealth = 100f;

    void Start()
    {
        mainCamera = Camera.main;
        
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
        }
        
        // Store initial width
        if (fillTransform != null)
        {
            maxWidth = fillTransform.sizeDelta.x;
        }
    }

    void LateUpdate()
    {
        if (target == null || mainCamera == null) return;
        
        // Position above pawn
        transform.position = target.position + offset;
        
        // Always face camera
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        
        // Hide when full or too far
        bool shouldShow = true;
        
        if (hideWhenFull && currentHealth >= maxHealth)
        {
            shouldShow = false;
        }
        
        if (hideWhenFar)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, target.position);
            if (distance > maxVisibleDistance)
            {
                shouldShow = false;
            }
        }
        
        canvas.enabled = shouldShow;
    }

    public void Setup(Transform targetTransform, float health, float maxHp)
    {
        target = targetTransform;
        maxHealth = maxHp;
        SetHealth(health);
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        float healthPercent = currentHealth / maxHealth;
        
        if (fillTransform != null)
        {
            // Change width based on health
            Vector2 size = fillTransform.sizeDelta;
            size.x = maxWidth * healthPercent;
            fillTransform.sizeDelta = size;
        }
        
        if (fillImage != null)
        {
            // Color based on health percentage
            if (healthPercent > 0.6f)
                fillImage.color = healthyColor;
            else if (healthPercent > 0.3f)
                fillImage.color = hurtColor;
            else
                fillImage.color = criticalColor;
        }
    }

    public void SetMaxHealth(float maxHp)
    {
        maxHealth = maxHp;
        SetHealth(currentHealth);
    }
}
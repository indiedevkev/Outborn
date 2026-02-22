using UnityEngine;
using TMPro;

public class PawnNameTag : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Canvas canvas;
    
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private bool hideWhenFar = true;
    [SerializeField] private float maxVisibleDistance = 30f;
    
    private Transform target;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
        }
    }

    void LateUpdate()
    {
        if (target == null || mainCamera == null) return;
        
        // Position above pawn
        transform.position = target.position + offset;
        
        // Always face camera
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        
        // Hide when too far
        if (hideWhenFar)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, target.position);
            canvas.enabled = distance <= maxVisibleDistance;
        }
    }

    public void Setup(Transform targetTransform, string pawnName)
    {
        target = targetTransform;
        
        if (nameText != null)
        {
            nameText.text = pawnName;
        }
    }

    public void SetName(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
        }
    }
}
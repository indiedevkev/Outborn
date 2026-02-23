using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCameraControllerV2 : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float fastMoveMultiplier = 2f;
    [SerializeField] private float edgeScrollSize = 20f;
    [SerializeField] private bool useEdgeScrolling = true;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private float zoomSmoothTime = 0.1f;
    
    [Header("Rotation Settings (Middle Mouse)")]
    [SerializeField] private float mouseRotationSpeed = 200f;
    [SerializeField] private float mousePitchSpeed = 100f;
    [SerializeField] private float minPitch = 10f;
    [SerializeField] private float maxPitch = 85f;
    
    [Header("Keyboard Rotation (Q/E)")]
    [SerializeField] private float keyboardRotationSpeed = 100f;
    
    [Header("Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);
    
    private Camera cam;
    private Vector2 moveInput;
    private float keyboardRotateInput;
    private Vector3 targetPosition;
    private float currentZoom;
    private float targetZoom;
    private float zoomVelocity;
    
    // Rotation
    private bool isRotating = false;
    private Vector2 lastMousePosition;
    private float currentYaw = 0f;
    private float currentPitch = 45f;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        targetPosition = transform.position;
        currentZoom = cam.transform.localPosition.magnitude;
        targetZoom = currentZoom;
        
        // Set initial rotation
        UpdateCameraRotation();
    }

    void Update()
    {
        HandleMovement();
        HandleMouseRotation();
        HandleKeyboardRotation();
        HandleZoom();
    }

    // Input System Callbacks
    public void OnMovement(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        float zoomValue = context.ReadValue<float>();
        targetZoom -= zoomValue * zoomSpeed;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    public void OnRotateCamera(InputAction.CallbackContext context)
    {
        // Middle mouse button hold
        if (context.started)
        {
            isRotating = true;
            lastMousePosition = Mouse.current.position.ReadValue();
            Cursor.visible = false;
        }
        
        if (context.canceled)
        {
            isRotating = false;
            Cursor.visible = true;
        }
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        keyboardRotateInput = context.ReadValue<float>();
    }

    void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;
        
        // Get forward and right relative to camera rotation (but keep horizontal)
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();
        
        // Input System Movement
        moveDirection += forward * moveInput.y;
        moveDirection += right * moveInput.x;
        
        // Edge Scrolling
        if (useEdgeScrolling && Mouse.current != null && !isRotating)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            
            if (mousePos.x < edgeScrollSize)
                moveDirection -= right;
            if (mousePos.x > Screen.width - edgeScrollSize)
                moveDirection += right;
            if (mousePos.y < edgeScrollSize)
                moveDirection -= forward;
            if (mousePos.y > Screen.height - edgeScrollSize)
                moveDirection += forward;
        }
        
        // Fast move with Shift
        float currentSpeed = moveSpeed;
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
        {
            currentSpeed *= fastMoveMultiplier;
        }
        
        // unscaledDeltaTime: Kamera immer gleich schnell, unabhängig von Pause/Zeitskala
        float dt = Time.unscaledDeltaTime;
        targetPosition += moveDirection.normalized * currentSpeed * dt;

        if (useBoundaries)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, dt * 10f);
    }

    void HandleMouseRotation()
    {
        if (!isRotating || Mouse.current == null) return;
        
        Vector2 currentMousePosition = Mouse.current.position.ReadValue();
        Vector2 mouseDelta = currentMousePosition - lastMousePosition;

        float dt = Time.unscaledDeltaTime;
        currentYaw += mouseDelta.x * mouseRotationSpeed * dt;
        currentPitch -= mouseDelta.y * mousePitchSpeed * dt;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        
        UpdateCameraRotation();
        
        lastMousePosition = currentMousePosition;
    }

    void HandleKeyboardRotation()
    {
        if (Mathf.Abs(keyboardRotateInput) > 0.01f)
        {
            currentYaw += keyboardRotateInput * keyboardRotationSpeed * Time.unscaledDeltaTime;
            UpdateCameraRotation();
        }
    }

    void UpdateCameraRotation()
    {
        // Apply rotation to the rig
        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        
        // Update camera distance
        UpdateCameraPosition();
    }

    void HandleZoom()
    {
        // Smooth zoom mit unscaledDeltaTime, damit bei Pause/Zeitskala Zoom normal reagiert
        currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        if (cam.orthographic)
        {
            cam.orthographicSize = currentZoom;
        }
        else
        {
            // Camera looks backward along local -Z, so position it back
            cam.transform.localPosition = new Vector3(0, 0, -currentZoom);
        }
    }

    // Public methods
    public void SetRotation(float yaw, float pitch)
    {
        currentYaw = yaw;
        currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        UpdateCameraRotation();
    }

    public void ResetRotation()
    {
        currentYaw = 0f;
        currentPitch = 45f;
        UpdateCameraRotation();
    }
}
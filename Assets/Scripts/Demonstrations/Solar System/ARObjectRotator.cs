using UnityEngine;
using UnityEngine.InputSystem;

public class ARObjectRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("How fast the model spins when you drag")]
    public float rotationSpeed = 0.2f;
    
    [Header("Scale Settings")]
    [Tooltip("How fast the model scales when pinching")]
    public float pinchScaleSpeed = 0.005f;
    [Tooltip("How fast the model scales with the mouse scroll wheel")]
    public float scrollScaleSpeed = 0.001f;
    
    [Tooltip("Minimum allowed scale")]
    public float minScale = 0.01f;
    [Tooltip("Maximum allowed scale")]
    public float maxScale = 2.0f;

    // State trackers
    private Vector2 lastSingleTouchPosition;
    private float lastPinchDistance;
    private bool isDragging = false;
    private bool wasPinching = false;

    private void Update()
    {
        HandleMouseInput();
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null) return;

        // 1. Mouse Scroll for Scaling (XR Simulator)
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            ApplyScale(scroll * scrollScaleSpeed);
        }

        // 2. Left Click & Drag for Full Axis Rotation
        bool isPressed = Mouse.current.leftButton.isPressed;
        bool wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
        bool wasReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
        Vector2 currentPos = Mouse.current.position.ReadValue();

        if (wasPressedThisFrame)
        {
            isDragging = true;
            lastSingleTouchPosition = currentPos;
        }
        else if (wasReleasedThisFrame)
        {
            isDragging = false;
        }
        else if (isDragging && isPressed)
        {
            RotateObject(currentPos);
        }
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touches = Touchscreen.current.touches;
        bool touch0Pressed = touches[0].press.isPressed;
        bool touch1Pressed = touches[1].press.isPressed;

        // 1. Two Fingers: Pinch to Scale
        if (touch0Pressed && touch1Pressed)
        {
            isDragging = false; // Cancel rotation when scaling

            Vector2 touch0Pos = touches[0].position.ReadValue();
            Vector2 touch1Pos = touches[1].position.ReadValue();
            float currentDistance = Vector2.Distance(touch0Pos, touch1Pos);

            if (wasPinching)
            {
                float distanceDelta = currentDistance - lastPinchDistance;
                ApplyScale(distanceDelta * pinchScaleSpeed);
            }

            lastPinchDistance = currentDistance;
            wasPinching = true;
        }
        // 2. One Finger: Swipe to Rotate (Full Axis)
        else if (touch0Pressed && !touch1Pressed)
        {
            wasPinching = false;
            Vector2 currentPos = touches[0].position.ReadValue();
            bool wasPressedThisFrame = touches[0].press.wasPressedThisFrame;
            bool wasReleasedThisFrame = touches[0].press.wasReleasedThisFrame;

            if (wasPressedThisFrame)
            {
                isDragging = true;
                lastSingleTouchPosition = currentPos;
            }
            else if (wasReleasedThisFrame)
            {
                isDragging = false;
            }
            else if (isDragging)
            {
                RotateObject(currentPos);
            }
        }
        else
        {
            isDragging = false;
            wasPinching = false;
        }
    }

    private void RotateObject(Vector2 currentPos)
    {
        // Calculate the difference in X and Y from the last frame
        Vector2 delta = currentPos - lastSingleTouchPosition;
        
        // Horizontal drag (X-axis) rotates the model around the World's Up axis (Yaw)
        transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
        
        // Vertical drag (Y-axis) rotates the model around the Camera's Right axis (Pitch)
        // Using the camera's right axis prevents the controls from feeling inverted when the model is turned upside down.
        if (Camera.main != null)
        {
            transform.Rotate(Camera.main.transform.right, delta.y * rotationSpeed, Space.World);
        }
        
        lastSingleTouchPosition = currentPos;
    }

    private void ApplyScale(float scaleDelta)
    {
        Vector3 newScale = transform.localScale + (Vector3.one * scaleDelta);
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
        transform.localScale = newScale;
    }
}
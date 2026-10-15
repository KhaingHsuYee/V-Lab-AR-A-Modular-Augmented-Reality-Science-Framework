using UnityEngine;
using UnityEngine.InputSystem;

public class FlaskInteraction : MonoBehaviour
{
    private Camera arCamera;
    private bool isRotating = false;
    private Vector2 lastPointerPosition;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 0.2f; // Adjust to make rotation faster/slower

    void Start()
    {
        arCamera = Camera.main;
    }

    void Update()
    {
        // Safety check to ensure an input device pointer exists
        if (Pointer.current == null) return;

        Vector2 pointerPosition = Pointer.current.position.ReadValue();

        // ========================================================
        // 1. SIMULATED TWO-FINGER DOUBLE TOUCH (Open Chemical Menu)
        // ========================================================
        bool isRealDoubleTouch = Touchscreen.current != null && Touchscreen.current.touches.Count == 2 &&
                                 Touchscreen.current.touches[0].press.wasPressedThisFrame && 
                                 Touchscreen.current.touches[1].press.wasPressedThisFrame;

        bool isSimulatedDoubleTouch = Keyboard.current != null && Keyboard.current.shiftKey.isPressed && 
                                      Pointer.current.press.wasPressedThisFrame;

        if (isRealDoubleTouch || isSimulatedDoubleTouch)
        {
            Ray ray = arCamera.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.root == transform)
                {
                    LabUIManager uiManager = FindFirstObjectByType<LabUIManager>();
                    if (uiManager != null)
                    {
                        uiManager.ToggleIndicatorMenu(transform.position);
                        isRotating = false; 
                        return; // Exit update frame early
                    }
                }
            }
        }

        // ========================================================
        // 2. ONE-FINGER SWIPE ROTATION (Spin in Place)
        // ========================================================
        bool isMenuModifierPressed = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

        if (!isMenuModifierPressed)
        {
            // Detect initialization touch or click on the object
            if (Pointer.current.press.wasPressedThisFrame)
            {
                Ray ray = arCamera.ScreenPointToRay(pointerPosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform.root == transform) 
                    {
                        isRotating = true;
                        lastPointerPosition = pointerPosition;
                    }
                }
            }
            // Execute continuous rotation while finger/mouse moves
            else if (Pointer.current.press.isPressed && isRotating)
            {
                // Calculate how far the finger moved horizontally since the last frame
                float deltaX = pointerPosition.x - lastPointerPosition.x;

                // Rotate around the Y-axis (Up axis) based on horizontal movement
                // Multiplying by negative deltaX ensures dragging right rotates clockwise
                transform.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);

                // Save current position for the next frame's math calculation
                lastPointerPosition = pointerPosition;
            }
        }

        // Reset tracking states when input completes
        if (Pointer.current.press.wasReleasedThisFrame)
        {
            isRotating = false;
        }
    }
}
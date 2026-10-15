using UnityEngine;
using UnityEngine.InputSystem;

public class Flask_SwipeToRotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 0.2f; // How fast the flask spins
    public bool invertDirection = false; 

    private Vector2 touchStartPos;
    private bool isDragging = false;

    void Update()
    {
        Vector2 currentTouchPos = Vector2.zero;
        bool touchPressed = false;
        bool touchReleased = false;

        // Support both Mobile Touch and PC Mouse for easy testing
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            currentTouchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame) touchPressed = true;
            if (Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) touchReleased = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            currentTouchPos = Mouse.current.position.ReadValue();
            
            if (Mouse.current.leftButton.wasPressedThisFrame) touchPressed = true;
            if (Mouse.current.leftButton.wasReleasedThisFrame) touchReleased = true;
        }
        else
        {
            isDragging = false;
            return;
        }

        // When the user first touches the screen, record the starting position
        if (touchPressed)
        {
            touchStartPos = currentTouchPos;
            isDragging = true;
        }

        // If the user is dragging their finger, rotate the object
        if (isDragging)
        {
            float deltaX = currentTouchPos.x - touchStartPos.x;
            
            if (invertDirection) deltaX = -deltaX;

            // Rotate the flask around its Y axis (Up/Down axis)
            transform.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);

            // Reset the start position for the next frame so the spin is continuous
            touchStartPos = currentTouchPos; 
        }

        if (touchReleased)
        {
            isDragging = false;
        }
    }
}
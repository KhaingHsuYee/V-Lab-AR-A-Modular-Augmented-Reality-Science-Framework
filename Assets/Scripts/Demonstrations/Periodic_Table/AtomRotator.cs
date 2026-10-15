using UnityEngine;
using UnityEngine.InputSystem;

public class AtomRotator : MonoBehaviour
{
    private Vector2 previousInputPos;
    private bool isDragging = false;
    private float rotationSpeed = 0.25f;

    void Update()
    {
        Vector2 currentInputPos = Vector2.zero;
        bool isPressing = false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            currentInputPos = Touchscreen.current.primaryTouch.position.ReadValue();
            isPressing = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            currentInputPos = Mouse.current.position.ReadValue();
            isPressing = true;
        }

        if (isPressing)
        {
            if (!isDragging) previousInputPos = currentInputPos;
            
            Vector2 delta = currentInputPos - previousInputPos;
            
            // Rotate relative to the camera's perspective for true 360 trackball movement
            Transform cam = Camera.main.transform;
            transform.Rotate(cam.up, -delta.x * rotationSpeed, Space.World);
            transform.Rotate(cam.right, delta.y * rotationSpeed, Space.World);
            
            previousInputPos = currentInputPos;
            isDragging = true;
        }
        else
        {
            isDragging = false;
        }
    }
}
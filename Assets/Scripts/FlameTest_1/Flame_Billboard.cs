using UnityEngine;

public class Flame_Billboard : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            // 1. Get the direction from the flame TO the camera
            Vector3 directionToCamera = Camera.main.transform.position - transform.position;
            
            // 2. Lock the Y axis so the flame never tilts up or down (keeps it in the hole)
            directionToCamera.y = 0; 
            
            if (directionToCamera != Vector3.zero)
            {
                // 3. VFX Quads often render on the back (-Z). 
                // By pointing the FORWARD direction (+Z) away from the camera, 
                // the back perfectly faces the player without flipping the mesh!
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
}
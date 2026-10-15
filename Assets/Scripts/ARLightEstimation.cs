using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARLightEstimation : MonoBehaviour
{
    // Assign this to your AR Camera in the Inspector
    [SerializeField]
    private ARCameraManager m_ARCameraManager;

    // Assign your scene's directional light in the Inspector
    [SerializeField]
    private Light m_DirectionalLight;

    void OnEnable()
    {
        if (m_ARCameraManager != null)
            m_ARCameraManager.frameReceived += OnFrameReceived;
    }

    void OnDisable()
    {
        if (m_ARCameraManager != null)
            m_ARCameraManager.frameReceived -= OnFrameReceived;
    }

    void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        if (m_DirectionalLight == null) return;

        // Apply Brightness (Intensity)
        if (args.lightEstimation.averageBrightness.HasValue)
        {
            m_DirectionalLight.intensity = args.lightEstimation.averageBrightness.Value;
        }

        // Apply Color Temperature
        if (args.lightEstimation.averageColorTemperature.HasValue)
        {
            m_DirectionalLight.colorTemperature = args.lightEstimation.averageColorTemperature.Value;
        }
        
        // Optional: Apply Color Correction if available
        if (args.lightEstimation.colorCorrection.HasValue)
        {
            m_DirectionalLight.color = args.lightEstimation.colorCorrection.Value;
        }
    }
}
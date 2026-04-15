using UnityEngine;
using System.Collections;

namespace VRDiagnostics
{
    /// <summary>
    /// Fixes camera height issue caused by OVR Manager adding tracking offset
    /// on top of the Camera Offset. Resets Camera Offset Y to 0 at startup
    /// so only OVR Manager / headset tracking controls the height.
    /// </summary>
    public class CameraStartFix : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null; // Wait one frame

            // Find Camera Offset and reset its Y to 0
            // The OVR Manager already handles camera height via tracking
            var cameraOffset = transform.Find("Camera Offset");
            if (cameraOffset != null)
            {
                Vector3 pos = cameraOffset.localPosition;
                Debug.Log($"[CameraStartFix] Camera Offset was at Y={pos.y}. Resetting to 0.");
                pos.y = 0f;
                cameraOffset.localPosition = pos;
            }

            // Also reset the Main Camera local position to zero
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.localPosition = Vector3.zero;
                mainCam.transform.localRotation = Quaternion.identity;
                Debug.Log("[CameraStartFix] Main Camera local position and rotation reset.");
            }
        }
    }
}

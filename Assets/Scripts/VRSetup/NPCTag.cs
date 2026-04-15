using UnityEngine;

namespace VRDiagnostics
{
    /// <summary>
    /// Attach this tag component to any GameObject that should be identifiable as a gaze target.
    /// The EyeTrackingManager uses GetComponentInParent to find the root NPC name.
    /// </summary>
    public class NPCTag : MonoBehaviour
    {
        [Tooltip("Optional display name for this gaze target. If empty, uses GameObject.name.")]
        public string displayName;

        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;

namespace VRDiagnostics
{
    [Serializable]
    public class GazeDataPoint
    {
        public float timestamp;
        public Vector3 gazeOrigin;
        public Vector3 gazeDirection;
        public string hitTarget;
        public float fixationDuration;
    }

    public class EyeTrackingManager : MonoBehaviour
    {
        [Header("Gaze Settings")]
        [SerializeField] private float gazeRayMaxDistance = 50f;
        [SerializeField] private LayerMask gazeLayerMask = ~0;
        [SerializeField] private float fixationThreshold = 0.05f; // angle threshold in degrees to count as same fixation
        [SerializeField] private float sampleRate = 0.033f; // ~30 Hz

        [Header("Debug")]
        [SerializeField] private bool showDebugRay = true;

        public bool IsTracking { get; private set; }
        public Vector3 CurrentGazeOrigin { get; private set; }
        public Vector3 CurrentGazeDirection { get; private set; }
        public string CurrentFixationTarget { get; private set; }
        public float CurrentFixationDuration { get; private set; }

        public event Action<GazeDataPoint> OnGazeDataRecorded;
        public event Action<string, float> OnFixationEnded; // target name, duration

        private readonly List<GazeDataPoint> gazeDataLog = new List<GazeDataPoint>();
        private string previousFixationTarget;
        private float fixationStartTime;
        private float lastSampleTime;
        private Transform xrCameraTransform;

        // OVR Eye Tracking references (populated at runtime if Meta SDK available)
        private Component ovrEyeGazeLeft;
        private Component ovrEyeGazeRight;
        private bool useOVREyeTracking;

        private void Start()
        {
            InitializeEyeTracking();
        }

        private void InitializeEyeTracking()
        {
            // Try to find the XR Camera
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                xrCameraTransform = mainCam.transform;
            }

            // Try to find OVREyeGaze components (Meta Quest Pro eye tracking)
            // These are added via the Meta XR SDK and attached to the XR Rig's eye anchor objects
            TryInitializeMetaEyeTracking();

            if (!useOVREyeTracking)
            {
                Debug.Log("[EyeTrackingManager] Meta eye tracking not found. Falling back to head-gaze (camera forward direction).");
            }
            else
            {
                Debug.Log("[EyeTrackingManager] Meta Quest Pro eye tracking initialized.");
            }

            IsTracking = true;
        }

        private void TryInitializeMetaEyeTracking()
        {
            // Look for OVREyeGaze components in the scene
            // These are typically on "LeftEyeAnchor" and "RightEyeAnchor" under OVRCameraRig
            var eyeGazeType = Type.GetType("OVREyeGaze, Meta.XR.Core");
            if (eyeGazeType == null)
                return;

            #pragma warning disable CS0618 // FindObjectsOfType with Type is acceptable here for reflection-based lookup
            var allEyeGazes = FindObjectsOfType(eyeGazeType);
            #pragma warning restore CS0618
            foreach (var obj in allEyeGazes)
            {
                var component = obj as Component;
                if (component == null) continue;

                // Determine left vs right by GameObject name
                if (component.gameObject.name.Contains("Left"))
                    ovrEyeGazeLeft = component;
                else if (component.gameObject.name.Contains("Right"))
                    ovrEyeGazeRight = component;
            }

            useOVREyeTracking = ovrEyeGazeLeft != null || ovrEyeGazeRight != null;
        }

        private void Update()
        {
            if (!IsTracking) return;
            if (Time.time - lastSampleTime < sampleRate) return;

            lastSampleTime = Time.time;
            UpdateGaze();
        }

        private void UpdateGaze()
        {
            // Get gaze ray
            Ray gazeRay = GetGazeRay();
            CurrentGazeOrigin = gazeRay.origin;
            CurrentGazeDirection = gazeRay.direction;

            if (showDebugRay)
            {
                Debug.DrawRay(gazeRay.origin, gazeRay.direction * gazeRayMaxDistance, Color.cyan);
            }

            // Raycast to find what user is looking at
            string hitTargetName = "None";
            if (Physics.Raycast(gazeRay, out RaycastHit hit, gazeRayMaxDistance, gazeLayerMask))
            {
                hitTargetName = hit.collider.gameObject.name;

                // Check parent for more meaningful name (e.g., NPC root object)
                var npcController = hit.collider.GetComponentInParent<NPCTag>();
                if (npcController != null)
                {
                    hitTargetName = npcController.gameObject.name;
                }
            }

            // Update fixation tracking
            UpdateFixation(hitTargetName);

            // Record data point
            var dataPoint = new GazeDataPoint
            {
                timestamp = Time.realtimeSinceStartup,
                gazeOrigin = CurrentGazeOrigin,
                gazeDirection = CurrentGazeDirection,
                hitTarget = hitTargetName,
                fixationDuration = CurrentFixationDuration
            };

            gazeDataLog.Add(dataPoint);
            OnGazeDataRecorded?.Invoke(dataPoint);
        }

        private Ray GetGazeRay()
        {
            // If Meta eye tracking is available, use the combined eye gaze
            if (useOVREyeTracking)
            {
                // Use the combined gaze from left/right OVREyeGaze transforms
                Transform gazeTransform = null;

                if (ovrEyeGazeLeft != null && ovrEyeGazeRight != null)
                {
                    // Average of both eyes for combined gaze
                    var leftT = ((Component)ovrEyeGazeLeft).transform;
                    var rightT = ((Component)ovrEyeGazeRight).transform;
                    Vector3 origin = (leftT.position + rightT.position) / 2f;
                    Vector3 direction = ((leftT.forward + rightT.forward) / 2f).normalized;
                    return new Ray(origin, direction);
                }
                else
                {
                    gazeTransform = ovrEyeGazeLeft != null
                        ? ((Component)ovrEyeGazeLeft).transform
                        : ((Component)ovrEyeGazeRight).transform;
                    return new Ray(gazeTransform.position, gazeTransform.forward);
                }
            }

            // Fallback: use head direction (camera forward)
            if (xrCameraTransform != null)
            {
                return new Ray(xrCameraTransform.position, xrCameraTransform.forward);
            }

            return new Ray(Vector3.zero, Vector3.forward);
        }

        private void UpdateFixation(string currentTarget)
        {
            if (currentTarget == previousFixationTarget)
            {
                CurrentFixationDuration = Time.realtimeSinceStartup - fixationStartTime;
            }
            else
            {
                // Fixation ended on previous target
                if (!string.IsNullOrEmpty(previousFixationTarget) && previousFixationTarget != "None")
                {
                    OnFixationEnded?.Invoke(previousFixationTarget, CurrentFixationDuration);
                }

                // New fixation started
                previousFixationTarget = currentTarget;
                CurrentFixationTarget = currentTarget;
                fixationStartTime = Time.realtimeSinceStartup;
                CurrentFixationDuration = 0f;
            }
        }

        public List<GazeDataPoint> GetGazeLog()
        {
            return new List<GazeDataPoint>(gazeDataLog);
        }

        public void ClearLog()
        {
            gazeDataLog.Clear();
        }

        public void StartTracking()
        {
            IsTracking = true;
            gazeDataLog.Clear();
        }

        public void StopTracking()
        {
            IsTracking = false;
        }
    }

}

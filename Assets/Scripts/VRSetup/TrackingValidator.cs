using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System;

namespace VRDiagnostics
{
    public class TrackingValidator : MonoBehaviour
    {
        [Header("Tracking Thresholds")]
        [SerializeField] private float positionChangeThreshold = 0.001f;
        [SerializeField] private float validationCheckInterval = 1.0f;

        [Header("UI References")]
        [Tooltip("Assign a world-space UI panel that shows tracking warnings.")]
        [SerializeField] private GameObject trackingWarningUI;

        public bool Is6DoFActive { get; private set; }
        public bool IsHeadsetWorn { get; private set; }
        public TrackingQuality Quality { get; private set; }

        public event Action<bool> OnTrackingStatusChanged;
        public event Action<TrackingQuality> OnTrackingQualityChanged;

        private InputDevice headDevice;
        private Vector3 lastHeadPosition;
        private Quaternion lastHeadRotation;
        private float lastCheckTime;
        private bool wasTracking;
        private int consecutiveStaticFrames;

        public enum TrackingQuality
        {
            Unknown,
            Good,
            Degraded,
            Lost
        }

        private void Start()
        {
            if (trackingWarningUI != null)
                trackingWarningUI.SetActive(false);

            TryGetHeadDevice();
        }

        private void TryGetHeadDevice()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.Head, devices);
            if (devices.Count > 0)
            {
                headDevice = devices[0];
                Debug.Log($"[TrackingValidator] Head device found: {headDevice.name}");
            }
        }

        private void Update()
        {
            if (Time.time - lastCheckTime < validationCheckInterval)
                return;

            lastCheckTime = Time.time;

            if (!headDevice.isValid)
            {
                TryGetHeadDevice();
                if (!headDevice.isValid)
                {
                    SetTrackingStatus(false, TrackingQuality.Lost);
                    return;
                }
            }

            ValidateTracking();
        }

        private void ValidateTracking()
        {
            bool hasPosition = headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 currentPosition);
            bool hasRotation = headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion currentRotation);
            bool isTracked = false;
            headDevice.TryGetFeatureValue(CommonUsages.isTracked, out isTracked);

            // Check if we have 6DoF (both position and rotation changing)
            bool positionChanging = hasPosition && Vector3.Distance(currentPosition, lastHeadPosition) > positionChangeThreshold;
            bool rotationChanging = hasRotation && Quaternion.Angle(currentRotation, lastHeadRotation) > 0.1f;

            if (!positionChanging && !rotationChanging)
            {
                consecutiveStaticFrames++;
            }
            else
            {
                consecutiveStaticFrames = 0;
            }

            // Determine tracking quality
            TrackingQuality quality;
            bool tracking;

            if (!isTracked || (!hasPosition && !hasRotation))
            {
                quality = TrackingQuality.Lost;
                tracking = false;
            }
            else if (consecutiveStaticFrames > 5)
            {
                // Position hasn't changed for several checks — might be 3DoF only or static
                quality = TrackingQuality.Degraded;
                tracking = true;
            }
            else
            {
                quality = TrackingQuality.Good;
                tracking = true;
            }

            Is6DoFActive = hasPosition && isTracked;
            lastHeadPosition = currentPosition;
            lastHeadRotation = currentRotation;

            SetTrackingStatus(tracking, quality);

            // Check if headset is on user's head (user presence)
            if (headDevice.TryGetFeatureValue(CommonUsages.userPresence, out bool userPresent))
            {
                IsHeadsetWorn = userPresent;
            }
        }

        private void SetTrackingStatus(bool isTracking, TrackingQuality quality)
        {
            if (isTracking != wasTracking)
            {
                wasTracking = isTracking;
                OnTrackingStatusChanged?.Invoke(isTracking);

                if (trackingWarningUI != null)
                    trackingWarningUI.SetActive(!isTracking);

                if (!isTracking)
                    Debug.LogWarning("[TrackingValidator] Tracking lost!");
                else
                    Debug.Log("[TrackingValidator] Tracking restored.");
            }

            if (quality != Quality)
            {
                Quality = quality;
                OnTrackingQualityChanged?.Invoke(quality);
            }
        }

        /// <summary>
        /// Call this during onboarding to confirm 6DoF is working before starting the scenario.
        /// Returns true if position and rotation tracking are active.
        /// </summary>
        public bool Validate6DoF()
        {
            if (!headDevice.isValid)
                TryGetHeadDevice();

            if (!headDevice.isValid)
            {
                Debug.LogError("[TrackingValidator] No head device found. Is the headset connected?");
                return false;
            }

            bool hasPosition = headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out _);
            bool hasRotation = headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out _);
            bool isTracked = false;
            headDevice.TryGetFeatureValue(CommonUsages.isTracked, out isTracked);

            bool result = hasPosition && hasRotation && isTracked;
            Debug.Log($"[TrackingValidator] 6DoF Validation: Position={hasPosition}, Rotation={hasRotation}, Tracked={isTracked} => {(result ? "PASS" : "FAIL")}");

            Is6DoFActive = result;
            return result;
        }

        /// <summary>
        /// Returns a summary of current tracking metrics for logging.
        /// </summary>
        public string GetTrackingReport()
        {
            return $"6DoF={Is6DoFActive}, Quality={Quality}, HeadsetWorn={IsHeadsetWorn}";
        }
    }
}

using UnityEngine;
using System;

namespace VRDiagnostics
{
    /// <summary>
    /// Detects when the user stands up in VR by monitoring head height changes.
    /// Used during Tasks 1 and 3 where the user is asked to stand and present.
    /// Also provides a fallback UI button for testing in Editor.
    /// </summary>
    public class StandUpDetector : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("The VR camera (head) to track height")]
        [SerializeField] private Transform vrCamera;

        [Tooltip("Height increase (meters) from seated baseline to count as standing")]
        [SerializeField] private float standingThreshold = 0.3f;

        [Tooltip("How long the height must be above threshold to confirm standing (seconds)")]
        [SerializeField] private float confirmationTime = 0.5f;

        [Header("Baseline")]
        [Tooltip("Seated head height is captured automatically at calibration")]
        [SerializeField] private float seatedHeight;
        [SerializeField] private bool baselineSet;

        public bool IsStanding { get; private set; }
        public float CurrentHeight { get; private set; }

        public event Action OnStoodUp;
        public event Action OnSatDown;

        private float timeAboveThreshold;
        private bool wasStanding;

        private void Start()
        {
            if (vrCamera == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                    vrCamera = mainCam.transform;
            }
        }

        /// <summary>
        /// Call this to set the current head height as the seated baseline.
        /// Should be called during onboarding while user is seated.
        /// </summary>
        public void CalibrateSeatedHeight()
        {
            if (vrCamera != null)
            {
                seatedHeight = vrCamera.localPosition.y;
                baselineSet = true;
                Debug.Log($"[StandUpDetector] Seated baseline set: {seatedHeight:F2}m");
            }
        }

        private void Update()
        {
            if (vrCamera == null || !baselineSet) return;

            CurrentHeight = vrCamera.localPosition.y;
            float heightDifference = CurrentHeight - seatedHeight;

            if (heightDifference >= standingThreshold)
            {
                timeAboveThreshold += Time.deltaTime;

                if (timeAboveThreshold >= confirmationTime && !IsStanding)
                {
                    IsStanding = true;
                    OnStoodUp?.Invoke();

                    if (ScenarioManager.Instance != null)
                        ScenarioManager.Instance.FireEvent(ScenarioEventType.UserStoodUp,
                            $"Height: {CurrentHeight:F2}m (baseline: {seatedHeight:F2}m)");

                    Debug.Log($"[StandUpDetector] User stood up. Height: {CurrentHeight:F2}m");
                }
            }
            else
            {
                timeAboveThreshold = 0f;

                if (IsStanding)
                {
                    IsStanding = false;
                    OnSatDown?.Invoke();

                    if (ScenarioManager.Instance != null)
                        ScenarioManager.Instance.FireEvent(ScenarioEventType.UserSatDown,
                            $"Height: {CurrentHeight:F2}m");

                    Debug.Log("[StandUpDetector] User sat down.");
                }
            }
        }

        /// <summary>
        /// Manually trigger standing (for Editor testing or UI button fallback).
        /// </summary>
        public void SimulateStandUp()
        {
            if (!IsStanding)
            {
                IsStanding = true;
                OnStoodUp?.Invoke();

                if (ScenarioManager.Instance != null)
                    ScenarioManager.Instance.FireEvent(ScenarioEventType.UserStoodUp, "Simulated stand-up");

                Debug.Log("[StandUpDetector] Simulated stand-up.");
            }
        }

        /// <summary>
        /// Manually trigger sitting down.
        /// </summary>
        public void SimulateSitDown()
        {
            if (IsStanding)
            {
                IsStanding = false;
                OnSatDown?.Invoke();

                if (ScenarioManager.Instance != null)
                    ScenarioManager.Instance.FireEvent(ScenarioEventType.UserSatDown, "Simulated sit-down");

                Debug.Log("[StandUpDetector] Simulated sit-down.");
            }
        }
    }
}

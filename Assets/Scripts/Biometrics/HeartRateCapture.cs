using UnityEngine;
using System;
using System.Collections.Generic;

namespace VRDiagnostics
{
    /// <summary>
    /// Captures heart rate data from Meta Quest Pro's heart rate sensor.
    /// Falls back to simulated data if the sensor is unavailable (for testing).
    /// </summary>
    public class HeartRateCapture : MonoBehaviour
    {
        public static HeartRateCapture Instance { get; private set; }

        [Header("Sampling")]
        [Tooltip("How often to sample heart rate (in seconds)")]
        [SerializeField] private float sampleInterval = 1f;

        [Header("Simulation (Editor Testing)")]
        [Tooltip("Enable simulated HR data when no sensor is available")]
        [SerializeField] private bool enableSimulation = true;
        [SerializeField] private int simulatedBaseHR = 75;
        [SerializeField] private int simulatedVariation = 10;

        public bool IsCapturing { get; private set; }
        public int CurrentHeartRate { get; private set; }
        public float CurrentConfidence { get; private set; }
        public bool IsSensorAvailable { get; private set; }

        public event Action<HeartRateDataPoint> OnHeartRateRecorded;

        private readonly List<HeartRateDataPoint> heartRateLog = new List<HeartRateDataPoint>();
        private float lastSampleTime;
        private bool useOVRHeartRate;

        // OVR Body tracking reference for HR (populated at runtime if available)
        private System.Reflection.MethodInfo getHeartRateMethod;
        private object ovrBodyComponent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartCapture()
        {
            heartRateLog.Clear();
            TryInitializeSensor();
            IsCapturing = true;
            lastSampleTime = Time.realtimeSinceStartup;
            Debug.Log($"[HeartRateCapture] Started. Sensor available: {IsSensorAvailable}, Simulation: {enableSimulation}");
        }

        public void StopCapture()
        {
            IsCapturing = false;
            Debug.Log($"[HeartRateCapture] Stopped. {heartRateLog.Count} samples recorded.");
        }

        private void TryInitializeSensor()
        {
            // Attempt to find Meta Quest Pro heart rate sensor via reflection
            // The Quest Pro exposes HR through OVRPlugin.GetHeartRateStatus() in newer SDK versions
            try
            {
                var ovrPluginType = Type.GetType("OVRPlugin, Meta.XR.Core");
                if (ovrPluginType != null)
                {
                    // Check if heart rate API exists
                    var hrMethod = ovrPluginType.GetMethod("GetHeartRateStatus",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (hrMethod != null)
                    {
                        getHeartRateMethod = hrMethod;
                        useOVRHeartRate = true;
                        IsSensorAvailable = true;
                        Debug.Log("[HeartRateCapture] OVR heart rate sensor found.");
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeartRateCapture] Could not initialize OVR HR sensor: {e.Message}");
            }

            IsSensorAvailable = false;

            if (!enableSimulation)
            {
                Debug.LogWarning("[HeartRateCapture] No heart rate sensor found and simulation is disabled.");
            }
        }

        private void Update()
        {
            if (!IsCapturing) return;
            if (Time.realtimeSinceStartup - lastSampleTime < sampleInterval) return;

            lastSampleTime = Time.realtimeSinceStartup;
            SampleHeartRate();
        }

        private void SampleHeartRate()
        {
            int hr = 0;
            float confidence = 0f;

            if (useOVRHeartRate && getHeartRateMethod != null)
            {
                try
                {
                    // Attempt to read from OVR sensor
                    var result = getHeartRateMethod.Invoke(null, null);
                    if (result != null)
                    {
                        // Parse the result — structure depends on SDK version
                        hr = Convert.ToInt32(result);
                        confidence = hr > 0 ? 1f : 0f;
                    }
                }
                catch
                {
                    // Sensor read failed — fall through to simulation
                    hr = 0;
                    confidence = 0f;
                }
            }

            // Fall back to simulation if no real data
            if (hr <= 0 && enableSimulation)
            {
                hr = simulatedBaseHR + UnityEngine.Random.Range(-simulatedVariation, simulatedVariation + 1);
                confidence = 0.5f; // Mark as simulated with lower confidence

                // Add slight trending based on scenario state for realistic simulation
                if (ScenarioManager.Instance != null)
                {
                    var state = ScenarioManager.Instance.CurrentState;
                    switch (state)
                    {
                        case ScenarioState.Task1_Response:
                        case ScenarioState.Task3_Response:
                            hr += 15; // Higher HR during speaking tasks
                            break;
                        case ScenarioState.Task2_GroupWork:
                            hr += 5; // Slightly elevated during group work
                            break;
                        case ScenarioState.Onboarding:
                            hr -= 5; // Calmer during onboarding
                            break;
                    }
                }
            }

            if (hr <= 0) return; // No data available at all

            CurrentHeartRate = hr;
            CurrentConfidence = confidence;

            var dataPoint = new HeartRateDataPoint
            {
                timestamp = BiometricSynchronizer.GetTimestamp(),
                heartRate = hr,
                confidence = confidence
            };

            heartRateLog.Add(dataPoint);
            OnHeartRateRecorded?.Invoke(dataPoint);
        }

        /// <summary>
        /// Get all recorded heart rate data for export.
        /// </summary>
        public List<HeartRateDataPoint> GetHeartRateLog()
        {
            return new List<HeartRateDataPoint>(heartRateLog);
        }

        /// <summary>
        /// Write heart rate data into the session for export.
        /// </summary>
        public void FlushToSession(SessionData sessionData)
        {
            sessionData.heartRateData.AddRange(heartRateLog);
            Debug.Log($"[HeartRateCapture] Flushed {heartRateLog.Count} HR samples to session data.");
        }
    }
}

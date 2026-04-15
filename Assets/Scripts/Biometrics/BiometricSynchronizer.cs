using UnityEngine;
using System;

namespace VRDiagnostics
{
    /// <summary>
    /// Provides a shared timestamp source for all biometric data streams.
    /// All loggers (gaze, heart rate, speech, scenario events) should use
    /// BiometricSynchronizer.GetTimestamp() to ensure data alignment.
    /// </summary>
    public class BiometricSynchronizer : MonoBehaviour
    {
        public static BiometricSynchronizer Instance { get; private set; }

        /// <summary>
        /// The absolute UTC time when recording started.
        /// Used to convert relative timestamps to absolute time in exports.
        /// </summary>
        public static DateTime RecordingStartTimeUTC { get; private set; }

        /// <summary>
        /// The realtimeSinceStartup value when recording started.
        /// All relative timestamps are offset from this.
        /// </summary>
        public static float RecordingStartRealtime { get; private set; }

        public static bool IsRecording { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Call this when the session/scenario starts to set the recording origin.
        /// All subsequent GetTimestamp() calls will be relative to this point.
        /// </summary>
        public static void StartRecording()
        {
            RecordingStartTimeUTC = DateTime.UtcNow;
            RecordingStartRealtime = Time.realtimeSinceStartup;
            IsRecording = true;
            Debug.Log($"[BiometricSynchronizer] Recording started at {RecordingStartTimeUTC:O}, realtime={RecordingStartRealtime:F3}");
        }

        /// <summary>
        /// Call this when the session ends.
        /// </summary>
        public static void StopRecording()
        {
            IsRecording = false;
            float duration = Time.realtimeSinceStartup - RecordingStartRealtime;
            Debug.Log($"[BiometricSynchronizer] Recording stopped. Duration: {duration:F1}s");
        }

        /// <summary>
        /// Get the current synchronized timestamp (seconds since recording start).
        /// This is the ONLY method all biometric loggers should use for timestamps.
        /// Precision: milliseconds via Time.realtimeSinceStartup.
        /// </summary>
        public static float GetTimestamp()
        {
            return Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Convert a relative timestamp to absolute UTC DateTime.
        /// Useful for data export and cross-system alignment.
        /// </summary>
        public static DateTime ToAbsoluteTime(float relativeTimestamp)
        {
            float offset = relativeTimestamp - RecordingStartRealtime;
            return RecordingStartTimeUTC.AddSeconds(offset);
        }

        /// <summary>
        /// Get elapsed time since recording started.
        /// </summary>
        public static float GetElapsedTime()
        {
            if (!IsRecording) return 0f;
            return Time.realtimeSinceStartup - RecordingStartRealtime;
        }
    }
}

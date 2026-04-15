using UnityEngine;
using System;

namespace VRDiagnostics
{
    /// <summary>
    /// Subscribes to SpeechDetector events and calculates aggregate speech statistics.
    /// Provides summary data (total duration, utterance count, average volume)
    /// for each task and the overall session.
    /// </summary>
    public class SpeechActivityLogger : MonoBehaviour
    {
        public static SpeechActivityLogger Instance { get; private set; }

        public bool IsLogging { get; private set; }
        public float TotalSpeechDuration { get; private set; }
        public int TotalUtterances { get; private set; }
        public float AverageAmplitude { get; private set; }
        public float PeakAmplitude { get; private set; }

        private float amplitudeSum;
        private int amplitudeSamples;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartLogging()
        {
            IsLogging = true;
            TotalSpeechDuration = 0f;
            TotalUtterances = 0;
            AverageAmplitude = 0f;
            PeakAmplitude = 0f;
            amplitudeSum = 0f;
            amplitudeSamples = 0;

            // Subscribe to SpeechDetector events
            if (SpeechDetector.Instance != null)
            {
                SpeechDetector.Instance.OnSpeechStarted += OnSpeechStarted;
                SpeechDetector.Instance.OnSpeechEnded += OnSpeechEnded;
                SpeechDetector.Instance.OnAmplitudeUpdated += OnAmplitudeUpdated;
            }

            Debug.Log("[SpeechActivityLogger] Started logging.");
        }

        public void StopLogging()
        {
            IsLogging = false;

            // Unsubscribe
            if (SpeechDetector.Instance != null)
            {
                SpeechDetector.Instance.OnSpeechStarted -= OnSpeechStarted;
                SpeechDetector.Instance.OnSpeechEnded -= OnSpeechEnded;
                SpeechDetector.Instance.OnAmplitudeUpdated -= OnAmplitudeUpdated;
            }

            // Calculate final average
            if (amplitudeSamples > 0)
                AverageAmplitude = amplitudeSum / amplitudeSamples;

            Debug.Log($"[SpeechActivityLogger] Stopped. Utterances: {TotalUtterances}, " +
                      $"Total Duration: {TotalSpeechDuration:F1}s, " +
                      $"Avg Amplitude: {AverageAmplitude:F4}, " +
                      $"Peak: {PeakAmplitude:F4}");
        }

        private void OnSpeechStarted()
        {
            if (!IsLogging) return;
            TotalUtterances++;
        }

        private void OnSpeechEnded(float duration)
        {
            if (!IsLogging) return;
            TotalSpeechDuration += duration;
        }

        private void OnAmplitudeUpdated(float amplitude)
        {
            if (!IsLogging) return;

            // Only count amplitude samples while speech is active
            if (SpeechDetector.Instance != null && SpeechDetector.Instance.IsSpeaking)
            {
                amplitudeSum += amplitude;
                amplitudeSamples++;

                if (amplitude > PeakAmplitude)
                    PeakAmplitude = amplitude;
            }
        }

        /// <summary>
        /// Get a summary string of speech activity for the session data.
        /// </summary>
        public string GetSummary()
        {
            return $"Utterances: {TotalUtterances}, Duration: {TotalSpeechDuration:F1}s, " +
                   $"AvgAmp: {AverageAmplitude:F4}, Peak: {PeakAmplitude:F4}";
        }

        private void OnDestroy()
        {
            // Safety: unsubscribe on destroy
            if (SpeechDetector.Instance != null)
            {
                SpeechDetector.Instance.OnSpeechStarted -= OnSpeechStarted;
                SpeechDetector.Instance.OnSpeechEnded -= OnSpeechEnded;
                SpeechDetector.Instance.OnAmplitudeUpdated -= OnAmplitudeUpdated;
            }
        }
    }
}

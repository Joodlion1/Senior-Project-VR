using UnityEngine;
using System;
using System.Collections.Generic;

namespace VRDiagnostics
{
    public class SpeechDetector : MonoBehaviour
    {
        public static SpeechDetector Instance { get; private set; }

        [Header("Detection Settings")]
        [SerializeField] private float amplitudeThreshold = 0.02f;
        [SerializeField] private float silenceTimeout = 1.0f; // seconds of silence before speech "ends"
        [SerializeField] private int sampleSize = 256;

        [Header("Debug")]
        [SerializeField] private bool logSpeechEvents = true;

        public bool IsListening { get; private set; }
        public bool IsSpeaking { get; private set; }
        public float CurrentAmplitude { get; private set; }
        public float CurrentSpeechDuration { get; private set; }
        public float TotalSpeechDuration { get; private set; }
        public int UtteranceCount { get; private set; }

        public event Action OnSpeechStarted;
        public event Action<float> OnSpeechEnded; // duration of the utterance
        public event Action<float> OnAmplitudeUpdated; // current amplitude level

        private AudioClip micClip;
        private string micDevice;
        private float[] samples;
        private float speechStartTime;
        private float lastSpeechTime;
        private bool micInitialized;

        private readonly List<SpeechActivityRecord> speechLog = new List<SpeechActivityRecord>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            samples = new float[sampleSize];
        }

        public void StartListening()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[SpeechDetector] No microphone found.");
                return;
            }

            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 1, 44100);
            micInitialized = true;
            IsListening = true;
            TotalSpeechDuration = 0f;
            UtteranceCount = 0;
            speechLog.Clear();

            Debug.Log($"[SpeechDetector] Listening on: {micDevice}");
        }

        public void StopListening()
        {
            if (micInitialized && !string.IsNullOrEmpty(micDevice))
            {
                Microphone.End(micDevice);
                micInitialized = false;
            }

            // End any active speech
            if (IsSpeaking)
            {
                EndSpeech();
            }

            IsListening = false;
            Debug.Log($"[SpeechDetector] Stopped. Total speech: {TotalSpeechDuration:F1}s, Utterances: {UtteranceCount}");
        }

        private void Update()
        {
            if (!IsListening || !micInitialized) return;

            UpdateAmplitude();
            UpdateSpeechState();
        }

        private void UpdateAmplitude()
        {
            int micPosition = Microphone.GetPosition(micDevice);
            if (micPosition <= 0 || micClip == null) return;

            int startPos = Mathf.Max(0, micPosition - samples.Length);
            micClip.GetData(samples, startPos);

            // Calculate peak amplitude
            float maxAmp = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float abs = Mathf.Abs(samples[i]);
                if (abs > maxAmp) maxAmp = abs;
            }

            CurrentAmplitude = maxAmp;
            OnAmplitudeUpdated?.Invoke(CurrentAmplitude);
        }

        private void UpdateSpeechState()
        {
            bool aboveThreshold = CurrentAmplitude > amplitudeThreshold;

            if (aboveThreshold)
            {
                lastSpeechTime = Time.realtimeSinceStartup;

                if (!IsSpeaking)
                {
                    StartSpeech();
                }
                else
                {
                    CurrentSpeechDuration = Time.realtimeSinceStartup - speechStartTime;
                }
            }
            else if (IsSpeaking)
            {
                // Check if silence has lasted long enough to end speech
                float silenceDuration = Time.realtimeSinceStartup - lastSpeechTime;
                if (silenceDuration >= silenceTimeout)
                {
                    EndSpeech();
                }
            }
        }

        private void StartSpeech()
        {
            IsSpeaking = true;
            speechStartTime = Time.realtimeSinceStartup;
            CurrentSpeechDuration = 0f;
            UtteranceCount++;

            // Log
            speechLog.Add(new SpeechActivityRecord
            {
                timestamp = speechStartTime,
                eventType = "start",
                amplitude = CurrentAmplitude,
                duration = 0f
            });

            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.FireEvent(ScenarioEventType.UserSpeechDetected,
                    $"Utterance #{UtteranceCount} started");

            OnSpeechStarted?.Invoke();

            if (logSpeechEvents)
                Debug.Log($"[SpeechDetector] Speech started (utterance #{UtteranceCount})");
        }

        private void EndSpeech()
        {
            float duration = Time.realtimeSinceStartup - speechStartTime;
            IsSpeaking = false;
            TotalSpeechDuration += duration;
            CurrentSpeechDuration = 0f;

            // Log
            speechLog.Add(new SpeechActivityRecord
            {
                timestamp = Time.realtimeSinceStartup,
                eventType = "stop",
                amplitude = 0f,
                duration = duration
            });

            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.FireEvent(ScenarioEventType.UserSpeechEnded,
                    $"Utterance #{UtteranceCount} ended, duration={duration:F2}s");

            OnSpeechEnded?.Invoke(duration);

            if (logSpeechEvents)
                Debug.Log($"[SpeechDetector] Speech ended. Duration: {duration:F2}s, Total: {TotalSpeechDuration:F1}s");
        }

        /// <summary>
        /// Get all speech activity records for session data export.
        /// </summary>
        public List<SpeechActivityRecord> GetSpeechLog()
        {
            return new List<SpeechActivityRecord>(speechLog);
        }

        /// <summary>
        /// Flush speech data into the session.
        /// </summary>
        public void FlushToSession(SessionData sessionData)
        {
            sessionData.speechData.AddRange(speechLog);
        }
    }
}

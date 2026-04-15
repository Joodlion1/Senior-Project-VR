using UnityEngine;
using System;

namespace VRDiagnostics
{
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [Header("Session Settings")]
        [SerializeField] private float maxSessionDuration = 900f; // 15 minutes
        [SerializeField] private float warningDuration = 840f;    // warn at 14 minutes

        public SessionData CurrentSession { get; private set; }
        public bool IsSessionActive { get; private set; }
        public float SessionElapsed => IsSessionActive ? Time.realtimeSinceStartup - sessionStartRealtime : 0f;

        public event Action OnSessionStarted;
        public event Action OnSessionEnded;
        public event Action OnMaxDurationReached;

        private float sessionStartRealtime;
        private bool warningFired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!IsSessionActive) return;

            // Check session duration limits
            float elapsed = SessionElapsed;

            if (!warningFired && elapsed >= warningDuration)
            {
                warningFired = true;
                Debug.LogWarning($"[SessionManager] Session approaching max duration ({elapsed:F0}s / {maxSessionDuration:F0}s)");
            }

            if (elapsed >= maxSessionDuration)
            {
                Debug.LogWarning("[SessionManager] Max session duration reached. Ending session.");
                OnMaxDurationReached?.Invoke();
                EndSession();
            }
        }

        public void StartSession()
        {
            CurrentSession = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.UtcNow.ToString("o")
            };

            sessionStartRealtime = Time.realtimeSinceStartup;
            warningFired = false;
            IsSessionActive = true;

            Debug.Log($"[SessionManager] Session started: {CurrentSession.sessionId}");
            OnSessionStarted?.Invoke();
        }

        public void EndSession()
        {
            if (!IsSessionActive) return;

            CurrentSession.endTime = DateTime.UtcNow.ToString("o");
            CurrentSession.durationSeconds = SessionElapsed;
            IsSessionActive = false;

            Debug.Log($"[SessionManager] Session ended. Duration: {CurrentSession.durationSeconds:F1}s");
            OnSessionEnded?.Invoke();
        }

        /// <summary>
        /// Add a task result to the session data.
        /// </summary>
        public void RecordTaskResult(string taskName, bool successful, float speechDuration, float taskDuration)
        {
            if (CurrentSession == null) return;

            CurrentSession.taskResults.Add(new TaskResult
            {
                taskName = taskName,
                successful = successful,
                speechDuration = speechDuration,
                taskDuration = taskDuration,
                responseType = successful ? "Successful" : "Unsuccessful"
            });

            Debug.Log($"[SessionManager] Task result recorded: {taskName} = {(successful ? "Successful" : "Unsuccessful")}");
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace VRDiagnostics
{
    public class EventLogger : MonoBehaviour
    {
        public static EventLogger Instance { get; private set; }

        private readonly List<ScenarioEventRecord> eventLog = new List<ScenarioEventRecord>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Subscribe to ScenarioManager events when available
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioEvent += RecordEvent;
            }
        }

        private void Start()
        {
            // Also try subscribing in Start in case ScenarioManager initializes in Awake
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioEvent -= RecordEvent; // prevent double sub
                ScenarioManager.Instance.OnScenarioEvent += RecordEvent;
            }
        }

        private void OnDisable()
        {
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioEvent -= RecordEvent;
            }
        }

        private void RecordEvent(ScenarioEventRecord record)
        {
            eventLog.Add(record);
            Debug.Log($"[EventLogger] {record.eventType} | State: {record.scenarioState} | {record.details} | t={record.timestamp:F3}");
        }

        /// <summary>
        /// Manually log a custom event (e.g., from other systems like speech or gaze).
        /// </summary>
        public void LogEvent(ScenarioEventType eventType, string details = "")
        {
            var state = ScenarioManager.Instance != null
                ? ScenarioManager.Instance.CurrentState
                : ScenarioState.None;

            var record = new ScenarioEventRecord(eventType, state, details);
            eventLog.Add(record);
        }

        public List<ScenarioEventRecord> GetEventLog()
        {
            return new List<ScenarioEventRecord>(eventLog);
        }

        /// <summary>
        /// Write all events to the SessionData and clear the log.
        /// Called by DataExporter before saving.
        /// </summary>
        public void FlushToSession(SessionData sessionData)
        {
            sessionData.scenarioEvents.AddRange(eventLog);
            Debug.Log($"[EventLogger] Flushed {eventLog.Count} events to session data.");
        }

        public void Clear()
        {
            eventLog.Clear();
        }
    }
}

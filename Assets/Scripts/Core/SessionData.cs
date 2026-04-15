using System;
using System.Collections.Generic;

namespace VRDiagnostics
{
    [Serializable]
    public class SessionData
    {
        public string sessionId;
        public string startTime;       // ISO 8601 format
        public string endTime;
        public float durationSeconds;

        // Scenario events timeline
        public List<ScenarioEventRecord> scenarioEvents = new List<ScenarioEventRecord>();

        // Biometric data
        public List<GazeDataPoint> gazeData = new List<GazeDataPoint>();
        public List<HeartRateDataPoint> heartRateData = new List<HeartRateDataPoint>();
        public List<SpeechActivityRecord> speechData = new List<SpeechActivityRecord>();

        // Task results
        public List<TaskResult> taskResults = new List<TaskResult>();
    }

    [Serializable]
    public class HeartRateDataPoint
    {
        public float timestamp;
        public int heartRate;
        public float confidence;
    }

    [Serializable]
    public class SpeechActivityRecord
    {
        public float timestamp;
        public string eventType; // "start" or "stop"
        public float amplitude;
        public float duration;   // only set on "stop" events
    }

    [Serializable]
    public class TaskResult
    {
        public string taskName;
        public bool successful;
        public float speechDuration;
        public float taskDuration;
        public string responseType; // "Successful" or "Unsuccessful"
    }
}

using System;

namespace VRDiagnostics
{
    public enum ScenarioEventType
    {
        TaskStarted,
        TaskCompleted,
        NPCReaction,
        UserSpeechDetected,
        UserSpeechEnded,
        UserGazeEvent,
        SceneTransition,
        FadeStarted,
        FadeCompleted,
        SessionStarted,
        SessionEnded,
        SessionPaused,
        SessionResumed,
        UserStoodUp,
        UserSatDown,
        DialoguePlayed,
        UIShown,
        UIHidden
    }

    [Serializable]
    public class ScenarioEventRecord
    {
        public float timestamp;
        public ScenarioEventType eventType;
        public ScenarioState scenarioState;
        public string details;

        public ScenarioEventRecord(ScenarioEventType type, ScenarioState state, string details = "")
        {
            this.timestamp = UnityEngine.Time.realtimeSinceStartup;
            this.eventType = type;
            this.scenarioState = state;
            this.details = details;
        }
    }
}

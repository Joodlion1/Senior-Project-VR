using UnityEngine;
using UnityEngine.Events;
using System;

namespace VRDiagnostics
{
    public class ScenarioManager : MonoBehaviour
    {
        public static ScenarioManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private ScenarioState currentState = ScenarioState.None;

        [Header("Events")]
        public UnityEvent<ScenarioState, ScenarioState> OnStateChanged; // old state, new state
        public UnityEvent OnScenarioPaused;
        public UnityEvent OnScenarioResumed;

        public ScenarioState CurrentState => currentState;
        public bool IsPaused { get; private set; }
        public float StateStartTime { get; private set; }
        public float TimeInCurrentState => Time.realtimeSinceStartup - StateStartTime;

        // C# events for script-to-script communication
        public event Action<ScenarioEventRecord> OnScenarioEvent;

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
        /// Begin the scenario from Onboarding.
        /// Called by OnboardingManager when user is ready or by external trigger.
        /// </summary>
        public void StartScenario()
        {
            TransitionTo(ScenarioState.Onboarding);
            FireEvent(ScenarioEventType.SessionStarted, "Scenario started");
        }

        /// <summary>
        /// Transition to a new scenario state.
        /// </summary>
        public void TransitionTo(ScenarioState newState)
        {
            if (IsPaused)
            {
                Debug.LogWarning("[ScenarioManager] Cannot transition while paused.");
                return;
            }

            var oldState = currentState;
            currentState = newState;
            StateStartTime = Time.realtimeSinceStartup;

            Debug.Log($"[ScenarioManager] State: {oldState} -> {newState}");

            OnStateChanged?.Invoke(oldState, newState);
            FireEvent(ScenarioEventType.SceneTransition, $"{oldState} -> {newState}");

            // Auto-fire task events
            switch (newState)
            {
                case ScenarioState.Task1_Intro:
                    FireEvent(ScenarioEventType.TaskStarted, "Task1_IntroduceYourself");
                    break;
                case ScenarioState.Task2_GroupWork:
                    FireEvent(ScenarioEventType.TaskStarted, "Task2_WorkWithTeam");
                    break;
                case ScenarioState.Task3_Presentation:
                    FireEvent(ScenarioEventType.TaskStarted, "Task3_GivePresentation");
                    break;
                case ScenarioState.Task1_Response:
                    FireEvent(ScenarioEventType.TaskCompleted, "Task1_IntroduceYourself");
                    break;
                case ScenarioState.Task2_Response:
                    FireEvent(ScenarioEventType.TaskCompleted, "Task2_WorkWithTeam");
                    break;
                case ScenarioState.SessionEnd:
                    FireEvent(ScenarioEventType.TaskCompleted, "Task3_GivePresentation");
                    FireEvent(ScenarioEventType.SessionEnded, "Scenario completed");
                    break;
            }
        }

        /// <summary>
        /// Advance to the next logical state in the scenario flow.
        /// </summary>
        public void AdvanceToNextState()
        {
            ScenarioState next = currentState switch
            {
                ScenarioState.None => ScenarioState.Onboarding,
                ScenarioState.Onboarding => ScenarioState.Task1_Intro,
                ScenarioState.Task1_Intro => ScenarioState.Task1_Response,
                ScenarioState.Task1_Response => ScenarioState.Task2_Transition,
                ScenarioState.Task2_Transition => ScenarioState.Task2_GroupWork,
                ScenarioState.Task2_GroupWork => ScenarioState.Task2_Response,
                ScenarioState.Task2_Response => ScenarioState.Task3_Presentation,
                ScenarioState.Task3_Presentation => ScenarioState.Task3_Response,
                ScenarioState.Task3_Response => ScenarioState.SessionEnd,
                _ => ScenarioState.SessionEnd
            };

            TransitionTo(next);
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            OnScenarioPaused?.Invoke();
            FireEvent(ScenarioEventType.SessionPaused, "User paused");
            Debug.Log("[ScenarioManager] Paused");
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            OnScenarioResumed?.Invoke();
            FireEvent(ScenarioEventType.SessionResumed, "User resumed");
            Debug.Log("[ScenarioManager] Resumed");
        }

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        /// <summary>
        /// End the session early (user chose to exit).
        /// </summary>
        public void EndSessionEarly()
        {
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = 1f;
            }
            FireEvent(ScenarioEventType.SessionEnded, "Session ended early by user");
            TransitionTo(ScenarioState.SessionEnd);
        }

        /// <summary>
        /// Fire a scenario event that gets logged by EventLogger and used for biometric sync.
        /// </summary>
        public void FireEvent(ScenarioEventType eventType, string details = "")
        {
            var record = new ScenarioEventRecord(eventType, currentState, details);
            OnScenarioEvent?.Invoke(record);
        }
    }
}

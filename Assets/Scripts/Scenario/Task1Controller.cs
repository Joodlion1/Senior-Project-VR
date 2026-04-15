using UnityEngine;
using System.Collections;

namespace VRDiagnostics
{
    /// <summary>
    /// Task 1: Introduce Yourself to the Class.
    /// Handles the full flow from teacher introduction to user response evaluation.
    /// </summary>
    public class Task1Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TeacherController teacher;
        [SerializeField] private TaskInstructionPanel taskPanel;
        [SerializeField] private AudioSpectrumUI audioSpectrum;

        [Header("Timing")]
        [SerializeField] private float talkingPointsDelay = 0.5f;
        [SerializeField] private float spectrumShowDelay = 1f;

        private float taskStartTime;
        private float speechDurationAtStart;
        private bool isActive;

        private void Start()
        {
            if (teacher == null)
                teacher = FindAnyObjectByType<TeacherController>();

            // Listen for state changes
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.OnStateChanged.AddListener(OnStateChanged);
        }

        private void OnDestroy()
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.OnStateChanged.RemoveListener(OnStateChanged);
        }

        private void OnStateChanged(ScenarioState oldState, ScenarioState newState)
        {
            if (newState == ScenarioState.Task1_Intro)
                StartCoroutine(RunTask1Intro());
            else if (newState == ScenarioState.Task1_Response)
                StartCoroutine(RunTask1Response());
        }

        /// <summary>
        /// Phase 1: Teacher introduces the new student.
        /// </summary>
        private IEnumerator RunTask1Intro()
        {
            isActive = true;
            Debug.Log("[Task1Controller] Starting Task 1 — Introduce Yourself");

            // Teacher introduces and points at user
            bool teacherDone = false;
            if (teacher != null)
                teacher.PlayTask1Introduction(() => teacherDone = true);
            else
                teacherDone = true;

            yield return new WaitUntil(() => teacherDone);

            // Show task instruction panel
            yield return new WaitForSeconds(talkingPointsDelay);
            if (taskPanel != null)
                taskPanel.ShowTask(1);

            // Show audio spectrum to indicate user should speak
            yield return new WaitForSeconds(spectrumShowDelay);
            if (audioSpectrum != null)
                audioSpectrum.Activate();

            // Start listening for speech
            if (SpeechDetector.Instance != null && !SpeechDetector.Instance.IsListening)
                SpeechDetector.Instance.StartListening();

            // Record starting speech state
            taskStartTime = Time.realtimeSinceStartup;
            speechDurationAtStart = SpeechDetector.Instance != null
                ? SpeechDetector.Instance.TotalSpeechDuration : 0f;

            // Transition to response state — this triggers waiting for the user
            ScenarioManager.Instance.AdvanceToNextState();
        }

        /// <summary>
        /// Phase 2: Wait for user to speak, then evaluate and react.
        /// </summary>
        private IEnumerator RunTask1Response()
        {
            float timeout = ResponseEvaluator.Instance != null
                ? ResponseEvaluator.Instance.ResponseTimeout : 30f;

            // Wait for speech or timeout
            float elapsed = 0f;
            bool speechDetected = false;

            while (elapsed < timeout)
            {
                elapsed += Time.deltaTime;

                if (SpeechDetector.Instance != null && SpeechDetector.Instance.IsSpeaking)
                    speechDetected = true;

                // If user has spoken and then stopped, allow a brief buffer before evaluating
                if (speechDetected && SpeechDetector.Instance != null && !SpeechDetector.Instance.IsSpeaking)
                {
                    float speechDuration = SpeechDetector.Instance.TotalSpeechDuration - speechDurationAtStart;
                    if (speechDuration >= (ResponseEvaluator.Instance != null
                        ? ResponseEvaluator.Instance.MinSpeechDuration : 3f))
                    {
                        // Wait a moment after speech ends before reacting
                        yield return new WaitForSeconds(1.5f);
                        break;
                    }
                }

                yield return null;
            }

            // Hide UI
            if (taskPanel != null)
                taskPanel.Hide();
            if (audioSpectrum != null)
                audioSpectrum.Deactivate();

            // Evaluate
            float totalSpeech = SpeechDetector.Instance != null
                ? SpeechDetector.Instance.TotalSpeechDuration - speechDurationAtStart : 0f;

            ResponseResult result = ResponseEvaluator.Instance != null
                ? ResponseEvaluator.Instance.Evaluate(totalSpeech, speechDetected)
                : (speechDetected ? ResponseResult.Successful : ResponseResult.Unsuccessful);

            // Record task result
            float taskDuration = Time.realtimeSinceStartup - taskStartTime;
            if (SessionManager.Instance != null)
                SessionManager.Instance.RecordTaskResult("Task1_IntroduceYourself", result == ResponseResult.Successful, totalSpeech, taskDuration);

            // Play NPC reactions
            bool reactionsDone = false;
            if (NPCReactionOrchestrator.Instance != null)
                NPCReactionOrchestrator.Instance.PlayTask1Reactions(result, () => reactionsDone = true);
            else
                reactionsDone = true;

            yield return new WaitUntil(() => reactionsDone);

            isActive = false;

            // Advance to Task 2 transition
            yield return new WaitForSeconds(1f);
            ScenarioManager.Instance.AdvanceToNextState();
        }
    }
}

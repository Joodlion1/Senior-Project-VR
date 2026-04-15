using UnityEngine;
using System.Collections;

namespace VRDiagnostics
{
    /// <summary>
    /// Task 3: Give a Presentation.
    /// Handles: teacher intro, all NPCs look at user, user speaks, evaluation, reactions, session end.
    /// </summary>
    public class Task3Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TeacherController teacher;
        [SerializeField] private TaskInstructionPanel taskPanel;
        [SerializeField] private AudioSpectrumUI audioSpectrum;

        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 1.5f;
        [SerializeField] private float holdBlackDuration = 1f;

        private float taskStartTime;
        private float speechDurationAtStart;

        private void Start()
        {
            if (teacher == null)
                teacher = FindAnyObjectByType<TeacherController>();

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
            if (newState == ScenarioState.Task3_Presentation)
                StartCoroutine(RunTask3Presentation());
            else if (newState == ScenarioState.Task3_Response)
                StartCoroutine(RunTask3Response());
        }

        /// <summary>
        /// Teacher announces presentations, transition back to classroom, user presents.
        /// </summary>
        private IEnumerator RunTask3Presentation()
        {
            Debug.Log("[Task3Controller] Starting Task 3 — Give a Presentation");

            // Fade back to classroom arrangement if currently at round table
            if (SceneArrangementManager.Instance != null && !SceneArrangementManager.Instance.IsClassroomActive)
            {
                if (ScreenFader.Instance != null)
                {
                    bool fadeDone = false;
                    ScreenFader.Instance.FadeOutAndIn(
                        fadeDuration,
                        holdBlackDuration,
                        whileBlack: () =>
                        {
                            SceneArrangementManager.Instance.SwitchToClassroom();
                        },
                        onComplete: () => fadeDone = true
                    );
                    yield return new WaitUntil(() => fadeDone);
                }
                else
                {
                    SceneArrangementManager.Instance.SwitchToClassroom();
                }
            }

            // Teacher announces presentation time
            bool teacherDone = false;
            if (teacher != null)
                teacher.PlayTask3Introduction(() => teacherDone = true);
            else
                teacherDone = true;

            yield return new WaitUntil(() => teacherDone);

            // All NPCs look at user
            if (NPCReactionOrchestrator.Instance != null)
                NPCReactionOrchestrator.Instance.AllNPCsLookAtUser();

            // Show task instruction
            if (taskPanel != null)
                taskPanel.ShowTask(3);

            yield return new WaitForSeconds(1f);

            // Show audio spectrum
            if (audioSpectrum != null)
                audioSpectrum.Activate();

            // Start listening
            if (SpeechDetector.Instance != null && !SpeechDetector.Instance.IsListening)
                SpeechDetector.Instance.StartListening();

            taskStartTime = Time.realtimeSinceStartup;
            speechDurationAtStart = SpeechDetector.Instance != null
                ? SpeechDetector.Instance.TotalSpeechDuration : 0f;

            // Advance to response state
            ScenarioManager.Instance.AdvanceToNextState();
        }

        /// <summary>
        /// Wait for speech, evaluate, play reactions, end session.
        /// </summary>
        private IEnumerator RunTask3Response()
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

                if (speechDetected && SpeechDetector.Instance != null && !SpeechDetector.Instance.IsSpeaking)
                {
                    float speechDuration = SpeechDetector.Instance.TotalSpeechDuration - speechDurationAtStart;
                    if (speechDuration >= (ResponseEvaluator.Instance != null
                        ? ResponseEvaluator.Instance.MinSpeechDuration : 3f))
                    {
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
                SessionManager.Instance.RecordTaskResult("Task3_GivePresentation", result == ResponseResult.Successful, totalSpeech, taskDuration);

            // Play NPC reactions
            bool reactionsDone = false;
            if (NPCReactionOrchestrator.Instance != null)
                NPCReactionOrchestrator.Instance.PlayTask3Reactions(result, () => reactionsDone = true);
            else
                reactionsDone = true;

            yield return new WaitUntil(() => reactionsDone);

            // Reset NPCs
            if (NPCReactionOrchestrator.Instance != null)
                NPCReactionOrchestrator.Instance.ResetAllNPCs();

            yield return new WaitForSeconds(2f);

            // End the session
            ScenarioManager.Instance.AdvanceToNextState();
        }
    }
}

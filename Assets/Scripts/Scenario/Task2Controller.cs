using UnityEngine;
using System.Collections;

namespace VRDiagnostics
{
    /// <summary>
    /// Task 2: Work with a Team (Group Discussion).
    /// Handles: transition fade, group discussion, user response, peer feedback.
    /// </summary>
    public class Task2Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TeacherController teacher;
        [SerializeField] private TaskInstructionPanel taskPanel;
        [SerializeField] private AudioSpectrumUI audioSpectrum;

        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 1.5f;
        [SerializeField] private float holdBlackDuration = 1f;

        [Header("Timing")]
        [SerializeField] private float groupDiscussionDuration = 5f;

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
            if (newState == ScenarioState.Task2_Transition)
                StartCoroutine(RunTask2Transition());
            else if (newState == ScenarioState.Task2_GroupWork)
                StartCoroutine(RunTask2GroupWork());
            else if (newState == ScenarioState.Task2_Response)
                StartCoroutine(RunTask2Response());
        }

        /// <summary>
        /// Teacher announces group exercise, then fade to round table.
        /// </summary>
        private IEnumerator RunTask2Transition()
        {
            Debug.Log("[Task2Controller] Starting Task 1 → Task 2 Transition");

            // Teacher explains group exercise
            bool teacherDone = false;
            if (teacher != null)
                teacher.PlayTask2Transition(() => teacherDone = true);
            else
                teacherDone = true;

            yield return new WaitUntil(() => teacherDone);

            yield return new WaitForSeconds(1f);

            // Fade to black, switch arrangement, fade back in
            if (ScreenFader.Instance != null)
            {
                bool fadeDone = false;
                ScreenFader.Instance.FadeOutAndIn(
                    fadeDuration,
                    holdBlackDuration,
                    whileBlack: () =>
                    {
                        // Switch to round table layout while screen is black
                        if (SceneArrangementManager.Instance != null)
                            SceneArrangementManager.Instance.SwitchToRoundTable();
                    },
                    onComplete: () => fadeDone = true
                );
                yield return new WaitUntil(() => fadeDone);
            }
            else
            {
                // No fader — just switch directly
                if (SceneArrangementManager.Instance != null)
                    SceneArrangementManager.Instance.SwitchToRoundTable();
            }

            // Advance to group work state
            ScenarioManager.Instance.AdvanceToNextState();
        }

        /// <summary>
        /// Group discussion: NPCs discuss, then ask user for opinion.
        /// </summary>
        private IEnumerator RunTask2GroupWork()
        {
            Debug.Log("[Task2Controller] Starting Task 2 — Group Work");

            // Show task instruction
            if (taskPanel != null)
                taskPanel.ShowTask(2);

            yield return new WaitForSeconds(1f);

            // NPCs discuss among themselves
            if (NPCReactionOrchestrator.Instance != null)
                NPCReactionOrchestrator.Instance.PlayGroupDiscussion();

            // Wait for group discussion to play out
            yield return new WaitForSeconds(groupDiscussionDuration);

            // After student asks "What's your opinion?" — show audio spectrum
            if (audioSpectrum != null)
                audioSpectrum.Activate();

            // Start listening
            if (SpeechDetector.Instance != null && !SpeechDetector.Instance.IsListening)
                SpeechDetector.Instance.StartListening();

            taskStartTime = Time.realtimeSinceStartup;
            speechDurationAtStart = SpeechDetector.Instance != null
                ? SpeechDetector.Instance.TotalSpeechDuration : 0f;

            // Advance to response waiting
            ScenarioManager.Instance.AdvanceToNextState();
        }

        /// <summary>
        /// Wait for user speech, evaluate, play peer response.
        /// </summary>
        private IEnumerator RunTask2Response()
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
                SessionManager.Instance.RecordTaskResult("Task2_WorkWithTeam", result == ResponseResult.Successful, totalSpeech, taskDuration);

            // Play peer response
            if (NPCReactionOrchestrator.Instance != null)
            {
                if (result == ResponseResult.Successful)
                    NPCReactionOrchestrator.Instance.PlayGroupPositiveResponse();
                else
                    NPCReactionOrchestrator.Instance.PlayGroupUnsuccessfulResponse();
            }

            yield return new WaitForSeconds(3f);

            // Advance to Task 3
            ScenarioManager.Instance.AdvanceToNextState();
        }
    }
}

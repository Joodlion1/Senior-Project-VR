using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VRDiagnostics
{
    /// <summary>
    /// Handles the session end sequence: stop recording, fade out, show completion UI, export data.
    /// </summary>
    public class SessionEndController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject sessionCompleteCanvas;
        [SerializeField] private Text completionMessageText;
        [SerializeField] private Text sessionSummaryText;

        [Header("Timing")]
        [SerializeField] private float fadeOutDuration = 2f;
        [SerializeField] private float holdBlackDuration = 1f;
        [SerializeField] private float fadeInDuration = 1f;

        private void Start()
        {
            if (sessionCompleteCanvas != null)
                sessionCompleteCanvas.SetActive(false);

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
            if (newState == ScenarioState.SessionEnd)
                StartCoroutine(RunSessionEnd());
        }

        private IEnumerator RunSessionEnd()
        {
            Debug.Log("[SessionEndController] Session ending...");

            // Stop speech detection
            if (SpeechDetector.Instance != null && SpeechDetector.Instance.IsListening)
                SpeechDetector.Instance.StopListening();

            // Stop eye tracking
            var eyeTracker = FindAnyObjectByType<EyeTrackingManager>();
            if (eyeTracker != null)
                eyeTracker.StopTracking();

            // Stop heart rate capture
            if (HeartRateCapture.Instance != null)
                HeartRateCapture.Instance.StopCapture();

            // Stop speech activity logger
            if (SpeechActivityLogger.Instance != null)
                SpeechActivityLogger.Instance.StopLogging();

            // Stop biometric synchronizer
            BiometricSynchronizer.StopRecording();

            // Fade to black
            if (ScreenFader.Instance != null)
            {
                bool fadeDone = false;
                ScreenFader.Instance.FadeOut(fadeOutDuration, () => fadeDone = true);
                yield return new WaitUntil(() => fadeDone);
            }

            yield return new WaitForSeconds(holdBlackDuration);

            // End the session (triggers data export via DataExporter)
            if (SessionManager.Instance != null)
                SessionManager.Instance.EndSession();

            // Show completion UI
            if (sessionCompleteCanvas != null)
            {
                sessionCompleteCanvas.SetActive(true);

                if (completionMessageText != null)
                    completionMessageText.text = "Session Complete\n\nThank you for participating.\nYou may now remove the headset.";

                if (sessionSummaryText != null)
                    UpdateSummary();
            }

            // Fade back in to show the completion screen
            if (ScreenFader.Instance != null)
            {
                bool fadeDone = false;
                ScreenFader.Instance.FadeIn(fadeInDuration, () => fadeDone = true);
                yield return new WaitUntil(() => fadeDone);
            }

            Debug.Log("[SessionEndController] Session complete. Data exported.");
        }

        private void UpdateSummary()
        {
            if (sessionSummaryText == null) return;

            var session = SessionManager.Instance?.CurrentSession;
            if (session == null)
            {
                sessionSummaryText.text = "";
                return;
            }

            int minutes = (int)(session.durationSeconds / 60f);
            int seconds = (int)(session.durationSeconds % 60f);
            int tasksCompleted = session.taskResults.Count;

            sessionSummaryText.text = $"Duration: {minutes:00}:{seconds:00}\nTasks Completed: {tasksCompleted}/3";

            // Show export path in debug
            if (DataExporter.Instance != null && !string.IsNullOrEmpty(DataExporter.Instance.LastExportPath))
            {
                Debug.Log($"[SessionEndController] Data saved to: {DataExporter.Instance.LastExportPath}");
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace VRDiagnostics
{
    public class OnboardingManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject onboardingCanvas;
        [SerializeField] private Text welcomeText;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject speechCheckIndicator;
        [SerializeField] private GameObject trackingIndicator;
        [SerializeField] private Button startButton;

        [Header("Settings")]
        [SerializeField] private float speechCheckDuration = 3f;
        [SerializeField] private float micThreshold = 0.01f;

        public event Action OnOnboardingComplete;

        private bool speechCheckPassed;
        private bool trackingCheckPassed;
        private TrackingValidator trackingValidator;

        private void Start()
        {
            trackingValidator = FindAnyObjectByType<TrackingValidator>();

            if (startButton != null)
            {
                startButton.interactable = false;
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (welcomeText != null)
                welcomeText.text = "Welcome to the VR Diagnostic Experience\n\nYou will participate in a classroom scenario with 3 social tasks.\nYour eye gaze, heart rate, and speech will be recorded.\n\nYou can pause or exit at any time.";

            if (statusText != null)
                statusText.text = "Checking systems...";

            // Auto-start onboarding when scene loads
            StartOnboarding();
        }

        /// <summary>
        /// Call this to begin the onboarding sequence.
        /// </summary>
        public void StartOnboarding()
        {
            if (onboardingCanvas != null)
                onboardingCanvas.SetActive(true);

            StartCoroutine(OnboardingSequence());
        }

        private IEnumerator OnboardingSequence()
        {
            // Step 1: Check 6DoF tracking
            if (statusText != null)
                statusText.text = "Checking VR tracking...";

            yield return new WaitForSeconds(1f);

            if (trackingValidator != null)
            {
                trackingCheckPassed = trackingValidator.Validate6DoF();
            }
            else
            {
                trackingCheckPassed = true; // Skip if no validator
            }

            if (trackingIndicator != null)
            {
                var img = trackingIndicator.GetComponent<Image>();
                if (img != null)
                    img.color = trackingCheckPassed ? Color.green : Color.red;
            }

            if (statusText != null)
                statusText.text = trackingCheckPassed
                    ? "VR Tracking: OK"
                    : "VR Tracking: Issue detected. Try looking around slowly.";

            yield return new WaitForSeconds(1f);

            // Step 2: Speech check
            if (statusText != null)
                statusText.text = "Speech Check: Please say something...";

            if (speechCheckIndicator != null)
                speechCheckIndicator.SetActive(true);

            yield return StartCoroutine(SpeechCheckRoutine());

            if (speechCheckIndicator != null)
            {
                var img = speechCheckIndicator.GetComponent<Image>();
                if (img != null)
                    img.color = speechCheckPassed ? Color.green : Color.yellow;
            }

            if (statusText != null)
                statusText.text = speechCheckPassed
                    ? "Speech Check: OK\n\nYou're all set! Press Start when ready."
                    : "Speech Check: No audio detected.\nYou can still continue — press Start when ready.";

            // Enable start button
            if (startButton != null)
                startButton.interactable = true;
        }

        private IEnumerator SpeechCheckRoutine()
        {
            speechCheckPassed = false;

            // Try to start microphone
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[OnboardingManager] No microphone found.");
                yield break;
            }

            string micDevice = Microphone.devices[0];
            AudioClip micClip = Microphone.Start(micDevice, true, 1, 44100);
            float[] samples = new float[128];
            float elapsed = 0f;

            while (elapsed < speechCheckDuration)
            {
                elapsed += Time.deltaTime;

                // Read mic level
                int micPosition = Microphone.GetPosition(micDevice);
                if (micPosition > 0 && micClip != null)
                {
                    micClip.GetData(samples, Mathf.Max(0, micPosition - samples.Length));
                    float maxLevel = 0f;
                    foreach (float s in samples)
                    {
                        float abs = Mathf.Abs(s);
                        if (abs > maxLevel) maxLevel = abs;
                    }

                    if (maxLevel > micThreshold)
                    {
                        speechCheckPassed = true;
                        break;
                    }
                }

                yield return null;
            }

            Microphone.End(micDevice);
        }

        private void OnStartClicked()
        {
            if (onboardingCanvas != null)
                onboardingCanvas.SetActive(false);

            // Start the session and scenario
            if (SessionManager.Instance != null)
                SessionManager.Instance.StartSession();

            // Start all biometric recording
            BiometricSynchronizer.StartRecording();

            if (HeartRateCapture.Instance != null)
                HeartRateCapture.Instance.StartCapture();

            if (SpeechActivityLogger.Instance != null)
                SpeechActivityLogger.Instance.StartLogging();

            var eyeTracker = FindAnyObjectByType<EyeTrackingManager>();
            if (eyeTracker != null)
                eyeTracker.StartTracking();

            // Calibrate seated height for stand-up detection
            var standUpDetector = FindAnyObjectByType<StandUpDetector>();
            if (standUpDetector != null)
                standUpDetector.CalibrateSeatedHeight();

            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.StartScenario();       // → Onboarding state
                ScenarioManager.Instance.AdvanceToNextState();   // → Task1_Intro state
            }

            OnOnboardingComplete?.Invoke();
            Debug.Log("[OnboardingManager] Onboarding complete. Scenario started.");
        }
    }
}

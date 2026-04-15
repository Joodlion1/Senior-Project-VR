using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace VRDiagnostics
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuCanvas;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button endSessionButton;
        [SerializeField] private Text statusText;

        [Header("Input")]
        [SerializeField] private InputActionReference menuButtonAction;

        private bool isVisible;

        private void Start()
        {
            if (pauseMenuCanvas != null)
                pauseMenuCanvas.SetActive(false);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (endSessionButton != null)
                endSessionButton.onClick.AddListener(OnEndSessionClicked);
        }

        private void OnEnable()
        {
            if (menuButtonAction != null && menuButtonAction.action != null)
            {
                menuButtonAction.action.Enable();
                menuButtonAction.action.performed += OnMenuButtonPressed;
            }
        }

        private void OnDisable()
        {
            if (menuButtonAction != null && menuButtonAction.action != null)
            {
                menuButtonAction.action.performed -= OnMenuButtonPressed;
            }
        }

        private void Update()
        {
            // Fallback: keyboard Escape key for editor testing
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePauseMenu();
            }
        }

        private void OnMenuButtonPressed(InputAction.CallbackContext context)
        {
            TogglePauseMenu();
        }

        public void TogglePauseMenu()
        {
            if (isVisible)
                HideMenu();
            else
                ShowMenu();
        }

        public void ShowMenu()
        {
            isVisible = true;

            if (pauseMenuCanvas != null)
                pauseMenuCanvas.SetActive(true);

            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.Pause();

            UpdateStatusText();
        }

        public void HideMenu()
        {
            isVisible = false;

            if (pauseMenuCanvas != null)
                pauseMenuCanvas.SetActive(false);

            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.Resume();
        }

        private void OnResumeClicked()
        {
            HideMenu();
        }

        private void OnEndSessionClicked()
        {
            HideMenu();

            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.EndSessionEarly();
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;

            string state = ScenarioManager.Instance != null
                ? ScenarioManager.Instance.CurrentState.ToString()
                : "Unknown";

            float elapsed = SessionManager.Instance != null
                ? SessionManager.Instance.SessionElapsed
                : 0f;

            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);

            statusText.text = $"Current: {state}\nTime: {minutes:00}:{seconds:00}\n\nAre you comfortable continuing?";
        }
    }
}

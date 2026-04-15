using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VRDiagnostics
{
    public class VRUIManager : MonoBehaviour
    {
        public static VRUIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject onboardingPanel;
        [SerializeField] private GameObject taskInstructionPanel;
        [SerializeField] private GameObject audioSpectrumPanel;
        [SerializeField] private GameObject pauseMenuPanel;

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.3f;

        private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Register panels
            RegisterPanel("Onboarding", onboardingPanel);
            RegisterPanel("TaskInstruction", taskInstructionPanel);
            RegisterPanel("AudioSpectrum", audioSpectrumPanel);
            RegisterPanel("PauseMenu", pauseMenuPanel);

            // Hide all panels at start
            HideAll();
        }

        public void RegisterPanel(string id, GameObject panel)
        {
            if (panel != null && !panels.ContainsKey(id))
                panels[id] = panel;
        }

        public void ShowPanel(string id)
        {
            if (panels.TryGetValue(id, out var panel) && panel != null)
            {
                panel.SetActive(true);
                var cg = panel.GetComponent<CanvasGroup>();
                if (cg != null)
                    StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, fadeDuration));

                if (ScenarioManager.Instance != null)
                    ScenarioManager.Instance.FireEvent(ScenarioEventType.UIShown, id);
            }
        }

        public void HidePanel(string id)
        {
            if (panels.TryGetValue(id, out var panel) && panel != null)
            {
                var cg = panel.GetComponent<CanvasGroup>();
                if (cg != null)
                    StartCoroutine(FadeAndDisable(cg, panel, fadeDuration));
                else
                    panel.SetActive(false);

                if (ScenarioManager.Instance != null)
                    ScenarioManager.Instance.FireEvent(ScenarioEventType.UIHidden, id);
            }
        }

        public void HideAll()
        {
            foreach (var kvp in panels)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(false);
            }
        }

        public bool IsPanelVisible(string id)
        {
            return panels.TryGetValue(id, out var panel) && panel != null && panel.activeSelf;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            cg.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        private IEnumerator FadeAndDisable(CanvasGroup cg, GameObject panel, float duration)
        {
            yield return FadeCanvasGroup(cg, 1f, 0f, duration);
            panel.SetActive(false);
        }
    }
}

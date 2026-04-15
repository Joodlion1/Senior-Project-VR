using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace VRDiagnostics
{
    /// <summary>
    /// Full-screen fade overlay for VR transitions (fade to black / fade in).
    /// Attach to a Canvas with a full-screen Image.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private Canvas fadeCanvas;

        [Header("Settings")]
        [SerializeField] private float defaultFadeDuration = 1f;
        [SerializeField] private Color fadeColor = Color.black;

        public bool IsFading { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-setup if references not assigned
            if (fadeCanvas == null)
                fadeCanvas = GetComponent<Canvas>();

            if (fadeImage != null)
            {
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
                fadeImage.raycastTarget = false;
            }
        }

        /// <summary>
        /// Fade the screen to black.
        /// </summary>
        public void FadeOut(float duration = -1f, Action onComplete = null)
        {
            if (duration < 0f) duration = defaultFadeDuration;
            StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));
        }

        /// <summary>
        /// Fade the screen back in from black.
        /// </summary>
        public void FadeIn(float duration = -1f, Action onComplete = null)
        {
            if (duration < 0f) duration = defaultFadeDuration;
            StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));
        }

        /// <summary>
        /// Fade out, execute an action while screen is black, then fade back in.
        /// </summary>
        public void FadeOutAndIn(float fadeDuration, float holdDuration, Action whileBlack = null, Action onComplete = null)
        {
            StartCoroutine(FadeOutAndInRoutine(fadeDuration, holdDuration, whileBlack, onComplete));
        }

        private IEnumerator FadeOutAndInRoutine(float fadeDuration, float holdDuration, Action whileBlack, Action onComplete)
        {
            // Fade out
            bool fadeOutDone = false;
            FadeOut(fadeDuration, () => fadeOutDone = true);
            yield return new WaitUntil(() => fadeOutDone);

            // Execute action while screen is black
            whileBlack?.Invoke();

            // Hold black screen
            yield return new WaitForSeconds(holdDuration);

            // Fade in
            bool fadeInDone = false;
            FadeIn(fadeDuration, () => fadeInDone = true);
            yield return new WaitUntil(() => fadeInDone);

            onComplete?.Invoke();
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration, Action onComplete)
        {
            if (fadeImage == null)
            {
                Debug.LogWarning("[ScreenFader] No fade image assigned.");
                onComplete?.Invoke();
                yield break;
            }

            IsFading = true;

            if (ScenarioManager.Instance != null)
            {
                string direction = toAlpha > fromAlpha ? "FadeOut" : "FadeIn";
                ScenarioManager.Instance.FireEvent(ScenarioEventType.FadeStarted, direction);
            }

            // Ensure canvas is active
            if (fadeCanvas != null)
                fadeCanvas.enabled = true;

            float elapsed = 0f;
            Color c = fadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                fadeImage.color = c;
                yield return null;
            }

            c.a = toAlpha;
            fadeImage.color = c;

            // Disable canvas when fully transparent
            if (toAlpha <= 0f && fadeCanvas != null)
                fadeCanvas.enabled = false;

            IsFading = false;

            if (ScenarioManager.Instance != null)
            {
                string direction = toAlpha > fromAlpha ? "FadeOut" : "FadeIn";
                ScenarioManager.Instance.FireEvent(ScenarioEventType.FadeCompleted, direction);
            }

            onComplete?.Invoke();
        }
    }
}

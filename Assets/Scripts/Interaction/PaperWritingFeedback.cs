using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VRDiagnostics
{
    /// <summary>
    /// Visual feedback for writing on paper during Task 2.
    /// Reveals pre-written text lines one by one to simulate the user writing.
    /// No actual handwriting recognition — just visual feedback.
    /// </summary>
    public class PaperWritingFeedback : MonoBehaviour
    {
        [Header("Writing Lines")]
        [Tooltip("Text objects that appear one by one to simulate writing")]
        [SerializeField] private GameObject[] writingLines;

        [Header("Timing")]
        [SerializeField] private float lineRevealInterval = 2f;
        [SerializeField] private float fadeInDuration = 0.3f;

        [Header("Visual")]
        [SerializeField] private Color inkColor = new Color(0.1f, 0.1f, 0.4f);

        public bool IsWriting { get; private set; }
        public int LinesWritten { get; private set; }

        private Coroutine writingRoutine;

        private void Awake()
        {
            // Hide all writing lines at start
            if (writingLines != null)
            {
                foreach (var line in writingLines)
                {
                    if (line != null)
                        line.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Start revealing writing lines.
        /// </summary>
        public void StartWriting()
        {
            if (IsWriting) return;
            IsWriting = true;

            if (writingRoutine != null)
                StopCoroutine(writingRoutine);

            writingRoutine = StartCoroutine(WritingSequence());

            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.FireEvent(ScenarioEventType.UserGazeEvent, "Writing started");

            Debug.Log("[PaperWritingFeedback] Writing started.");
        }

        /// <summary>
        /// Stop the writing sequence.
        /// </summary>
        public void StopWriting()
        {
            IsWriting = false;

            if (writingRoutine != null)
            {
                StopCoroutine(writingRoutine);
                writingRoutine = null;
            }

            Debug.Log($"[PaperWritingFeedback] Writing stopped. Lines written: {LinesWritten}");
        }

        /// <summary>
        /// Reveal all lines instantly (skip animation).
        /// </summary>
        public void RevealAllLines()
        {
            if (writingLines == null) return;

            foreach (var line in writingLines)
            {
                if (line != null)
                    line.SetActive(true);
            }

            LinesWritten = writingLines.Length;
        }

        private IEnumerator WritingSequence()
        {
            if (writingLines == null) yield break;

            for (int i = LinesWritten; i < writingLines.Length; i++)
            {
                if (!IsWriting) yield break;

                var line = writingLines[i];
                if (line == null) continue;

                // Reveal the line
                line.SetActive(true);

                // Fade in if it has a CanvasGroup
                var cg = line.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0f;
                    float elapsed = 0f;
                    while (elapsed < fadeInDuration)
                    {
                        elapsed += Time.deltaTime;
                        cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                        yield return null;
                    }
                    cg.alpha = 1f;
                }

                LinesWritten = i + 1;

                // Wait before next line
                yield return new WaitForSeconds(lineRevealInterval);
            }

            // All lines written
            IsWriting = false;
            Debug.Log("[PaperWritingFeedback] All lines revealed.");
        }
    }
}

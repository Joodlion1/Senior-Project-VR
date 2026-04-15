using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VRDiagnostics
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Subtitle UI (Optional)")]
        [SerializeField] private GameObject subtitleCanvas;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text speakerNameText;

        [Header("Settings")]
        [SerializeField] private float subtitleDisplayPadding = 0.5f;
        [SerializeField] private float estimatedSecondsPerChar = 0.07f;

        public bool IsPlaying { get; private set; }
        public event Action<DialogueLine> OnDialogueStarted;
        public event Action<DialogueLine> OnDialogueFinished;
        public event Action OnQueueCompleted;

        private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
        private Coroutine playbackRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (subtitleCanvas != null)
                subtitleCanvas.SetActive(false);
        }

        /// <summary>
        /// Play a single dialogue line on a specific AudioSource.
        /// </summary>
        public void PlayLine(DialogueLine line, AudioSource source, Action onComplete = null)
        {
            StartCoroutine(PlayLineRoutine(line, source, onComplete));
        }

        /// <summary>
        /// Queue multiple lines and play them in sequence on a specific AudioSource.
        /// </summary>
        public void PlaySequence(List<DialogueLine> lines, AudioSource source, Action onAllComplete = null)
        {
            if (playbackRoutine != null)
                StopCoroutine(playbackRoutine);

            playbackRoutine = StartCoroutine(PlaySequenceRoutine(lines, source, onAllComplete));
        }

        private IEnumerator PlayLineRoutine(DialogueLine line, AudioSource source, Action onComplete)
        {
            IsPlaying = true;
            OnDialogueStarted?.Invoke(line);

            // Show subtitle
            ShowSubtitle(line.speakerName, line.subtitleText);

            // Log event
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.FireEvent(ScenarioEventType.DialoguePlayed,
                    $"{line.speakerName}: {line.subtitleText}");

            // Calculate duration
            float duration = GetLineDuration(line);

            // Play audio
            if (line.audioClip != null && source != null)
            {
                source.clip = line.audioClip;
                source.Play();
            }

            // Wait for duration
            yield return new WaitForSeconds(duration);

            // Hide subtitle
            HideSubtitle();

            IsPlaying = false;
            OnDialogueFinished?.Invoke(line);
            onComplete?.Invoke();
        }

        private IEnumerator PlaySequenceRoutine(List<DialogueLine> lines, AudioSource source, Action onAllComplete)
        {
            foreach (var line in lines)
            {
                bool lineComplete = false;
                yield return PlayLineRoutine(line, source, () => lineComplete = true);

                // Small gap between lines
                yield return new WaitForSeconds(subtitleDisplayPadding);
            }

            OnQueueCompleted?.Invoke();
            onAllComplete?.Invoke();
        }

        private float GetLineDuration(DialogueLine line)
        {
            if (line.duration > 0f)
                return line.duration;

            if (line.audioClip != null)
                return line.audioClip.length;

            // Estimate from text length
            float estimated = Mathf.Max(2f, line.subtitleText.Length * estimatedSecondsPerChar);
            Debug.LogWarning($"[DialogueManager] No audio/duration for \"{line.subtitleText}\" — using {estimated:F1}s estimate");
            return estimated;
        }

        private void ShowSubtitle(string speaker, string text)
        {
            if (subtitleCanvas == null) return;

            subtitleCanvas.SetActive(true);

            if (speakerNameText != null)
                speakerNameText.text = speaker;

            if (subtitleText != null)
                subtitleText.text = text;
        }

        private void HideSubtitle()
        {
            if (subtitleCanvas != null)
                subtitleCanvas.SetActive(false);
        }
    }
}

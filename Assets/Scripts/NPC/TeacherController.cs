using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VRDiagnostics
{
    public class TeacherController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Dialogue Clips")]
        [Tooltip("'Please welcome our new student. Would you like to stand up and introduce yourself?'")]
        [SerializeField] private AudioClip dialogueIntroduceStudent;

        [Tooltip("'Alright... thank you.'")]
        [SerializeField] private AudioClip dialogueAlrightThankYou;

        [Tooltip("'Nice to meet you, thank you for sharing. Please take your seat.'")]
        [SerializeField] private AudioClip dialogueNiceToMeetYou;

        [Tooltip("'Discuss the effects of social media on people and summarize your discussion. I'll ask someone from each group to present.'")]
        [SerializeField] private AudioClip dialogueGroupExercise;

        [Tooltip("'Each group will now present one key point from your discussion.'")]
        [SerializeField] private AudioClip dialoguePresentKeyPoint;

        [Tooltip("'Thank you, that was clear.'")]
        [SerializeField] private AudioClip dialogueThankYouClear;

        [Tooltip("'Take your time.'")]
        [SerializeField] private AudioClip dialogueTakeYourTime;

        [Header("Timing")]
        [SerializeField] private float pauseBeforeResponse = 2f;
        [SerializeField] private float unsuccessfulPauseDuration = 4f;

        [Header("Animation")]
        [SerializeField] private string pointTrigger = "Point";
        [SerializeField] private string talkTrigger = "Talk";
        [SerializeField] private string idleTrigger = "Idle";

        public event Action OnDialogueStarted;
        public event Action OnDialogueFinished;
        public event Action<string> OnDialogueLine;

        private Animator animator;
        private NPCController npcController;
        private NPCLookAt lookAt;
        private bool isSpeaking;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            npcController = GetComponent<NPCController>();
            lookAt = GetComponent<NPCLookAt>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 1f; // 3D audio
            audioSource.playOnAwake = false;
        }

        // ----- TASK 1: Introduce Yourself -----

        /// <summary>
        /// Teacher introduces the new student and asks them to stand up.
        /// </summary>
        public void PlayTask1Introduction(Action onComplete = null)
        {
            StartCoroutine(Task1IntroSequence(onComplete));
        }

        private IEnumerator Task1IntroSequence(Action onComplete)
        {
            // Teacher looks at class, then at user
            if (lookAt != null) lookAt.SetLookAtActive(true);
            TriggerAnim(talkTrigger);

            // Play dialogue
            yield return PlayDialogue(dialogueIntroduceStudent, "Please welcome our new student. Would you like to stand up and introduce yourself?");

            // Point at user
            TriggerAnim(pointTrigger);
            yield return new WaitForSeconds(1f);

            TriggerAnim(idleTrigger);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Teacher responds after Task 1 based on success/failure.
        /// </summary>
        public void PlayTask1Response(ResponseResult result, Action onComplete = null)
        {
            StartCoroutine(Task1ResponseSequence(result, onComplete));
        }

        private IEnumerator Task1ResponseSequence(ResponseResult result, Action onComplete)
        {
            if (result == ResponseResult.Successful)
            {
                // Small pause before responding
                yield return new WaitForSeconds(pauseBeforeResponse);

                TriggerAnim(talkTrigger);
                yield return PlayDialogue(dialogueNiceToMeetYou, "Nice to meet you, thank you for sharing. Please take your seat.");
            }
            else
            {
                // Longer awkward pause
                yield return new WaitForSeconds(unsuccessfulPauseDuration);

                TriggerAnim(talkTrigger);
                yield return PlayDialogue(dialogueAlrightThankYou, "Alright... thank you.");
            }

            TriggerAnim(idleTrigger);
            onComplete?.Invoke();
        }

        // ----- TASK 2: Group Work Transition -----

        /// <summary>
        /// Teacher explains the group exercise.
        /// </summary>
        public void PlayTask2Transition(Action onComplete = null)
        {
            StartCoroutine(Task2TransitionSequence(onComplete));
        }

        private IEnumerator Task2TransitionSequence(Action onComplete)
        {
            if (lookAt != null) lookAt.SetLookAtActive(false); // look at whole class

            TriggerAnim(talkTrigger);
            yield return PlayDialogue(dialogueGroupExercise, "Discuss the effects of social media on people and summarize your discussion. I'll ask someone from each group to present.");

            yield return new WaitForSeconds(1f);
            TriggerAnim(idleTrigger);
            onComplete?.Invoke();
        }

        // ----- TASK 3: Presentation -----

        /// <summary>
        /// Teacher announces presentation time and tells user to present.
        /// </summary>
        public void PlayTask3Introduction(Action onComplete = null)
        {
            StartCoroutine(Task3IntroSequence(onComplete));
        }

        private IEnumerator Task3IntroSequence(Action onComplete)
        {
            if (lookAt != null) lookAt.SetLookAtActive(false);

            TriggerAnim(talkTrigger);
            yield return PlayDialogue(dialoguePresentKeyPoint, "Each group will now present one key point from your discussion.");

            // Point at user
            if (lookAt != null) lookAt.SetLookAtActive(true);
            TriggerAnim(pointTrigger);
            yield return new WaitForSeconds(1.5f);

            TriggerAnim(idleTrigger);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Teacher responds after Task 3 based on success/failure.
        /// </summary>
        public void PlayTask3Response(ResponseResult result, Action onComplete = null)
        {
            StartCoroutine(Task3ResponseSequence(result, onComplete));
        }

        private IEnumerator Task3ResponseSequence(ResponseResult result, Action onComplete)
        {
            if (result == ResponseResult.Successful)
            {
                yield return new WaitForSeconds(pauseBeforeResponse);

                TriggerAnim(talkTrigger);
                yield return PlayDialogue(dialogueThankYouClear, "Thank you, that was clear.");

                // Nod
                if (npcController != null)
                    npcController.SetBehavior(NPCBehavior.Nod);
            }
            else
            {
                // Teacher waits silently
                yield return new WaitForSeconds(unsuccessfulPauseDuration);

                TriggerAnim(talkTrigger);
                yield return PlayDialogue(dialogueTakeYourTime, "Take your time.");
            }

            TriggerAnim(idleTrigger);
            onComplete?.Invoke();
        }

        // ----- Utility -----

        private IEnumerator PlayDialogue(AudioClip clip, string text)
        {
            isSpeaking = true;
            OnDialogueStarted?.Invoke();
            OnDialogueLine?.Invoke(text);

            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.FireEvent(ScenarioEventType.DialoguePlayed, $"Teacher: {text}");

            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
            else
            {
                // No audio clip — estimate duration from text length (roughly 0.07s per character)
                float estimatedDuration = Mathf.Max(2f, text.Length * 0.07f);
                Debug.LogWarning($"[TeacherController] No audio clip for: \"{text}\" — using {estimatedDuration:F1}s estimated duration");
                yield return new WaitForSeconds(estimatedDuration);
            }

            isSpeaking = false;
            OnDialogueFinished?.Invoke();
        }

        private void TriggerAnim(string trigger)
        {
            if (animator != null && !string.IsNullOrEmpty(trigger))
                animator.SetTrigger(trigger);
        }
    }
}

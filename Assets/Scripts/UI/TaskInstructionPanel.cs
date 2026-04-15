using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VRDiagnostics
{
    public class TaskInstructionPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text talkingPointsText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float talkingPointsDisplayTime = 10f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Task 1 Content")]
        [SerializeField] private string task1Title = "Task 1: Introduce Yourself";
        [SerializeField] private string task1Description = "Stand up and introduce yourself to the class.";
        [SerializeField] [TextArea] private string task1TalkingPoints = "- Your name\n- Where you're from\n- Your interests or hobbies";

        [Header("Task 2 Content")]
        [SerializeField] private string task2Title = "Task 2: Group Discussion";
        [SerializeField] private string task2Description = "Discuss the effects of social media and write a summary.";
        [SerializeField] [TextArea] private string task2TalkingPoints = "- How social media affects daily life\n- Positive vs negative impacts\n- Key points to summarize";

        [Header("Task 3 Content")]
        [SerializeField] private string task3Title = "Task 3: Give a Presentation";
        [SerializeField] private string task3Description = "Present one key point from your group's discussion.";
        [SerializeField] [TextArea] private string task3TalkingPoints = "- Summarize your main point\n- Speak clearly and address the class\n- Keep it brief and focused";

        private Coroutine activeRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void ShowTask(int taskNumber)
        {
            string title, description, points;

            switch (taskNumber)
            {
                case 1:
                    title = task1Title;
                    description = task1Description;
                    points = task1TalkingPoints;
                    break;
                case 2:
                    title = task2Title;
                    description = task2Description;
                    points = task2TalkingPoints;
                    break;
                case 3:
                    title = task3Title;
                    description = task3Description;
                    points = task3TalkingPoints;
                    break;
                default:
                    return;
            }

            Show(title, description, points);
        }

        public void Show(string title, string description, string talkingPoints)
        {
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;
            if (talkingPointsText != null) talkingPointsText.text = talkingPoints;

            gameObject.SetActive(true);
            activeRoutine = StartCoroutine(ShowSequence());
        }

        private IEnumerator ShowSequence()
        {
            // Fade in
            yield return FadeCanvas(0f, 1f, fadeInDuration);

            // Show talking points for a limited time, then hide them
            if (talkingPointsText != null)
            {
                talkingPointsText.gameObject.SetActive(true);
                yield return new WaitForSeconds(talkingPointsDisplayTime);
                talkingPointsText.gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(HideSequence());
        }

        private IEnumerator HideSequence()
        {
            yield return FadeCanvas(1f, 0f, fadeOutDuration);
            gameObject.SetActive(false);
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}

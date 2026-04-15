using UnityEngine;

namespace VRDiagnostics
{
    /// <summary>
    /// Evaluates whether the user's response to a task was Successful or Unsuccessful.
    /// Based on speech detection: did the user speak, and for how long?
    /// This is for NPC reaction branching only — NOT clinical diagnosis.
    /// </summary>
    public class ResponseEvaluator : MonoBehaviour
    {
        public static ResponseEvaluator Instance { get; private set; }

        [Header("Evaluation Thresholds")]
        [Tooltip("Minimum speech duration (seconds) to count as successful")]
        [SerializeField] private float minSpeechDuration = 3f;

        [Tooltip("Maximum wait time (seconds) before marking as unsuccessful")]
        [SerializeField] private float responseTimeout = 30f;

        public float MinSpeechDuration => minSpeechDuration;
        public float ResponseTimeout => responseTimeout;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Evaluate based on the speech data collected during a task.
        /// </summary>
        public ResponseResult Evaluate(float totalSpeechDuration, bool speechDetected)
        {
            if (!speechDetected || totalSpeechDuration < minSpeechDuration)
            {
                Debug.Log($"[ResponseEvaluator] Unsuccessful — speechDetected={speechDetected}, duration={totalSpeechDuration:F1}s (min={minSpeechDuration}s)");
                return ResponseResult.Unsuccessful;
            }

            Debug.Log($"[ResponseEvaluator] Successful — duration={totalSpeechDuration:F1}s");
            return ResponseResult.Successful;
        }
    }
}

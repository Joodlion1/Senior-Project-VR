using UnityEngine;
using System.IO;

namespace VRDiagnostics
{
    public class DataExporter : MonoBehaviour
    {
        public static DataExporter Instance { get; private set; }

        [Header("Export Settings")]
        [SerializeField] private bool prettyPrint = true;

        public string LastExportPath { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionEnded += OnSessionEnded;
            }
        }

        private void Start()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionEnded -= OnSessionEnded;
                SessionManager.Instance.OnSessionEnded += OnSessionEnded;
            }
        }

        private void OnDisable()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionEnded -= OnSessionEnded;
            }
        }

        private void OnSessionEnded()
        {
            ExportSession();
        }

        /// <summary>
        /// Export the current session data to a local JSON file.
        /// </summary>
        public void ExportSession()
        {
            var session = SessionManager.Instance?.CurrentSession;
            if (session == null)
            {
                Debug.LogWarning("[DataExporter] No session data to export.");
                return;
            }

            // Collect data from all loggers
            CollectAllData(session);

            // Create sessions directory
            string sessionsDir = Path.Combine(Application.persistentDataPath, "sessions");
            if (!Directory.Exists(sessionsDir))
            {
                Directory.CreateDirectory(sessionsDir);
            }

            // Generate filename
            string timestamp = System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string filename = $"session_{session.sessionId.Substring(0, 8)}_{timestamp}.json";
            string filePath = Path.Combine(sessionsDir, filename);

            // Serialize to JSON
            string json = JsonUtility.ToJson(session, prettyPrint);

            // Write to file
            File.WriteAllText(filePath, json);

            LastExportPath = filePath;
            Debug.Log($"[DataExporter] Session exported to: {filePath}");
            Debug.Log($"[DataExporter] Data summary: " +
                      $"{session.scenarioEvents.Count} events, " +
                      $"{session.gazeData.Count} gaze points, " +
                      $"{session.heartRateData.Count} HR points, " +
                      $"{session.speechData.Count} speech records, " +
                      $"{session.taskResults.Count} task results");
        }

        private void CollectAllData(SessionData session)
        {
            // Collect scenario events
            if (EventLogger.Instance != null)
            {
                EventLogger.Instance.FlushToSession(session);
            }

            // Collect gaze data
            var eyeTracker = FindAnyObjectByType<EyeTrackingManager>();
            if (eyeTracker != null)
            {
                session.gazeData = eyeTracker.GetGazeLog();
            }

            // Collect speech data
            if (SpeechDetector.Instance != null)
            {
                SpeechDetector.Instance.FlushToSession(session);
            }

            // Collect heart rate data
            if (HeartRateCapture.Instance != null)
            {
                HeartRateCapture.Instance.FlushToSession(session);
            }
        }
    }
}

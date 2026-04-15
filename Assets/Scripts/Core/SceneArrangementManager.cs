using UnityEngine;

namespace VRDiagnostics
{
    /// <summary>
    /// Toggles between classroom layout (Tasks 1 & 3) and round table layout (Task 2).
    /// Enable/disable GameObjects and move the XR Rig to the correct position.
    /// </summary>
    public class SceneArrangementManager : MonoBehaviour
    {
        public static SceneArrangementManager Instance { get; private set; }

        [Header("Arrangement A — Classroom (Tasks 1 & 3)")]
        [Tooltip("Parent object containing all classroom-layout objects (rows of desks, etc.)")]
        [SerializeField] private GameObject classroomArrangement;
        [Tooltip("Where the XR Rig sits in classroom layout")]
        [SerializeField] private Transform classroomSeatPosition;

        [Header("Arrangement B — Round Table (Task 2)")]
        [Tooltip("Parent object containing round table setup")]
        [SerializeField] private GameObject roundTableArrangement;
        [Tooltip("Where the XR Rig sits at the round table")]
        [SerializeField] private Transform roundTableSeatPosition;

        [Header("XR Rig")]
        [SerializeField] private Transform xrRig;

        public bool IsClassroomActive { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Start in classroom arrangement — enable/disable objects only
            // Do NOT move XR Rig on start — let it use its scene position
            if (classroomArrangement != null)
                classroomArrangement.SetActive(true);
            if (roundTableArrangement != null)
                roundTableArrangement.SetActive(false);
            IsClassroomActive = true;
            Debug.Log("[SceneArrangementManager] Classroom arrangement activated (no XR Rig move on start).");
        }

        /// <summary>
        /// Switch to the classroom layout (Tasks 1 and 3).
        /// </summary>
        public void SwitchToClassroom()
        {
            IsClassroomActive = true;

            if (classroomArrangement != null)
                classroomArrangement.SetActive(true);

            if (roundTableArrangement != null)
                roundTableArrangement.SetActive(false);

            MoveXRRig(classroomSeatPosition);

            Debug.Log("[SceneArrangementManager] Switched to Classroom layout.");
        }

        /// <summary>
        /// Switch to the round table layout (Task 2).
        /// </summary>
        public void SwitchToRoundTable()
        {
            IsClassroomActive = false;

            if (classroomArrangement != null)
                classroomArrangement.SetActive(false);

            if (roundTableArrangement != null)
                roundTableArrangement.SetActive(true);

            MoveXRRig(roundTableSeatPosition);

            Debug.Log("[SceneArrangementManager] Switched to Round Table layout.");
        }

        private void MoveXRRig(Transform targetPosition)
        {
            if (xrRig == null || targetPosition == null) return;

            xrRig.position = targetPosition.position;
            xrRig.rotation = targetPosition.rotation;
        }
    }
}

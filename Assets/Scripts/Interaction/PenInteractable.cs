using UnityEngine;

namespace VRDiagnostics
{
    /// <summary>
    /// Simple pen interaction for Task 2.
    /// When the user grabs the pen (via XR Grab Interactable) and touches the paper,
    /// writing feedback is triggered.
    ///
    /// Simplified approach: The pen can also be activated by pressing the trigger
    /// button near the paper, without needing precise grab mechanics.
    /// </summary>
    public class PenInteractable : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PaperWritingFeedback paperFeedback;

        [Header("Settings")]
        [Tooltip("Is the pen currently held by the user?")]
        [SerializeField] private bool isHeld;

        public bool IsHeld => isHeld;
        public bool IsWriting { get; private set; }

        /// <summary>
        /// Called by XR Grab Interactable's Select Entered event (or manually).
        /// </summary>
        public void OnGrabbed()
        {
            isHeld = true;
            Debug.Log("[PenInteractable] Pen grabbed.");

            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.FireEvent(ScenarioEventType.UserGazeEvent, "Pen grabbed");
        }

        /// <summary>
        /// Called by XR Grab Interactable's Select Exited event (or manually).
        /// </summary>
        public void OnReleased()
        {
            isHeld = false;
            IsWriting = false;
            Debug.Log("[PenInteractable] Pen released.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isHeld) return;

            // Check if we're touching the paper
            if (other.CompareTag("Paper") || other.GetComponent<PaperWritingFeedback>() != null)
            {
                IsWriting = true;
                var paper = other.GetComponent<PaperWritingFeedback>();
                if (paper != null)
                    paper.StartWriting();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Paper") || other.GetComponent<PaperWritingFeedback>() != null)
            {
                IsWriting = false;
                var paper = other.GetComponent<PaperWritingFeedback>();
                if (paper != null)
                    paper.StopWriting();
            }
        }

        /// <summary>
        /// Simplified writing: call this to auto-grab and start writing
        /// (for keyboard/controller button testing without grab mechanics).
        /// </summary>
        public void SimulateWriting()
        {
            isHeld = true;
            IsWriting = true;
            if (paperFeedback != null)
                paperFeedback.StartWriting();
        }
    }
}

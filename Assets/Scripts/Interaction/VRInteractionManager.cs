using UnityEngine;

namespace VRDiagnostics
{
    /// <summary>
    /// Manages VR controller interaction modes based on task context.
    /// Switches between UI ray interaction (menus/buttons) and direct interaction (pen grab).
    /// </summary>
    public class VRInteractionManager : MonoBehaviour
    {
        public static VRInteractionManager Instance { get; private set; }

        [Header("Interactors")]
        [Tooltip("Ray Interactor on the right controller (for UI interaction)")]
        [SerializeField] private GameObject rayInteractor;
        [Tooltip("Direct Interactor on the right controller (for grabbing objects)")]
        [SerializeField] private GameObject directInteractor;

        [Header("Controller References")]
        [SerializeField] private GameObject leftController;
        [SerializeField] private GameObject rightController;

        public enum InteractionMode
        {
            UIOnly,     // Ray interactor for UI buttons
            Direct,     // Direct grab for pen/objects
            Both        // Both active
        }

        public InteractionMode CurrentMode { get; private set; }

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
            // Default to UI mode
            SetMode(InteractionMode.UIOnly);

            // Listen for state changes to auto-switch modes
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.OnStateChanged.AddListener(OnStateChanged);
        }

        private void OnDestroy()
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.OnStateChanged.RemoveListener(OnStateChanged);
        }

        private void OnStateChanged(ScenarioState oldState, ScenarioState newState)
        {
            switch (newState)
            {
                case ScenarioState.Task2_GroupWork:
                    // Enable direct interaction for pen/writing
                    SetMode(InteractionMode.Both);
                    break;
                default:
                    // UI only for everything else
                    SetMode(InteractionMode.UIOnly);
                    break;
            }
        }

        public void SetMode(InteractionMode mode)
        {
            CurrentMode = mode;

            switch (mode)
            {
                case InteractionMode.UIOnly:
                    SetActive(rayInteractor, true);
                    SetActive(directInteractor, false);
                    break;
                case InteractionMode.Direct:
                    SetActive(rayInteractor, false);
                    SetActive(directInteractor, true);
                    break;
                case InteractionMode.Both:
                    SetActive(rayInteractor, true);
                    SetActive(directInteractor, true);
                    break;
            }

            Debug.Log($"[VRInteractionManager] Mode: {mode}");
        }

        private void SetActive(GameObject obj, bool active)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}

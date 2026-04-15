using UnityEngine;

namespace VRDiagnostics
{
    [RequireComponent(typeof(Animator))]
    public class NPCLookAt : MonoBehaviour
    {
        [Header("Look-At Configuration")]
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private bool autoFindCamera = true;

        [Header("Weights")]
        [SerializeField] [Range(0f, 1f)] private float bodyWeight = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float headWeight = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float eyesWeight = 1f;
        [SerializeField] [Range(0f, 1f)] private float clampWeight = 0.5f;

        [Header("Smoothing")]
        [SerializeField] private float transitionSpeed = 3f;
        [SerializeField] private float maxHorizontalAngle = 70f;
        [SerializeField] private float maxVerticalAngle = 40f;

        private Animator animator;
        private bool isActive;
        private float currentWeight;
        private float targetWeight;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (autoFindCamera && lookAtTarget == null)
            {
                var cam = Camera.main;
                if (cam != null)
                    lookAtTarget = cam.transform;
            }
        }

        public void SetLookAtActive(bool active)
        {
            isActive = active;
            targetWeight = active ? 1f : 0f;
        }

        public void SetLookAtTarget(Transform target)
        {
            lookAtTarget = target;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || lookAtTarget == null)
                return;

            // Smooth weight transition
            currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * transitionSpeed);

            // Check if target is within valid angle range
            if (currentWeight > 0.01f)
            {
                Vector3 directionToTarget = lookAtTarget.position - transform.position;
                Vector3 localDirection = transform.InverseTransformDirection(directionToTarget.normalized);

                float horizontalAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
                float verticalAngle = Mathf.Asin(localDirection.y) * Mathf.Rad2Deg;

                // Reduce weight if target is outside comfortable range
                float angleFactor = 1f;
                if (Mathf.Abs(horizontalAngle) > maxHorizontalAngle || Mathf.Abs(verticalAngle) > maxVerticalAngle)
                {
                    angleFactor = 0.3f; // Still look slightly but not fully
                }

                float finalWeight = currentWeight * angleFactor;

                animator.SetLookAtPosition(lookAtTarget.position);
                animator.SetLookAtWeight(
                    finalWeight,
                    bodyWeight * finalWeight,
                    headWeight * finalWeight,
                    eyesWeight * finalWeight,
                    clampWeight
                );
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }
        }
    }
}

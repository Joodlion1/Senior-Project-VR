using UnityEngine;
using System;
using System.Collections;

namespace VRDiagnostics
{
    [RequireComponent(typeof(Animator))]
    public class NPCController : MonoBehaviour
    {
        [Header("NPC Identity")]
        [SerializeField] private string npcName;
        [SerializeField] private bool isSeated = true;

        [Header("Look-At Settings")]
        [SerializeField] private float lookAtSpeed = 2f;
        [SerializeField] private float headWeight = 0.7f;
        [SerializeField] private float bodyWeight = 0.2f;
        [SerializeField] private float eyesWeight = 1f;

        [Header("Animation Triggers")]
        [SerializeField] private string whisperTrigger = "Whisper";
        [SerializeField] private string nodTrigger = "Nod";
        [SerializeField] private string smileTrigger = "Smile";
        [SerializeField] private string turnAwayTrigger = "TurnAway";
        [SerializeField] private string talkTrigger = "Talk";
        [SerializeField] private string lookAtUserBool = "LookAtUser";

        public string NPCName => string.IsNullOrEmpty(npcName) ? gameObject.name : npcName;
        public NPCBehavior CurrentBehavior { get; private set; } = NPCBehavior.Idle;
        public bool IsLookingAtUser { get; private set; }

        private Animator animator;
        private Transform lookAtTarget;
        private float currentLookWeight;
        private float targetLookWeight;
        private NPCLookAt lookAtComponent;

        // Face expression
        private NPCExpressionController expressionController;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            lookAtComponent = GetComponent<NPCLookAt>();
            expressionController = GetComponent<NPCExpressionController>();
        }

        private void Start()
        {
            // Default: find the main camera as look-at target
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                lookAtTarget = mainCam.transform;
            }
        }

        /// <summary>
        /// Set the behavior of this NPC. Triggers animations and look-at changes.
        /// </summary>
        public void SetBehavior(NPCBehavior behavior)
        {
            CurrentBehavior = behavior;

            switch (behavior)
            {
                case NPCBehavior.Idle:
                    SetLookAtUser(false);
                    break;

                case NPCBehavior.LookAtUser:
                    SetLookAtUser(true);
                    break;

                case NPCBehavior.LookAway:
                    SetLookAtUser(false);
                    TriggerAnimation(turnAwayTrigger);
                    break;

                case NPCBehavior.Whisper:
                    SetLookAtUser(false);
                    TriggerAnimation(whisperTrigger);
                    break;

                case NPCBehavior.Nod:
                    TriggerAnimation(nodTrigger);
                    break;

                case NPCBehavior.Smile:
                    TriggerAnimation(smileTrigger);
                    if (expressionController != null)
                        expressionController.SetExpression(CharacterCustomizationTool.FaceManagement.FaceType.Happy);
                    break;

                case NPCBehavior.TurnHead:
                    SetLookAtUser(false);
                    TriggerAnimation(turnAwayTrigger);
                    break;

                case NPCBehavior.Talk:
                    SetLookAtUser(true);
                    TriggerAnimation(talkTrigger);
                    break;

                case NPCBehavior.Listen:
                    SetLookAtUser(true);
                    break;

                case NPCBehavior.Discuss:
                    TriggerAnimation(talkTrigger);
                    break;
            }
        }

        /// <summary>
        /// Set behavior with a delay (for staggered group reactions).
        /// </summary>
        public void SetBehaviorDelayed(NPCBehavior behavior, float delay)
        {
            StartCoroutine(SetBehaviorAfterDelay(behavior, delay));
        }

        private IEnumerator SetBehaviorAfterDelay(NPCBehavior behavior, float delay)
        {
            yield return new WaitForSeconds(delay);
            SetBehavior(behavior);
        }

        /// <summary>
        /// Reset NPC to idle state with neutral expression.
        /// </summary>
        public void ResetToIdle()
        {
            SetBehavior(NPCBehavior.Idle);
            if (expressionController != null)
                expressionController.SetExpression(CharacterCustomizationTool.FaceManagement.FaceType.Neutral);
        }

        private void SetLookAtUser(bool lookAt)
        {
            IsLookingAtUser = lookAt;
            targetLookWeight = lookAt ? 1f : 0f;

            if (animator != null)
                animator.SetBool(lookAtUserBool, lookAt);

            if (lookAtComponent != null)
                lookAtComponent.SetLookAtActive(lookAt);
        }

        private void TriggerAnimation(string triggerName)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }

        /// <summary>
        /// Built-in IK callback for head look-at when not using NPCLookAt component.
        /// </summary>
        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || lookAtTarget == null || lookAtComponent != null)
                return; // Skip if NPCLookAt handles this

            // Smoothly interpolate look weight
            currentLookWeight = Mathf.Lerp(currentLookWeight, targetLookWeight, Time.deltaTime * lookAtSpeed);

            animator.SetLookAtPosition(lookAtTarget.position);
            animator.SetLookAtWeight(currentLookWeight, bodyWeight, headWeight, eyesWeight);
        }

        public void SetLookAtTarget(Transform target)
        {
            lookAtTarget = target;
        }
    }
}

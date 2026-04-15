using UnityEngine;
using CharacterCustomizationTool.FaceManagement;

namespace VRDiagnostics
{
    /// <summary>
    /// Controls NPC facial expressions using the existing FacePicker system.
    /// Attach alongside FacePicker on NPC characters that support face swapping.
    /// </summary>
    public class NPCExpressionController : MonoBehaviour
    {
        [Header("Default Expression")]
        [SerializeField] private FaceType defaultExpression = FaceType.Neutral;

        [Header("Expression Timing")]
        [SerializeField] private float expressionDuration = 3f;
        [SerializeField] private bool autoRevertToDefault = true;

        private FacePicker facePicker;
        private float expressionTimer;
        private bool isTemporaryExpression;

        private void Awake()
        {
            facePicker = GetComponentInChildren<FacePicker>();
        }

        private void Start()
        {
            if (facePicker != null)
            {
                SetExpression(defaultExpression);
            }
        }

        private void Update()
        {
            if (isTemporaryExpression && autoRevertToDefault)
            {
                expressionTimer -= Time.deltaTime;
                if (expressionTimer <= 0f)
                {
                    SetExpression(defaultExpression);
                    isTemporaryExpression = false;
                }
            }
        }

        /// <summary>
        /// Set the NPC's facial expression permanently (until changed again).
        /// </summary>
        public void SetExpression(FaceType faceType)
        {
            if (facePicker != null)
            {
                facePicker.PickFace(faceType);
                isTemporaryExpression = false;
            }
        }

        /// <summary>
        /// Set a temporary expression that reverts to default after duration.
        /// </summary>
        public void SetTemporaryExpression(FaceType faceType, float duration = -1f)
        {
            if (facePicker != null)
            {
                facePicker.PickFace(faceType);
                isTemporaryExpression = true;
                expressionTimer = duration > 0 ? duration : expressionDuration;
            }
        }

        /// <summary>
        /// Set expression for scenario context (success vs failure).
        /// </summary>
        public void SetResponseExpression(ResponseResult result)
        {
            if (result == ResponseResult.Successful)
            {
                SetTemporaryExpression(FaceType.Happy);
            }
            else
            {
                SetExpression(FaceType.Neutral);
            }
        }
    }
}

using UnityEngine;
using System;

namespace VRDiagnostics
{
    /// <summary>
    /// Coordinates NPC reactions based on task result (Successful/Unsuccessful).
    /// Delegates to TeacherController and StudentNPCGroup.
    /// </summary>
    public class NPCReactionOrchestrator : MonoBehaviour
    {
        public static NPCReactionOrchestrator Instance { get; private set; }

        [Header("References")]
        [SerializeField] private TeacherController teacher;
        [SerializeField] private StudentNPCGroup studentGroup;

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
            if (teacher == null)
                teacher = FindAnyObjectByType<TeacherController>();
            if (studentGroup == null)
                studentGroup = FindAnyObjectByType<StudentNPCGroup>();
        }

        /// <summary>
        /// Play Task 1 reactions: teacher response + student reactions.
        /// </summary>
        public void PlayTask1Reactions(ResponseResult result, Action onComplete = null)
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.FireEvent(ScenarioEventType.NPCReaction,
                    $"Task1_{result}");

            // Student reactions
            if (studentGroup != null)
            {
                if (result == ResponseResult.Successful)
                    studentGroup.PlayTask1SuccessfulReaction();
                else
                    studentGroup.PlayTask1UnsuccessfulReaction();
            }

            // Teacher response
            if (teacher != null)
                teacher.PlayTask1Response(result, onComplete);
            else
                onComplete?.Invoke();
        }

        /// <summary>
        /// Play Task 2 group discussion sequence.
        /// </summary>
        public void PlayGroupDiscussion()
        {
            if (studentGroup != null)
                studentGroup.PlayGroupDiscussion();
        }

        /// <summary>
        /// Play Task 2 positive response after user speaks.
        /// </summary>
        public void PlayGroupPositiveResponse()
        {
            if (studentGroup != null)
                studentGroup.PlayGroupPositiveResponse();
        }

        /// <summary>
        /// Play Task 2 unsuccessful response when user doesn't participate.
        /// </summary>
        public void PlayGroupUnsuccessfulResponse()
        {
            if (studentGroup != null)
                studentGroup.PlayGroupUnsuccessfulResponse();
        }

        /// <summary>
        /// Play Task 3 reactions: teacher response + student reactions.
        /// </summary>
        public void PlayTask3Reactions(ResponseResult result, Action onComplete = null)
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.FireEvent(ScenarioEventType.NPCReaction,
                    $"Task3_{result}");

            // Student reactions
            if (studentGroup != null)
            {
                if (result == ResponseResult.Successful)
                    studentGroup.PlayTask3SuccessfulReaction();
                else
                    studentGroup.PlayTask3UnsuccessfulReaction();
            }

            // Teacher response
            if (teacher != null)
                teacher.PlayTask3Response(result, onComplete);
            else
                onComplete?.Invoke();
        }

        /// <summary>
        /// Make all NPCs look at the user (for presentation).
        /// </summary>
        public void AllNPCsLookAtUser()
        {
            if (studentGroup != null)
                studentGroup.AllLookAtUser();
        }

        /// <summary>
        /// Reset all NPCs to idle.
        /// </summary>
        public void ResetAllNPCs()
        {
            if (studentGroup != null)
                studentGroup.AllIdle();
        }
    }
}

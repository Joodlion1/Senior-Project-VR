using UnityEngine;

namespace VRDiagnostics
{
    /// <summary>
    /// ScriptableObject holding all timing/pacing values for the VR scenario.
    /// Create via: Assets > Create > VR Diagnostics > Scenario Timing Config
    /// Adjust values in the Inspector without changing code.
    /// </summary>
    [CreateAssetMenu(fileName = "ScenarioTimingConfig", menuName = "VR Diagnostics/Scenario Timing Config")]
    public class ScenarioTimingConfig : ScriptableObject
    {
        [Header("Onboarding")]
        [Tooltip("How long the onboarding welcome text stays visible (seconds)")]
        public float onboardingDisplayDuration = 5f;

        [Header("Task Instructions")]
        [Tooltip("How long task instruction panels stay visible before auto-hiding (seconds)")]
        public float taskInstructionDisplayTime = 8f;

        [Tooltip("How long talking points stay visible (seconds)")]
        public float talkingPointsDisplayDuration = 10f;

        [Header("Speech Detection")]
        [Tooltip("Minimum speech duration to count as successful (seconds)")]
        public float minSpeechDuration = 3f;

        [Tooltip("How long to wait for user speech before marking unsuccessful (seconds)")]
        public float speechDetectionTimeout = 30f;

        [Header("Transitions")]
        [Tooltip("Duration of fade to black / fade in transitions (seconds)")]
        public float fadeTransitionDuration = 1.5f;

        [Tooltip("How long the screen stays black during transitions (seconds)")]
        public float holdBlackDuration = 1f;

        [Header("Pacing")]
        [Tooltip("Pause between task phases (seconds)")]
        public float pauseBetweenPhases = 2f;

        [Tooltip("Pause before teacher responds after user speech (seconds)")]
        public float teacherResponseDelay = 2f;

        [Tooltip("Longer pause for unsuccessful responses — awkward silence (seconds)")]
        public float unsuccessfulPauseDuration = 4f;

        [Header("NPC Reactions")]
        [Tooltip("Minimum stagger delay for group NPC reactions (seconds)")]
        public float npcMinStaggerDelay = 0.2f;

        [Tooltip("Maximum stagger delay for group NPC reactions (seconds)")]
        public float npcMaxStaggerDelay = 1.0f;

        [Tooltip("How long group discussion plays before asking user (seconds)")]
        public float groupDiscussionDuration = 5f;

        [Header("Session Limits")]
        [Tooltip("Maximum session duration before auto-ending (seconds). Target: 7-15 min")]
        public float maxSessionDuration = 900f;

        [Tooltip("When to warn that session is approaching max duration (seconds)")]
        public float sessionWarningTime = 840f;
    }
}

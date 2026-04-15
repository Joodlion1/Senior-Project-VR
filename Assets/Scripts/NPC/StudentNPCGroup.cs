using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VRDiagnostics
{
    public class StudentNPCGroup : MonoBehaviour
    {
        [Header("NPC References")]
        [SerializeField] private List<NPCController> frontRowNPCs = new List<NPCController>();
        [SerializeField] private List<NPCController> secondRowNPCs = new List<NPCController>();
        [SerializeField] private List<NPCController> thirdRowNPCs = new List<NPCController>();
        [SerializeField] private List<NPCController> groupTableNPCs = new List<NPCController>(); // Task 2 round table

        [Header("Stagger Settings")]
        [SerializeField] private float minStaggerDelay = 0.2f;
        [SerializeField] private float maxStaggerDelay = 1.0f;

        [Header("Audio")]
        [Tooltip("Student says: 'What's your opinion?'")]
        [SerializeField] private AudioClip clipWhatsYourOpinion;
        [Tooltip("Student says: 'That makes sense.'")]
        [SerializeField] private AudioClip clipThatMakesSense;
        [Tooltip("Whisper sound effect")]
        [SerializeField] private AudioClip clipWhisper;

        [Header("Task 2 — Group Conversation")]
        [Tooltip("Group conversation part 1")]
        [SerializeField] private AudioClip clipConvoPart1;
        [Tooltip("Group conversation part 2")]
        [SerializeField] private AudioClip clipConvoPart2;
        [Tooltip("Group conversation part 3")]
        [SerializeField] private AudioClip clipConvoPart3;
        [Tooltip("Background student chatter")]
        [SerializeField] private AudioClip clipChatterBackground;
        [Tooltip("Student reply when user doesn't participate")]
        [SerializeField] private AudioClip clipUnsuccessfulReply;

        [Header("Task 3 — Presentation")]
        [Tooltip("Quiet clapping after successful presentation")]
        [SerializeField] private AudioClip clipClapping;

        private List<NPCController> allClassroomNPCs = new List<NPCController>();

        private void Start()
        {
            // Build combined list
            allClassroomNPCs.AddRange(frontRowNPCs);
            allClassroomNPCs.AddRange(secondRowNPCs);
            allClassroomNPCs.AddRange(thirdRowNPCs);
        }

        // ===== BULK ACTIONS (with staggered timing) =====

        public void AllLookAtUser()
        {
            SetBehaviorStaggered(allClassroomNPCs, NPCBehavior.LookAtUser);
        }

        public void AllLookAway()
        {
            SetBehaviorStaggered(allClassroomNPCs, NPCBehavior.LookAway);
        }

        public void AllIdle()
        {
            foreach (var npc in allClassroomNPCs)
                npc.ResetToIdle();
            foreach (var npc in groupTableNPCs)
                npc.ResetToIdle();
        }

        // ===== TASK 1 REACTIONS =====

        /// <summary>
        /// Task 1 Successful: Some students smile and nod encouragingly.
        /// </summary>
        public void PlayTask1SuccessfulReaction()
        {
            StartCoroutine(Task1SuccessSequence());
        }

        private IEnumerator Task1SuccessSequence()
        {
            // Some students smile
            var smilers = GetRandomSubset(allClassroomNPCs, 4);
            foreach (var npc in smilers)
            {
                npc.SetBehaviorDelayed(NPCBehavior.Smile, Random.Range(minStaggerDelay, maxStaggerDelay));
            }

            yield return new WaitForSeconds(1f);

            // Some students nod
            var nodders = GetRandomSubset(allClassroomNPCs, 3);
            foreach (var npc in nodders)
            {
                npc.SetBehaviorDelayed(NPCBehavior.Nod, Random.Range(minStaggerDelay, maxStaggerDelay));
            }
        }

        /// <summary>
        /// Task 1 Unsuccessful: One whispers, students turn heads away.
        /// </summary>
        public void PlayTask1UnsuccessfulReaction()
        {
            StartCoroutine(Task1UnsuccessSequence());
        }

        private IEnumerator Task1UnsuccessSequence()
        {
            // One student whispers
            if (allClassroomNPCs.Count > 0)
            {
                var whisperer = allClassroomNPCs[Random.Range(0, allClassroomNPCs.Count)];
                whisperer.SetBehavior(NPCBehavior.Whisper);
                PlayClipAtNPC(whisperer, clipWhisper);
            }

            yield return new WaitForSeconds(1.5f);

            // Students turn their heads away
            SetBehaviorStaggered(allClassroomNPCs, NPCBehavior.TurnHead);
        }

        // ===== TASK 2 REACTIONS =====

        /// <summary>
        /// Task 2: Group table students discuss among themselves.
        /// </summary>
        public void PlayGroupDiscussion()
        {
            StartCoroutine(GroupDiscussionSequence());
        }

        private IEnumerator GroupDiscussionSequence()
        {
            // Play background chatter
            AudioSource bgSource = null;
            if (clipChatterBackground != null)
            {
                bgSource = gameObject.AddComponent<AudioSource>();
                bgSource.clip = clipChatterBackground;
                bgSource.spatialBlend = 0.5f;
                bgSource.volume = 0.3f;
                bgSource.loop = true;
                bgSource.Play();
            }

            // Students at table start discussing
            foreach (var npc in groupTableNPCs)
            {
                npc.SetBehaviorDelayed(NPCBehavior.Discuss, Random.Range(0f, 0.5f));
            }

            // Play conversation parts in sequence
            if (groupTableNPCs.Count >= 3)
            {
                // Part 1
                if (clipConvoPart1 != null)
                {
                    groupTableNPCs[0].SetBehavior(NPCBehavior.Talk);
                    PlayClipAtNPC(groupTableNPCs[0], clipConvoPart1);
                    yield return new WaitForSeconds(clipConvoPart1.length + 0.5f);
                    groupTableNPCs[0].SetBehavior(NPCBehavior.Discuss);
                }

                // Part 2
                if (clipConvoPart2 != null)
                {
                    groupTableNPCs[1].SetBehavior(NPCBehavior.Talk);
                    PlayClipAtNPC(groupTableNPCs[1], clipConvoPart2);
                    yield return new WaitForSeconds(clipConvoPart2.length + 0.5f);
                    groupTableNPCs[1].SetBehavior(NPCBehavior.Discuss);
                }

                // Part 3
                if (clipConvoPart3 != null)
                {
                    groupTableNPCs[2].SetBehavior(NPCBehavior.Talk);
                    PlayClipAtNPC(groupTableNPCs[2], clipConvoPart3);
                    yield return new WaitForSeconds(clipConvoPart3.length + 0.5f);
                    groupTableNPCs[2].SetBehavior(NPCBehavior.Discuss);
                }
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            // Stop background chatter
            if (bgSource != null)
            {
                bgSource.Stop();
                Destroy(bgSource);
            }

            // Students turn to look at user
            foreach (var npc in groupTableNPCs)
            {
                npc.SetBehaviorDelayed(NPCBehavior.LookAtUser, Random.Range(0.1f, 0.5f));
            }

            yield return new WaitForSeconds(1f);

            // One student asks "What's your opinion?"
            if (groupTableNPCs.Count > 0)
            {
                var asker = groupTableNPCs[0];
                asker.SetBehavior(NPCBehavior.Talk);
                PlayClipAtNPC(asker, clipWhatsYourOpinion);
            }
        }

        /// <summary>
        /// Task 2: Peer responds positively after user speaks.
        /// </summary>
        public void PlayGroupPositiveResponse()
        {
            if (groupTableNPCs.Count > 1)
            {
                var responder = groupTableNPCs[1];
                responder.SetBehavior(NPCBehavior.Nod);
                PlayClipAtNPC(responder, clipThatMakesSense);
            }
        }

        /// <summary>
        /// Task 2: Peer responds when user doesn't participate.
        /// </summary>
        public void PlayGroupUnsuccessfulResponse()
        {
            if (groupTableNPCs.Count > 0 && clipUnsuccessfulReply != null)
            {
                var responder = groupTableNPCs[0];
                responder.SetBehavior(NPCBehavior.Talk);
                PlayClipAtNPC(responder, clipUnsuccessfulReply);
            }
        }

        // ===== TASK 3 REACTIONS =====

        /// <summary>
        /// Task 3 Successful: Student nods, peer responds positively, some students nod.
        /// </summary>
        public void PlayTask3SuccessfulReaction()
        {
            StartCoroutine(Task3SuccessSequence());
        }

        private IEnumerator Task3SuccessSequence()
        {
            // One student nods while listening
            if (allClassroomNPCs.Count > 0)
            {
                var nodder = allClassroomNPCs[Random.Range(0, allClassroomNPCs.Count)];
                nodder.SetBehavior(NPCBehavior.Nod);
            }

            yield return new WaitForSeconds(2f);

            // A peer responds: "That makes sense"
            if (allClassroomNPCs.Count > 1)
            {
                var responder = allClassroomNPCs[Random.Range(0, allClassroomNPCs.Count)];
                responder.SetBehavior(NPCBehavior.Talk);
                PlayClipAtNPC(responder, clipThatMakesSense);
            }

            yield return new WaitForSeconds(2f);

            // Some students nod
            var nodders = GetRandomSubset(allClassroomNPCs, 4);
            foreach (var npc in nodders)
            {
                npc.SetBehaviorDelayed(NPCBehavior.Nod, Random.Range(minStaggerDelay, maxStaggerDelay));
            }

            // Play quiet clapping
            if (clipClapping != null)
            {
                AudioSource.PlayClipAtPoint(clipClapping, transform.position);
            }
        }

        /// <summary>
        /// Task 3 Unsuccessful: Pause, one looks at another, brief response.
        /// </summary>
        public void PlayTask3UnsuccessfulReaction()
        {
            StartCoroutine(Task3UnsuccessSequence());
        }

        private IEnumerator Task3UnsuccessSequence()
        {
            // Slight pause — awkward silence
            yield return new WaitForSeconds(3f);

            // One student looks at another briefly
            if (allClassroomNPCs.Count >= 2)
            {
                var looker = allClassroomNPCs[0];
                var target = allClassroomNPCs[1];
                var lookAtComp = looker.GetComponent<NPCLookAt>();
                if (lookAtComp != null)
                {
                    lookAtComp.SetLookAtTarget(target.transform);
                    lookAtComp.SetLookAtActive(true);
                }
            }

            yield return new WaitForSeconds(2f);

            // Reset the look
            if (allClassroomNPCs.Count >= 2)
            {
                var lookAtComp = allClassroomNPCs[0].GetComponent<NPCLookAt>();
                if (lookAtComp != null)
                    lookAtComp.SetLookAtActive(false);
            }
        }

        // ===== UTILITY =====

        private void SetBehaviorStaggered(List<NPCController> npcs, NPCBehavior behavior)
        {
            foreach (var npc in npcs)
            {
                float delay = Random.Range(minStaggerDelay, maxStaggerDelay);
                npc.SetBehaviorDelayed(behavior, delay);
            }
        }

        private List<NPCController> GetRandomSubset(List<NPCController> source, int count)
        {
            var result = new List<NPCController>(source);
            // Fisher-Yates shuffle
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
            return result.GetRange(0, Mathf.Min(count, result.Count));
        }

        private void PlayClipAtNPC(NPCController npc, AudioClip clip)
        {
            if (clip == null) return;
            var source = npc.GetComponent<AudioSource>();
            if (source == null)
                source = npc.gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.clip = clip;
            source.Play();
        }
    }
}

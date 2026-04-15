using UnityEngine;
using System;

namespace VRDiagnostics
{
    [Serializable]
    public class DialogueLine
    {
        public string speakerName;
        public AudioClip audioClip;
        [TextArea] public string subtitleText;
        public float duration; // if 0, uses clip length or estimates from text
    }
}

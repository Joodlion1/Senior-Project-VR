using UnityEngine;
using UnityEngine.UI;

namespace VRDiagnostics
{
    public class AudioSpectrumUI : MonoBehaviour
    {
        [Header("Spectrum Bars")]
        [SerializeField] private RectTransform[] spectrumBars;
        [SerializeField] private int barCount = 8;
        [SerializeField] private float barMaxHeight = 100f;
        [SerializeField] private float barMinHeight = 5f;
        [SerializeField] private float sensitivity = 50f;
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Mic Settings")]
        [SerializeField] private float micThreshold = 0.005f;

        [Header("Visual")]
        [SerializeField] private Color activeColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color idleColor = new Color(0.3f, 0.3f, 0.3f);

        public bool IsActive { get; private set; }
        public bool IsSpeechDetected { get; private set; }

        private AudioClip micClip;
        private string micDevice;
        private float[] samples;
        private float[] spectrumData;
        private float[] barHeights;
        private bool micInitialized;

        private void Awake()
        {
            samples = new float[256];
            spectrumData = new float[barCount];
            barHeights = new float[barCount];
        }

        public void Activate()
        {
            IsActive = true;
            gameObject.SetActive(true);
            StartMicrophone();
        }

        public void Deactivate()
        {
            IsActive = false;
            StopMicrophone();
            gameObject.SetActive(false);
        }

        private void StartMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[AudioSpectrumUI] No microphone found.");
                return;
            }

            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 1, 44100);
            micInitialized = true;
        }

        private void StopMicrophone()
        {
            if (micInitialized && !string.IsNullOrEmpty(micDevice))
            {
                Microphone.End(micDevice);
                micInitialized = false;
            }
        }

        private void Update()
        {
            if (!IsActive || !micInitialized) return;

            UpdateSpectrum();
            UpdateBars();
        }

        private void UpdateSpectrum()
        {
            int micPosition = Microphone.GetPosition(micDevice);
            if (micPosition <= 0 || micClip == null) return;

            int startPos = Mathf.Max(0, micPosition - samples.Length);
            micClip.GetData(samples, startPos);

            // Calculate energy in frequency bands
            float totalEnergy = 0f;
            int samplesPerBar = samples.Length / barCount;

            for (int i = 0; i < barCount; i++)
            {
                float sum = 0f;
                for (int j = 0; j < samplesPerBar; j++)
                {
                    int idx = i * samplesPerBar + j;
                    if (idx < samples.Length)
                        sum += Mathf.Abs(samples[idx]);
                }
                spectrumData[i] = (sum / samplesPerBar) * sensitivity;
                totalEnergy += spectrumData[i];
            }

            IsSpeechDetected = (totalEnergy / barCount) > micThreshold;
        }

        private void UpdateBars()
        {
            if (spectrumBars == null) return;

            int count = Mathf.Min(spectrumBars.Length, barCount);
            for (int i = 0; i < count; i++)
            {
                if (spectrumBars[i] == null) continue;

                float targetHeight = Mathf.Lerp(barMinHeight, barMaxHeight, Mathf.Clamp01(spectrumData[i]));
                barHeights[i] = Mathf.Lerp(barHeights[i], targetHeight, Time.deltaTime * smoothSpeed);

                var sizeDelta = spectrumBars[i].sizeDelta;
                sizeDelta.y = barHeights[i];
                spectrumBars[i].sizeDelta = sizeDelta;

                // Color
                var img = spectrumBars[i].GetComponent<Image>();
                if (img != null)
                    img.color = IsSpeechDetected ? activeColor : idleColor;
            }
        }

        private void OnDisable()
        {
            StopMicrophone();
        }
    }
}

using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using Debug = UnityEngine.Debug;

namespace Whisper.Samples
{
    public class MicrophoneDemo : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public bool streamSegments = true;
        public bool printLanguage = true;

        [Header("UI")] 
        public Button button;
        public Text buttonText;
        public Text outputText;
        public Text timeText;
        public Dropdown languageDropdown;
        public Toggle translateToggle;
        
        private string _buffer;

        private void Awake()
        {
            Debug.Log("Awake");
            button.onClick.AddListener(OnButtonPressed);

            languageDropdown.value = languageDropdown.options
                .FindIndex(op => op.text == whisper.language);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            translateToggle.isOn = whisper.translateToEnglish;
            translateToggle.onValueChanged.AddListener(OnTranslateChanged);

            microphoneRecord.OnRecordStop += Transcribe;
            
            if (streamSegments)
                whisper.OnNewSegment += WhisperOnOnNewSegment;
        }

        private void OnButtonPressed()
        {
            Debug.Log("Button");
            if (!microphoneRecord.IsRecording)
                microphoneRecord.StartRecord();
            else
                microphoneRecord.StopRecord();

            if (buttonText)
                buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
        }
        
        private void OnLanguageChanged(int ind)
        {
            var opt = languageDropdown.options[ind];
            whisper.language = opt.text;
        }
        
        private void OnTranslateChanged(bool translate)
        {
            whisper.translateToEnglish = translate;
        }

        private async void Transcribe(float[] data, int frequency, int channels, float length)
        {
            _buffer = "";
            
            var sw = new Stopwatch();
            sw.Start();
            
            Debug.Log($"Before transcribing");
            var res = await whisper.GetTextAsync(data, frequency, channels);
            Debug.Log($"Transcribed: {res.Result}. Lang: {res.Language}");

            var time = sw.ElapsedMilliseconds;
            var rate = length / (time * 0.001f);
            timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";
            if (res == null)
                return;

            var text = res.Result;
            if (printLanguage)
                text += $"\n\nLanguage: {res.Language}";
            outputText.text = text;
        }
        
        private void WhisperOnOnNewSegment(WhisperSegment segment)
        {
            _buffer += segment.Text;
            outputText.text = _buffer + "...";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Utils
{
    public class MicrophoneRecord : MonoBehaviour
    {
        public int maxLengthSec = 30;
        public int frequency = 16000;
        public bool echo = true;
        
        [Header("Microphone selection (optional)")] 
        [CanBeNull] public Dropdown microphoneDropdown;
        public string microphoneDefaultLabel = "Default microphone";
        
        private float _recordStart;
        private AudioClip _clip;
        private float _length;

        private string _selectedMicDevice;
        public string SelectedMicDevice
        {
            get => _selectedMicDevice;
            set
            {
                if (value != null && !AvailableMicDevices.Contains(value))
                    throw new ArgumentException("Microphone device not found");
                _selectedMicDevice = value;
            }
        }

        public string RecordStartMicDevice { get; private set; }
        public bool IsRecording { get; private set; }

        public IEnumerable<string> AvailableMicDevices => Microphone.devices;

        private void Awake()
        {
            if(microphoneDropdown != null)
            {
                microphoneDropdown.options = AvailableMicDevices
                    .Prepend(microphoneDefaultLabel)
                    .Select(text => new Dropdown.OptionData(text))
                    .ToList();
                microphoneDropdown.value = microphoneDropdown.options
                    .FindIndex(op => op.text == microphoneDefaultLabel);
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            }
/*#if UNITY_WEBGL && !UNITY_EDITOR
            Microphone.Init();
            Microphone.QueryAudioInput();
#endif*/
        }


        
        private void Update()
        {
/*#if UNITY_WEBGL && !UNITY_EDITOR
            Microphone.Update();
#endif*/
            Debug.Log(Microphone.devices);
            if (!IsRecording)
                return;

            var timePassed = Time.realtimeSinceStartup - _recordStart;
            if (timePassed > maxLengthSec)
                StopRecord();
        }

        private void OnMicrophoneChanged(int ind)
        {
            if (microphoneDropdown == null) return;
            var opt = microphoneDropdown.options[ind];
            SelectedMicDevice = opt.text == microphoneDefaultLabel ? null : opt.text;
        }

        public void StartRecord()
        {
//#if !UNITY_WEBGL
            if (IsRecording)
                return;

            _recordStart = Time.realtimeSinceStartup;
            RecordStartMicDevice = SelectedMicDevice;
            _clip = Microphone.Start(RecordStartMicDevice, false, maxLengthSec, frequency);
            IsRecording = true;
//#endif
        }

        public void StopRecord()
        {
//#if !UNITY_WEBGL
            if (!IsRecording)
                return;

            var data = GetTrimmedData();
            if (echo)
            {
                var echoClip = AudioClip.Create("echo", data.Length,
                    _clip.channels, _clip.frequency, false);
                echoClip.SetData(data, 0);
                AudioSource.PlayClipAtPoint(echoClip, Vector3.zero);
            }

            Microphone.End(RecordStartMicDevice);
            IsRecording = false;
            _length = Time.realtimeSinceStartup - _recordStart;
            Debug.Log($"Data array: {data.Length}:");
            var dataStr = data.Take(100).Select(x => x.ToString(CultureInfo.InvariantCulture)).Aggregate((a,b) => $"{a}, {b}");
            Debug.Log($"{dataStr}\n");
            Debug.Log($"freq={_clip.frequency}, channels={_clip.channels}, length={_length}");
            OnRecordStop?.Invoke(data, _clip.frequency, _clip.channels, _length);
//#endif
        }
        
        public delegate void OnRecordStopDelegate(float[] data, int frequency, int channels, float length);
        public event OnRecordStopDelegate OnRecordStop;

        private float[] GetTrimmedData()
        {
//#if !UNITY_WEBGL
            // get microphone samples and current position
            var pos = Microphone.GetPosition(RecordStartMicDevice);
            var origData = new float[_clip.samples * _clip.channels];
            _clip.GetData(origData, 0);

            // check if mic just reached audio buffer end
            if (pos == 0)
                return origData;

            // looks like we need to trim it by pos
            var trimData = new float[pos];
            Array.Copy(origData, trimData, pos);
            return trimData;
//#else
//            return Array.Empty<float>();
//#endif
        }
    }
}
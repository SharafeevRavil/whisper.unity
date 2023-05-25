using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Whisper.Native;
using Whisper.Utils;

namespace Whisper
{
    public class WhisperManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Path to model weights file relative to StreamingAssets")]
        private string modelPath = "Whisper/ggml-base.bin";
        
        [SerializeField]
        [Tooltip("Should model weights be loaded on awake?")]
        private bool initOnAwake = true;
        
        [Header("Language")]
        [Tooltip("Output text language. Use empty or \"auto\" for auto-detection.")]
        public string language = "en";
        [Tooltip("Force output text to English translation. Improves translation quality.")]
        public bool translateToEnglish;
        
        [Header("Advanced settings")]
        [SerializeField]
        private WhisperSamplingStrategy strategy = WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY;

        [Tooltip("Do not use past transcription (if any) as initial prompt for the decoder.")]
        public bool noContext = true;
        
        [Tooltip("Force single segment output (useful for streaming).")]
        public bool singleSegment;
        
        [Tooltip("Output tokens with their confidence in each segment.")]
        public bool enableTokens;

        [Header("Experimental settings")]
        [Tooltip("[EXPERIMENTAL] Output timestamps for each token. Need enabled tokens to work.")]
        public bool tokensTimestamps;
        
        [Tooltip("[EXPERIMENTAL] Speed-up the audio by 2x using Phase Vocoder. " +
                 "These can significantly reduce the quality of the output.")]
        public bool speedUp = false;
        
        [Tooltip("[EXPERIMENTAL] Overwrite the audio context size (0 = use default). " +
                 "These can significantly reduce the quality of the output.")]
        public int audioCtx;

        public event OnNewSegmentDelegate OnNewSegment;

        private WhisperWrapper _whisper;
        private WhisperParams _params;
        private readonly MainThreadDispatcher _dispatcher = new MainThreadDispatcher();

        public bool IsLoaded => _whisper != null;
        public bool IsLoading { get; private set; }

        private async void Awake()
        {
            if (!initOnAwake)
                return;
            await InitModel();
        }

        private void Update()
        {
            _dispatcher.Update();
        }

        /// <summary>
        /// Load model and default parameters. Prepare it for text transcription.
        /// </summary>
        public async Task InitModel()
        {
            // check if model is already loaded or actively loading
            if (IsLoaded)
            {
                Debug.LogWarning("Whisper model is already loaded and ready for use!");
                return;
            } 
            if (IsLoading)
            {
                Debug.LogWarning("Whisper model is already loading!");
                return;
            }
            
            // load model and default params
            IsLoading = true;
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, modelPath);
                Debug.Log("InitFromFileAsync");
                _whisper = await WhisperWrapper.InitFromFileAsync(path);
                Debug.Log("GetDefaultParams");
                _params = WhisperParams.GetDefaultParams(strategy);
                _whisper.OnNewSegment += OnNewSegmentHandler;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            IsLoading = false;
        }

        public bool IsMultilingual()
        {
            if (!IsLoaded)
            {
                Debug.LogError("Whisper model isn't loaded! Init Whisper model first!");
                return false;
            }

            return _whisper.IsMultilingual;
        }

        /// <summary>
        /// Get transcription from audio clip.
        /// </summary>
        public async Task<WhisperResult> GetTextAsync(AudioClip clip)
        {
            var isLoaded = await CheckIfLoaded();
            if (!isLoaded)
                return null;
            
            UpdateParams();
            var res = await _whisper.GetTextAsync(clip, _params);
            return res;
        }
        
        
        /// <summary>
        /// Get transcription from audio buffer.
        /// </summary>
        public async Task<WhisperResult> GetTextAsync(float[] samples, int frequency, int channels)
        {
            Debug.Log("CheckIfLoaded");
            var isLoaded = await CheckIfLoaded();
            Debug.Log($"CheckIfLoaded {isLoaded}");
            if (!isLoaded)
                return null;

            Debug.Log("UpdateParams");
            UpdateParams();
            Debug.Log("_whisper.GetTextAsync");
            var res = await _whisper.GetTextAsync(samples, frequency, channels, _params);
            Debug.Log($"_whisper.GetTextAsync {res}");
            return res;
        }

        private void UpdateParams()
        {
            _params.Language = language;
            _params.Translate = translateToEnglish;
            _params.NoContext = noContext;
            _params.SingleSegment = singleSegment;
            _params.SpeedUp = speedUp;
            _params.AudioCtx = audioCtx;
            _params.EnableTokens = enableTokens;
            _params.TokenTimestamps = tokensTimestamps;
        }

        private async Task<bool> CheckIfLoaded()
        {
            if (!IsLoaded && !IsLoading)
            {
                Debug.LogError("Whisper model isn't loaded! Init Whisper model first!");
                return false;
            }
            
            // wait while model still loading
            while (IsLoading)
            {
                Debug.Log($"Loading {IsLoading}. Loaded {IsLoaded}");
                await Task.Yield();
            }
            Debug.Log($"Its loaded perhaps. Loading {IsLoading}. Loaded {IsLoaded}");

            return IsLoaded;
        }
        
        private void OnNewSegmentHandler(WhisperSegment segment)
        {
            _dispatcher.Execute(() =>
            {
                OnNewSegment?.Invoke(segment);
            });
        }
    }
}


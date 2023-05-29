using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Whisper.Samples
{
    public class InitialPromptDemo : MonoBehaviour
    {
        [Serializable]
        public class InitialPrompt
        {
            public string name;
            public string prompt;
        }
        
        public WhisperManager manager;
        public AudioClip clip;
        public bool echoSound = true;

        public List<InitialPrompt> initialPrompts = new()
        {
            new InitialPrompt
            {
                name = "lowercase",
                prompt = "hello how is it going always use lowercase no punctuation goodbye one two three start stop i you me they",
            },
            new InitialPrompt
            {
                name = "Start of the clip",
                prompt = "And so my fellow Americans, ask not what your country can do for you",
            },
            new InitialPrompt
            {
                name = "UPPERCASE",
                prompt = "EVERY WORD IS WRITTEN IN CAPITAL LETTERS AS IF THE CAPS LOCK KEY WAS PRESSED",
            },
            new InitialPrompt
            {
                name = "Custom",
                prompt = "",
            },
        };
        
        [Header("Text Output")]
        public bool streamSegments = true;
        public bool printLanguage = true;
        public bool showTimestamps;

        [Header("UI")]
        public Button button;
        public Text outputText;
        public Text timeText;
        public Dropdown initialPromptDropdown;
        public InputField selectedInitialPromptInput;

        private string _buffer;

        private void Awake()
        {
            button.onClick.AddListener(ButtonPressed);
            if (streamSegments)
                manager.OnNewSegment += OnNewSegmentHandler;

            initialPromptDropdown.options = initialPrompts
                .Select(x => new Dropdown.OptionData(x.name))
                .ToList();
            initialPromptDropdown.onValueChanged.AddListener(OnInitialPromptChanged);
            initialPromptDropdown.value = 0;
            OnInitialPromptChanged(initialPromptDropdown.value);
        }

        private void OnDestroy()
        {
            if (streamSegments)
                manager.OnNewSegment -= OnNewSegmentHandler;
        }

        private void OnInitialPromptChanged(int ind)
        {
            if (initialPromptDropdown == null) return;
            selectedInitialPromptInput.text = initialPrompts[ind].prompt;
        }

        public async void ButtonPressed()
        {
            _buffer = "";
            if (echoSound)
                AudioSource.PlayClipAtPoint(clip, Vector3.zero);
            
            manager.initialPrompt = selectedInitialPromptInput.text;
            
            var sw = new Stopwatch();
            sw.Start();
            
            var res = await manager.GetTextAsync(clip);
            if (res == null) 
                return;

            var time = sw.ElapsedMilliseconds;
            var rate = clip.length / (time * 0.001f);
            timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";

            var text = GetFinalText(res);
            if (printLanguage)
                text += $"\n\nLanguage: {res.Language}";
            
            outputText.text = text;
        }
        
        private void OnNewSegmentHandler(WhisperSegment segment)
        {
            if (!showTimestamps)
            {
                _buffer += segment.Text;
                outputText.text = _buffer + "...";
            }
            else
            {
                _buffer += $"<b>{segment.TimestampToString()}</b>{segment.Text}\n";
                outputText.text = _buffer;
            }
        }

        private string GetFinalText(WhisperResult output)
        {
            if (!showTimestamps)
                return output.Result;

            var sb = new StringBuilder();
            foreach (var seg in output.Segments)
            {
                var segment = $"<b>{seg.TimestampToString()}</b>{seg.Text}\n";
                sb.Append(segment);
            }

            return sb.ToString();
        }

    }
}


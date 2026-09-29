using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Pollinations
{
    /// <summary>Text generation for a layer of dialogue, a hint or a sign.</summary>
    [AddComponentMenu("Pollinations/Pollinations Text")]
    public class PollinationsText : PollinationsBehaviour
    {
        [Serializable]
        public sealed class TextEvent : UnityEvent<string>
        {
        }

        [Header("Text")]
        [Tooltip("Any model from the live text catalogue. nova-fast is the cheap default.")]
        [SerializeField] private string model = PollinationsConfig.DefaultTextModel;

        [Tooltip("Prepended as a system message, so a character keeps its voice.")]
        [TextArea(2, 5)]
        [SerializeField] private string systemPrompt = "";

        [Range(0f, 2f)]
        [SerializeField] private float temperature = 0.8f;

        [Tooltip("0 lets the model decide.")]
        [SerializeField] private int maxTokens = 0;

        [Tooltip("Called with the answer; wire it to a label or a dialogue box.")]
        public TextEvent onGenerated = new TextEvent();

        /// <summary>Called with the whole result, for code that needs the error or the usage.</summary>
        public event Action<PollinationsResult> Generated;

        public string Model
        {
            get { return model; }
            set { model = value; }
        }

        public string SystemPrompt
        {
            get { return systemPrompt; }
            set { systemPrompt = value; }
        }

        /// <summary>Asks for an answer and returns everything the API said.</summary>
        public async Task<PollinationsResult> GenerateAsync(string prompt)
        {
            PollinationsResult result = await ChatAsync(PollinationsChat.BuildMessages(prompt, systemPrompt));
            if (result.Ok && string.IsNullOrEmpty(PollinationsChat.ExtractText(result.Json())))
            {
                result = PollinationsResult.Failure(
                    PollinationsErrorKind.Parse,
                    result.StatusCode,
                    result.Url,
                    "the response carried no text");
            }

            Report(result);
            if (result.Ok)
            {
                string answer = PollinationsChat.ExtractText(result.Json());
                onGenerated.Invoke(answer);
                if (Generated != null)
                {
                    Generated(result);
                }
            }

            return result;
        }

        /// <summary>The common case: just the answer, or an empty string on failure.</summary>
        public async Task<string> GenerateTextAsync(string prompt)
        {
            PollinationsResult result = await GenerateAsync(prompt);
            return result.Ok ? PollinationsChat.ExtractText(result.Json()) : "";
        }

        /// <summary>Sends a whole conversation, for dialogue that keeps its history.</summary>
        public async Task<PollinationsResult> ChatAsync(List<PollinationsMessage> messages)
        {
            Dictionary<string, object> payload = PollinationsChat.BuildPayload(model, messages, temperature, maxTokens);
            return await Client.PostJsonAsync(PollinationsUrls.ChatCompletions(), payload);
        }

        /// <summary>The cheap single-prompt route, for one-off text.</summary>
        public async Task<string> QuickAsync(string prompt, int seed = -1)
        {
            PollinationsResult result = await Client.GetAsync(PollinationsUrls.TextPrompt(prompt, model, seed));
            Report(result);
            if (!result.Ok)
            {
                return "";
            }

            // this route answers with JSON, but a plain string is handled too
            string text = PollinationsChat.ExtractText(result.Json());
            return string.IsNullOrEmpty(text) ? result.Text : text;
        }
    }
}

using System.Collections.Generic;

namespace Pollinations
{
    /// <summary>A chat turn.</summary>
    public sealed class PollinationsMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }

        public PollinationsMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "role", Role },
                { "content", Content },
            };
        }
    }

    /// <summary>
    /// Builds the chat payload and reads the answer back out. Kept apart from the
    /// Unity components so it can be tested without an editor.
    /// </summary>
    public static class PollinationsChat
    {
        public static Dictionary<string, object> BuildPayload(
            string model,
            List<PollinationsMessage> messages,
            double temperature = 0.8,
            int maxTokens = 0,
            double seed = -1)
        {
            var wire = new List<object>();
            if (messages != null)
            {
                foreach (PollinationsMessage message in messages)
                {
                    wire.Add(message.ToDictionary());
                }
            }

            var payload = new Dictionary<string, object>
            {
                { "model", model ?? "" },
                { "messages", wire },
            };

            if (temperature > 0)
            {
                payload["temperature"] = temperature;
            }

            if (maxTokens > 0)
            {
                payload["max_tokens"] = (double)maxTokens;
            }

            if (seed >= 0)
            {
                payload["seed"] = seed;
            }

            return payload;
        }

        public static List<PollinationsMessage> BuildMessages(string prompt, string systemPrompt = "")
        {
            var messages = new List<PollinationsMessage>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new PollinationsMessage("system", systemPrompt));
            }

            messages.Add(new PollinationsMessage("user", prompt ?? ""));
            return messages;
        }

        /// <summary>
        /// Pulls the answer out of a chat response. Handles the OpenAI shape and
        /// the plain-text shape the prompt route answers with.
        /// </summary>
        public static string ExtractText(Dictionary<string, object> response)
        {
            if (response == null)
            {
                return "";
            }

            object choicesValue;
            if (response.TryGetValue("choices", out choicesValue))
            {
                var choices = choicesValue as List<object>;
                if (choices != null && choices.Count > 0)
                {
                    var first = choices[0] as Dictionary<string, object>;
                    if (first != null)
                    {
                        object messageValue;
                        if (first.TryGetValue("message", out messageValue))
                        {
                            string fromMessage = ContentToText(messageValue);
                            if (!string.IsNullOrEmpty(fromMessage))
                            {
                                return fromMessage;
                            }
                        }

                        string fromText = PollinationsJson.GetString(first, "text");
                        if (!string.IsNullOrEmpty(fromText))
                        {
                            return fromText;
                        }
                    }
                }
            }

            string[] keys = { "content", "text", "response", "output_text", "message", "result", "answer" };
            foreach (string key in keys)
            {
                string value = ContentToText(response.ContainsKey(key) ? response[key] : null);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return "";
        }

        /// <summary>"{prompt_tokens: 14, completion_tokens: 23}" when the API reported usage.</summary>
        public static Dictionary<string, object> ExtractUsage(Dictionary<string, object> response)
        {
            if (response == null || !response.ContainsKey("usage"))
            {
                return null;
            }

            return response["usage"] as Dictionary<string, object>;
        }

        public static string ExtractModel(Dictionary<string, object> response)
        {
            return response == null ? "" : PollinationsJson.GetString(response, "model");
        }

        /// <summary>Handles a string body, a list of parts and the {"type":"text"} part shape.</summary>
        public static string ContentToText(object value)
        {
            if (value == null)
            {
                return "";
            }

            string direct = PollinationsJson.AsString(value);
            if (direct != null)
            {
                return direct;
            }

            var list = value as List<object>;
            if (list != null)
            {
                var builder = new System.Text.StringBuilder();
                foreach (object item in list)
                {
                    string piece = ContentToText(item);
                    if (!string.IsNullOrEmpty(piece))
                    {
                        builder.Append(piece);
                    }
                }

                return builder.ToString();
            }

            var dictionary = value as Dictionary<string, object>;
            if (dictionary != null)
            {
                string text = PollinationsJson.GetString(dictionary, "text");
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }

                string content = PollinationsJson.GetString(dictionary, "content");
                if (!string.IsNullOrEmpty(content))
                {
                    return content;
                }

                if (dictionary.ContainsKey("content"))
                {
                    return ContentToText(dictionary["content"]);
                }

                if (dictionary.ContainsKey("message"))
                {
                    return ContentToText(dictionary["message"]);
                }
            }

            return "";
        }
    }
}

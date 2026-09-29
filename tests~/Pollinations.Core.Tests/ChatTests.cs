using System.Collections.Generic;

namespace Pollinations.Tests
{
    public static class ChatTests
    {
        public static void Run()
        {
            Check.Suite("chat");

            List<PollinationsMessage> messages = PollinationsChat.BuildMessages("hello", "be brief");
            Check.Equal(2, messages.Count, "a system prompt is prepended");
            Check.Equal("system", messages[0].Role, "the first role is system");
            Check.Equal("be brief", messages[0].Content, "the system content");
            Check.Equal("user", messages[1].Role, "the second role is user");
            Check.Equal("hello", messages[1].Content, "the user content");
            Check.Equal(1, PollinationsChat.BuildMessages("hello").Count, "no system prompt by default");
            Check.Equal("", PollinationsChat.BuildMessages(null)[0].Content, "a null prompt becomes an empty string");

            Dictionary<string, object> payload = PollinationsChat.BuildPayload("nova-fast", messages, 0.5, 128, 7);
            Check.Equal("nova-fast", PollinationsJson.GetString(payload, "model"), "the payload carries the model");
            Check.Equal(0.5, PollinationsJson.GetNumber(payload, "temperature", 0), "the payload carries the temperature");
            Check.Equal(128.0, PollinationsJson.GetNumber(payload, "max_tokens", 0), "the payload carries max_tokens");
            Check.Equal(7.0, PollinationsJson.GetNumber(payload, "seed", 0), "the payload carries the seed");
            Check.Equal(2, PollinationsJson.AsArray(payload["messages"]).Count, "the payload carries the messages");
            Check.Contains(PollinationsJson.Write(payload), "\"role\":\"system\"", "the wire form has roles");

            Dictionary<string, object> minimal = PollinationsChat.BuildPayload("m", messages, 0, 0, -1);
            Check.False(minimal.ContainsKey("max_tokens"), "no max_tokens when unset");
            Check.False(minimal.ContainsKey("seed"), "no seed when unset");
            Check.False(minimal.ContainsKey("temperature"), "no temperature when unset");

            string openAi = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"A watermill grinds grain.\"}}],\"model\":\"us.amazon.nova-micro-v1:0\",\"usage\":{\"prompt_tokens\":14,\"completion_tokens\":23,\"total_tokens\":37}}";
            Dictionary<string, object> response = PollinationsJson.ParseObject(openAi);
            Check.Equal("A watermill grinds grain.", PollinationsChat.ExtractText(response), "reads the chat answer");
            Check.Equal("us.amazon.nova-micro-v1:0", PollinationsChat.ExtractModel(response), "reads the served model");
            Dictionary<string, object> usage = PollinationsChat.ExtractUsage(response);
            Check.NotNull(usage, "reads usage");
            Check.Equal(14.0, PollinationsJson.GetNumber(usage, "prompt_tokens", 0), "prompt tokens");
            Check.Equal(23.0, PollinationsJson.GetNumber(usage, "completion_tokens", 0), "completion tokens");
            Check.Null(PollinationsChat.ExtractUsage(PollinationsJson.ParseObject("{}")), "no usage when absent");

            Check.Equal("plain", PollinationsChat.ExtractText(PollinationsJson.ParseObject("{\"content\":\"plain\"}")), "reads a content field");
            Check.Equal("via text", PollinationsChat.ExtractText(PollinationsJson.ParseObject("{\"choices\":[{\"text\":\"via text\"}]}")), "reads a completion text field");
            Check.Equal("answer", PollinationsChat.ExtractText(PollinationsJson.ParseObject("{\"response\":\"answer\"}")), "reads a response field");
            Check.Equal("joined", PollinationsChat.ExtractText(PollinationsJson.ParseObject("{\"output_text\":\"joined\"}")), "reads output_text");
            Check.Equal("", PollinationsChat.ExtractText(PollinationsJson.ParseObject("{\"unknown\":1}")), "no text when nothing matches");
            Check.Equal("", PollinationsChat.ExtractText(null), "no text without a response");
            Check.Equal("", PollinationsChat.ExtractModel(null), "no model without a response");
            Check.Equal("", PollinationsChat.ExtractText(PollinationsJson.ParseObject("{\"choices\":[]}")), "an empty choice list yields nothing");

            Check.Equal("from parts", PollinationsChat.ContentToText(
                PollinationsJson.Parse("[{\"type\":\"text\",\"text\":\"from \"},{\"type\":\"text\",\"text\":\"parts\"}]")),
                "joins content parts");
            Check.Equal("nested", PollinationsChat.ContentToText(
                PollinationsJson.ParseObject("{\"content\":{\"content\":\"nested\"}}")),
                "reads a nested content");
            Check.Equal("", PollinationsChat.ContentToText(null), "no content for null");

            Check.Contains(PollinationsChat.ExtractText(PollinationsJson.ParseObject("{\"message\":{\"content\":\"hi\"}}")), "hi", "reads a message object");
        }
    }
}

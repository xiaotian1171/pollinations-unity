using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Pollinations.Tests
{
    /// <summary>
    /// Talks to the real API through the same core the package ships, and prints
    /// status codes, latencies and payload shapes as evidence.
    /// </summary>
    public static class LiveCheck
    {
        private static int _failures;

        public static async Task<int> RunAsync()
        {
            Console.WriteLine();
            Console.WriteLine("=== Pollinations live check");
            string key = PollinationsConfig.ApiKey;
            Console.WriteLine("key present: " + (string.IsNullOrEmpty(key) ? "no (anonymous only)" : "yes"));
            Console.WriteLine("app key: " + PollinationsConfig.AppKey);

            var client = new PollinationsClient(new HttpClientTransport());

            await AnonymousAsync(client);
            await PromptRouteAsync(client);
            await ChatAsync(client);
            await CatalogAsync(client, "text");
            await CatalogAsync(client, "image");
            await CatalogAsync(client, "audio");
            await ImageAsync(client);
            await SpeechAsync(client);
            await DeviceFlowAsync(client);

            Console.WriteLine();
            Console.WriteLine("=== live check finished: " + _failures + " failing step(s)");
            return _failures == 0 ? 0 : 1;
        }

        private static void Pass(string what, PollinationsResult result, string extra = "")
        {
            string line = "[ok] " + what + " status=" + result.StatusCode + " attempts=" + result.Attempts
                + " " + result.Seconds.ToString("0.00") + "s";
            if (!string.IsNullOrEmpty(extra))
            {
                line += " " + extra;
            }

            Console.WriteLine(line);
        }

        private static void Fail(string what, PollinationsResult result)
        {
            _failures++;
            Console.WriteLine("[FAIL] " + what + " kind=" + result.KindName + " status=" + result.StatusCode + " error=" + result.Error);
        }

        private static async Task AnonymousAsync(PollinationsClient client)
        {
            // no Authorization header at all, on purpose
            var request = new PollinationsRequest { Method = "GET", Url = PollinationsUrls.Models("text") };
            PollinationsResult result = await client.SendAsync(request, false);
            if (result.Ok)
            {
                Pass("anonymous catalogue", result);
            }
            else
            {
                Console.WriteLine("[skip] anonymous requests answered " + result.StatusCode + " kind=" + result.KindName + ": " + result.Error);
            }
        }

        private static async Task PromptRouteAsync(PollinationsClient client)
        {
            PollinationsResult result = await client.GetAsync(PollinationsUrls.TextPrompt("What is pollen? Answer in one line."));
            if (!result.Ok)
            {
                Fail("prompt route", result);
                return;
            }

            Console.WriteLine("[ok] prompt route status=" + result.StatusCode + " " + result.Seconds.ToString("0.00") + "s");
            Console.WriteLine("     content=" + Trim(FirstLine(result.Text), 120));
        }

        private static async Task ChatAsync(PollinationsClient client)
        {
            string model = Environment.GetEnvironmentVariable("POLLINATIONS_TEST_MODEL") ?? PollinationsConfig.DefaultTextModel;
            Dictionary<string, object> payload = PollinationsChat.BuildPayload(
                model, PollinationsChat.BuildMessages("What is a watermill? One sentence.", "Be brief."), 0.5, 64);
            PollinationsResult result = await client.PostJsonAsync(PollinationsUrls.ChatCompletions(), payload);
            if (!result.Ok)
            {
                Fail("chat (" + model + ")", result);
                return;
            }

            Dictionary<string, object> json = result.Json();
            string answer = PollinationsChat.ExtractText(json);
            if (string.IsNullOrEmpty(answer))
            {
                Fail("chat returned no text", result);
                return;
            }

            Pass("chat", result, "model=" + PollinationsChat.ExtractModel(json));
            Console.WriteLine("     content=" + Trim(answer, 140));
            Dictionary<string, object> usage = PollinationsChat.ExtractUsage(json);
            if (usage != null)
            {
                Console.WriteLine("     usage=" + PollinationsJson.Write(usage));
            }
        }

        private static async Task CatalogAsync(PollinationsClient client, string modality)
        {
            PollinationsResult result = await client.GetAsync(PollinationsUrls.Models(modality));
            if (!result.Ok)
            {
                Fail("catalogue " + modality, result);
                return;
            }

            List<PollinationsModel> models = PollinationsModels.Parse(result.Text);
            if (models.Count == 0)
            {
                Fail("catalogue " + modality + " parsed to nothing", result);
                return;
            }

            Pass("catalogue " + modality, result, models.Count + " models");
            Console.WriteLine("     first ids: " + string.Join(", ", PollinationsModels.Ids(models).GetRange(0, Math.Min(5, models.Count))));
        }

        private static async Task ImageAsync(PollinationsClient client)
        {
            PollinationsResult result = await client.GetAsync(PollinationsUrls.ImagePrompt("a wooden watermill, flat illustration", "", 256, 256));
            if (!result.Ok)
            {
                Fail("image", result);
                return;
            }

            if (result.Bytes.Length == 0 || !string.IsNullOrEmpty(result.Text))
            {
                Fail("image body was not kept as bytes", result);
                return;
            }

            Pass("image", result, "content-type=" + result.ContentType + " bytes=" + result.Bytes.Length);
        }

        private static async Task SpeechAsync(PollinationsClient client)
        {
            // Most speech models bill to paid pollen and answer 402 without it, while
            // openai/tts-1 draws on the ordinary pollen balance. The model can be
            // swapped, the same way the chat step is:
            //   POLLINATIONS_TEST_SPEECH_MODEL=openai/tts-1
            string model = Environment.GetEnvironmentVariable("POLLINATIONS_TEST_SPEECH_MODEL") ?? PollinationsConfig.DefaultSpeechModel;
            var payload = new Dictionary<string, object>
            {
                { "model", model },
                { "input", "Welcome to Pollen Village." },
                { "voice", PollinationsConfig.DefaultVoice },
                { "response_format", "wav" },
            };
            PollinationsResult result = await client.PostJsonAsync(PollinationsUrls.Speech(), payload);
            string what = "speech (" + model + ")";
            if (result.Ok)
            {
                PollinationsAudio audio = PollinationsWav.Decode(result.Bytes);
                Pass(what, result, "content-type=" + result.ContentType + " bytes=" + result.Bytes.Length
                    + (audio == null ? "" : " wav=" + audio.SampleRate + "Hz " + audio.Channels + "ch " + audio.Seconds.ToString("0.0") + "s"));
                return;
            }

            if (result.Kind == PollinationsErrorKind.Balance)
            {
                Console.WriteLine("[skip] " + what + " needs paid pollen: " + Trim(result.Error, 140));
                Console.WriteLine("       set POLLINATIONS_TEST_SPEECH_MODEL=openai/tts-1 to bill the ordinary pollen balance instead");
                return;
            }

            Fail(what, result);
        }

        private static async Task DeviceFlowAsync(PollinationsClient client)
        {
            PollinationsResult result = await client.PostJsonAnonymousAsync(
                PollinationsUrls.DeviceCode(), PollinationsDeviceFlow.BuildCodeRequest(PollinationsConfig.AppKey));
            if (!result.Ok)
            {
                Fail("device code", result);
                return;
            }

            string error;
            PollinationsDeviceCode code = PollinationsDeviceFlow.ParseCode(result.Text, out error);
            if (code == null)
            {
                Fail("device code did not parse: " + error, result);
                return;
            }

            Pass("device code", result, "user_code=" + code.UserCode + " url=" + code.VerificationUrl
                + " interval=" + code.Interval + " expires_in=" + code.ExpiresIn);

            PollinationsResult poll = await client.PostJsonAnonymousAsync(
                PollinationsUrls.DeviceToken(), PollinationsDeviceFlow.BuildTokenRequest(code.DeviceCode));
            PollinationsTokenPoll token = PollinationsDeviceFlow.ParseToken(poll.StatusCode, poll.Text);
            Console.WriteLine("[ok] device token status=" + poll.StatusCode + " state=" + token.State
                + (token.State == PollinationsTokenState.Pending ? " (pending is correct: nobody approved)" : ""));
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            int newline = text.IndexOf('\n');
            return newline < 0 ? text : text.Substring(0, newline);
        }

        private static string Trim(string text, int limit)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            return text.Length <= limit ? text : text.Substring(0, limit) + "...";
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pollinations.Tests
{
    public static class ClientTests
    {
        public static void Run()
        {
            Check.Suite("client");

            PollinationsConfig.UseKeyStore(new InMemoryKeyStore());
            PollinationsConfig.SetApiKey("sk_test_key");

            var transport = new FakeTransport().Always(FakeTransport.Response(200, "{\"ok\":true}"));
            var delays = new List<double>();
            var client = new PollinationsClient(transport, (seconds, token) => { delays.Add(seconds); return Task.CompletedTask; });

            PollinationsResult result = client.GetAsync("https://gen.pollinations.ai/text/models").GetAwaiter().GetResult();
            Check.True(result.Ok, "a 200 is ok");
            Check.Equal(PollinationsErrorKind.None, result.Kind, "the kind is none");
            Check.Equal(200, result.StatusCode, "the status is kept");
            Check.Equal(1, result.Attempts, "one attempt");
            Check.Equal("https://gen.pollinations.ai/text/models", result.Url, "the url is kept");
            Check.Contains(result.Text, "\"ok\":true", "the text is kept");
            Check.Equal("Bearer sk_test_key", transport.Requests[0].Headers["Authorization"], "a signed GET carries the key");
            Check.Equal("GET", transport.Requests[0].Method, "the method is GET");

            // an anonymous call sends no Authorization header
            PollinationsConfig.ClearApiKey();
            transport = new FakeTransport().Always(FakeTransport.Response(200, "{}"));
            client = new PollinationsClient(transport, (s, t) => Task.CompletedTask);
            client.PostJsonAnonymousAsync("https://enter.pollinations.ai/api/device/code",
                PollinationsDeviceFlow.BuildCodeRequest("pk_x")).GetAwaiter().GetResult();
            Check.False(transport.Requests[0].Headers.ContainsKey("Authorization"), "the device flow sends no key");
            Check.Equal("application/json", transport.Requests[0].Headers["Content-Type"], "a POST body sets the content type");
            Check.Contains(System.Text.Encoding.UTF8.GetString(transport.Requests[0].Body), "pk_x", "the POST body carries the app key");

            PollinationsConfig.SetApiKey("sk_test_key");

            // 429 is retried with backoff
            delays.Clear();
            transport = new FakeTransport().Queue(
                FakeTransport.Response(429, "{\"error\":\"rate limited\"}"),
                FakeTransport.Response(200, "{}"));
            client = new PollinationsClient(transport, (seconds, token) => { delays.Add(seconds); return Task.CompletedTask; });
            result = client.GetAsync("https://gen.pollinations.ai/text/models").GetAwaiter().GetResult();
            Check.True(result.Ok, "a retry recovers");
            Check.Equal(2, result.Attempts, "two attempts");
            Check.Equal(1, delays.Count, "one delay");
            Check.Equal(0.75, delays[0], "the first backoff is 0.75s");

            // 500 is retried twice by default, then reported
            delays.Clear();
            transport = new FakeTransport().Always(FakeTransport.Response(503, "upstream down"));
            client = new PollinationsClient(transport, (seconds, token) => { delays.Add(seconds); return Task.CompletedTask; });
            result = client.GetAsync("https://gen.pollinations.ai/text/models").GetAwaiter().GetResult();
            Check.False(result.Ok, "a persistent 503 fails");
            Check.Equal(PollinationsErrorKind.Server, result.Kind, "classified as server");
            Check.Equal(3, result.Attempts, "three attempts with max_retries 2");
            Check.Equal(2, delays.Count, "two delays");
            Check.Equal(1.5, delays[1], "the second backoff is 1.5s");
            Check.Contains(result.Error, "upstream down", "the body becomes the error");
            Check.Contains(result.Message, "Pollinations", "a player facing message is available");

            // a dead connection is retried, then reported as network
            transport = new FakeTransport().Always(FakeTransport.Dead());
            client = new PollinationsClient(transport, (s, t) => Task.CompletedTask);
            result = client.GetAsync("https://gen.pollinations.ai/text/models").GetAwaiter().GetResult();
            Check.Equal(PollinationsErrorKind.Network, result.Kind, "a dead connection is network");
            Check.Equal("connection reset", result.Error, "the transport error is kept");
            Check.Equal(3, result.Attempts, "network failures are retried");

            // 402 and 400 and 401 are not retried
            transport = new FakeTransport().Always(FakeTransport.Response(402, "Insufficient balance"));
            client = new PollinationsClient(transport, (s, t) => Task.CompletedTask);
            result = client.GetAsync("https://gen.pollinations.ai/v1/audio/speech").GetAwaiter().GetResult();
            Check.Equal(PollinationsErrorKind.Balance, result.Kind, "402 is balance");
            Check.Equal(1, result.Attempts, "balance failures are not retried");

            transport = new FakeTransport().Always(FakeTransport.Response(400, "{\"error\":\"unknown model\"}"));
            client = new PollinationsClient(transport, (s, t) => Task.CompletedTask);
            result = client.GetAsync("https://gen.pollinations.ai/image/hello").GetAwaiter().GetResult();
            Check.Equal(PollinationsErrorKind.BadRequest, result.Kind, "400 is a bad request");
            Check.Equal(1, result.Attempts, "bad requests are not retried");
            Check.Contains(result.Error, "unknown model", "the API error is kept");

            transport = new FakeTransport().Always(FakeTransport.Response(401, "{\"error\":\"A valid API key is required\"}"));
            client = new PollinationsClient(transport, (s, t) => Task.CompletedTask);
            result = client.GetAsync("https://gen.pollinations.ai/text/hi").GetAwaiter().GetResult();
            Check.Equal(PollinationsErrorKind.Auth, result.Kind, "401 is auth");
            Check.Equal(1, result.Attempts, "auth failures are not retried");

            // a failure keeps the raw bytes for inspection
            transport = new FakeTransport().Always(FakeTransport.Binary(500, new byte[] { 0xff, 0x00, 0x01 }, "application/octet-stream"));
            client = new PollinationsClient(transport, (s, t) => Task.CompletedTask);
            result = client.GetAsync("https://gen.pollinations.ai/image/hi").GetAwaiter().GetResult();
            Check.Equal(3, result.Bytes.Length, "a failure keeps the bytes");
            Check.Equal("", result.Text, "a binary failure body has no text");

            // retry settings are adjustable
            transport = new FakeTransport().Queue(
                FakeTransport.Response(500, ""),
                FakeTransport.Response(200, "{}"));
            client = new PollinationsClient(transport, (s, t) => Task.CompletedTask);
            client.MaxRetries = 0;
            result = client.GetAsync("https://gen.pollinations.ai/text/models").GetAwaiter().GetResult();
            Check.False(result.Ok, "max_retries 0 turns retries off");
            Check.Equal(1, result.Attempts, "no retry with max_retries 0");

            PollinationsConfig.ClearApiKey();

            // a POST sends the key and a JSON body
            PollinationsConfig.SetApiKey("sk_post");
            transport = new FakeTransport().Always(FakeTransport.Response(200, "{\"choices\":[]}"));
            client = new PollinationsClient(transport, (s, t) => Task.CompletedTask);
            client.PostJsonAsync("https://gen.pollinations.ai/v1/chat/completions",
                PollinationsChat.BuildPayload("nova-fast", PollinationsChat.BuildMessages("hi"))).GetAwaiter().GetResult();
            Check.Equal("Bearer sk_post", transport.Requests[0].Headers["Authorization"], "a POST carries the key");
            string body = System.Text.Encoding.UTF8.GetString(transport.Requests[0].Body);
            Check.Contains(body, "nova-fast", "the POST body carries the model");
            Check.Contains(body, "\"role\":\"user\"", "the POST body carries the message");
            PollinationsConfig.ClearApiKey();
        }
    }
}

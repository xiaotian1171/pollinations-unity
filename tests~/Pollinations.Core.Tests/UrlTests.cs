namespace Pollinations.Tests
{
    public static class UrlTests
    {
        public static void Run()
        {
            Check.Suite("urls");

            Check.Equal("https://gen.pollinations.ai/v1/chat/completions", PollinationsUrls.ChatCompletions(), "chat url");
            Check.Equal("https://gen.pollinations.ai/v1/audio/speech", PollinationsUrls.Speech(), "speech url");
            Check.Equal("https://gen.pollinations.ai/text/models", PollinationsUrls.Models("text"), "models url");
            Check.Equal("https://gen.pollinations.ai/image/models", PollinationsUrls.Models("image"), "image models url");
            Check.Equal("https://gen.pollinations.ai/audio/models", PollinationsUrls.Models("audio"), "audio models url");
            Check.Equal("https://gen.pollinations.ai/embeddings/models", PollinationsUrls.Models("embeddings"), "embeddings models url");

            Check.Equal("https://enter.pollinations.ai/api/device/code", PollinationsUrls.DeviceCode(), "device code url");
            Check.Equal("https://enter.pollinations.ai/api/device/token", PollinationsUrls.DeviceToken(), "device token url");
            Check.Equal("https://enter.pollinations.ai/api/device/userinfo", PollinationsUrls.DeviceUserInfo(), "device userinfo url");
            Check.Equal("https://enter.pollinations.ai/device", PollinationsUrls.DeviceVerificationPage(), "verification page url");

            Check.Equal("https://gen.pollinations.ai/text/what%20is%20pollen", PollinationsUrls.TextPrompt("what is pollen"), "encodes the prompt");
            Check.Equal("https://gen.pollinations.ai/text/hi?model=nova-fast", PollinationsUrls.TextPrompt("hi", "nova-fast"), "text model parameter");
            Check.Equal("https://gen.pollinations.ai/text/hi?seed=7", PollinationsUrls.TextPrompt("hi", "", 7), "text seed parameter");
            Check.Equal("https://gen.pollinations.ai/text/hi", PollinationsUrls.TextPrompt("hi", "", -1), "drops a negative seed");
            Check.Contains(PollinationsUrls.TextPrompt("a/b?c=d"), "a%2Fb%3Fc%3Dd", "escapes reserved characters");
            Check.Equal("https://gen.pollinations.ai/text/", PollinationsUrls.TextPrompt(null), "an empty prompt is still a url");

            Check.Equal("https://gen.pollinations.ai/image/a%20mill", PollinationsUrls.ImagePrompt("a mill"), "image url");
            Check.Equal(
                "https://gen.pollinations.ai/image/mill?model=x&width=512&height=256&seed=3&safe=true&quality=hd&transparent=true",
                PollinationsUrls.ImagePrompt("mill", "x", 512, 256, 3, "true", "hd", true),
                "image parameters");
            Check.Equal("https://gen.pollinations.ai/image/mill", PollinationsUrls.ImagePrompt("mill"), "image drops empty parameters");
            Check.Equal("https://gen.pollinations.ai/image/mill?transparent=true", PollinationsUrls.ImagePrompt("mill", "", 0, 0, -1, "", "", true), "transparent only");
            Check.False(PollinationsUrls.ImagePrompt("mill").Contains("width"), "no width when unset");

            Check.Equal("https://enter.pollinations.ai/device", PollinationsUrls.Absolute("/device"), "absolute from a path");
            Check.Equal("https://enter.pollinations.ai/device", PollinationsUrls.Absolute("device"), "absolute from a bare path");
            Check.Equal("https://example.com/x", PollinationsUrls.Absolute("https://example.com/x"), "keeps an absolute url");
            Check.Equal("https://enter.pollinations.ai/device", PollinationsUrls.Absolute(""), "absolute of an empty url");
            Check.Equal("https://enter.pollinations.ai/device", PollinationsUrls.Absolute(null), "absolute of null");
        }
    }
}

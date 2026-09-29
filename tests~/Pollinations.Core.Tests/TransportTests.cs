using System.Collections.Generic;

namespace Pollinations.Tests
{
    public static class TransportTests
    {
        public static void Run()
        {
            Check.Suite("transport");

            var json = new PollinationsResponse { StatusCode = 200, Bytes = System.Text.Encoding.UTF8.GetBytes("{\"a\":1}"), ContentType = "application/json" };
            Check.True(json.IsTextual, "JSON is textual");
            Check.Equal("{\"a\":1}", json.Text, "JSON is readable as text");

            var image = new PollinationsResponse { StatusCode = 200, Bytes = new byte[] { 0xFF, 0xD8, 0xFF }, ContentType = "image/jpeg" };
            Check.False(image.IsTextual, "an image is not textual");
            Check.Equal("", image.Text, "an image never becomes text");
            Check.Equal(3, image.Bytes.Length, "the image bytes are kept");

            var audio = new PollinationsResponse { StatusCode = 200, Bytes = new byte[] { 1, 2 }, ContentType = "audio/mpeg" };
            Check.False(audio.IsTextual, "audio is not textual");
            var octet = new PollinationsResponse { StatusCode = 200, Bytes = new byte[] { 1 }, ContentType = "application/octet-stream" };
            Check.False(octet.IsTextual, "an octet stream is not textual");
            var parameterised = new PollinationsResponse { StatusCode = 200, Bytes = System.Text.Encoding.UTF8.GetBytes("{}"), ContentType = "application/json; charset=utf-8" };
            Check.True(parameterised.IsTextual, "a charset parameter does not change the type");
            var mixed = new PollinationsResponse { StatusCode = 200, Bytes = System.Text.Encoding.UTF8.GetBytes("{}"), ContentType = "IMAGE/PNG" };
            Check.False(mixed.IsTextual, "the content type is compared case insensitively");
            var empty = new PollinationsResponse { StatusCode = 204 };
            Check.True(empty.IsTextual, "a missing content type counts as text");
            Check.Equal("", empty.Text, "an empty body gives no text");

            var request = new PollinationsRequest();
            Check.Equal("GET", request.Method, "requests default to GET");
            Check.Equal("application/json", request.ContentType, "requests default to JSON");
            Check.Equal(120, request.TimeoutSeconds, "the default timeout");
            request.WithHeader("Authorization", "Bearer x");
            Check.Equal("Bearer x", request.Headers["Authorization"], "headers can be chained");
            request.WithHeader("Empty", "");
            Check.False(request.Headers.ContainsKey("Empty"), "empty headers are dropped");

            var result = PollinationsResult.Failure(PollinationsErrorKind.None, 0, "u", "boom");
            Check.Equal(PollinationsErrorKind.Unknown, result.Kind, "a failure is never kind none");
            Check.False(result.Ok, "a failure is not ok");
            Check.Contains(result.Message, "failed", "a failure has a message");
            Check.Null(result.Json(), "a failure without text has no JSON");
            Check.Equal(0, result.Bytes.Length, "a failure without bytes has an empty array");

            var ok = PollinationsResult.Success(
                new PollinationsResponse { StatusCode = 200, Bytes = System.Text.Encoding.UTF8.GetBytes("{\"model\":\"m\"}"), ContentType = "application/json" },
                1, 0.5);
            Check.True(ok.Ok, "a success is ok");
            Check.Equal("m", PollinationsJson.GetString(ok.Json(), "model"), "a success can be read as JSON");
            Check.Equal(0.5, ok.Seconds, "a success keeps its duration");
            Check.Equal("none", ok.KindName, "a success kind name");
        }
    }
}

using System.Collections.Generic;

namespace Pollinations.Tests
{
    public static class JsonTests
    {
        public static void Run()
        {
            Check.Suite("json");

            object parsed;
            Check.True(PollinationsJson.TryParse("{\"a\":1,\"b\":[true,null,\"x\"]}", out parsed), "parses an object");
            var root = PollinationsJson.AsObject(parsed);
            Check.Equal(1.0, PollinationsJson.GetNumber(root, "a", 0), "reads a number");
            var list = PollinationsJson.AsArray(root["b"]);
            Check.Equal(3, list.Count, "reads an array");
            Check.Equal(true, list[0], "reads true");
            Check.Null(list[1], "reads null");
            Check.Equal("x", list[2], "reads a string");

            Check.True(PollinationsJson.TryParse("[1,2,3]", out parsed), "parses a top level array");
            Check.Equal(3, PollinationsJson.AsArray(parsed).Count, "top level array length");

            Check.True(PollinationsJson.TryParse("\"plain\"", out parsed), "parses a bare string");
            Check.Equal("plain", parsed, "bare string value");

            Check.True(PollinationsJson.TryParse("{\"n\":1.5e3,\"m\":-2}", out parsed), "parses exponents");
            Check.Equal(1500.0, PollinationsJson.GetNumber(PollinationsJson.AsObject(parsed), "n", 0), "exponent value");
            Check.Equal(-2.0, PollinationsJson.GetNumber(PollinationsJson.AsObject(parsed), "m", 0), "negative value");

            Check.True(PollinationsJson.TryParse("{\"s\":\"a\\\"b\\\\c\\n\\u00e9\"}", out parsed), "parses escapes");
            Check.Equal("a\"b\\c\né", PollinationsJson.GetString(PollinationsJson.AsObject(parsed), "s"), "escape value");

            Check.False(PollinationsJson.TryParse("{", out parsed), "rejects a truncated object");
            Check.False(PollinationsJson.TryParse("{\"a\":}", out parsed), "rejects a missing value");
            Check.False(PollinationsJson.TryParse("", out parsed), "rejects empty text");
            Check.False(PollinationsJson.TryParse("{\"a\":1} trailing", out parsed), "rejects trailing text");
            Check.False(PollinationsJson.TryParse("nope", out parsed), "rejects a bare word");
            Check.False(PollinationsJson.TryParse("[1,2", out parsed), "rejects an unterminated array");
            Check.False(PollinationsJson.TryParse("\"abc", out parsed), "rejects an unterminated string");
            Check.Throws(() => PollinationsJson.Parse("{"), "Parse throws on bad input");
            Check.Equal(null, PollinationsJson.ParseObject("[1]"), "ParseObject returns null for an array");

            var payload = new Dictionary<string, object>
            {
                { "model", "nova-fast" },
                { "messages", new List<object> { new Dictionary<string, object> { { "role", "user" }, { "content", "hi" } } } },
                { "temperature", 0.8 },
                { "flag", true },
                { "nothing", null },
            };
            string written = PollinationsJson.Write(payload);
            Check.True(PollinationsJson.TryParse(written, out parsed), "writes valid JSON");
            var reread = PollinationsJson.AsObject(parsed);
            Check.Equal("nova-fast", PollinationsJson.GetString(reread, "model"), "round trips a string");
            Check.Equal(0.8, PollinationsJson.GetNumber(reread, "temperature", 0), "round trips a double");
            Check.Equal(true, reread["flag"], "round trips a bool");
            Check.Null(reread["nothing"], "round trips null");
            Check.Equal("hi", PollinationsJson.GetString(PollinationsJson.AsObject(PollinationsJson.AsArray(reread["messages"])[0]), "content"), "round trips a nested object");
            Check.Contains(PollinationsJson.Write("a\"b"), "\\\"", "escapes quotes");

            Check.Equal("", PollinationsJson.GetString(null, "a"), "GetString tolerates null");
            Check.Equal("fallback", PollinationsJson.GetString(new Dictionary<string, object>(), "a", "fallback"), "GetString falls back");
            Check.Equal(7.0, PollinationsJson.GetNumber(null, "a", 7), "GetNumber tolerates null");
            Check.Equal(null, PollinationsJson.AsString(new List<object>()), "AsString ignores non scalars");
        }
    }
}

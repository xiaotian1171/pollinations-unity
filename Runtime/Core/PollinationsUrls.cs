using System;
using System.Collections.Generic;
using System.Text;

namespace Pollinations
{
    /// <summary>Every URL the package talks to, built in one place.</summary>
    public static class PollinationsUrls
    {
        public const string GenerationBase = "https://gen.pollinations.ai";
        public const string EnterBase = "https://enter.pollinations.ai";

        public static string ChatCompletions()
        {
            return GenerationBase + "/v1/chat/completions";
        }

        public static string Speech()
        {
            return GenerationBase + "/v1/audio/speech";
        }

        public static string Models(string modality)
        {
            return GenerationBase + "/" + modality + "/models";
        }

        public static string DeviceCode()
        {
            return EnterBase + "/api/device/code";
        }

        public static string DeviceToken()
        {
            return EnterBase + "/api/device/token";
        }

        public static string DeviceUserInfo()
        {
            return EnterBase + "/api/device/userinfo";
        }

        /// <summary>The page a player opens to approve a sign-in code.</summary>
        public static string DeviceVerificationPage()
        {
            return EnterBase + "/device";
        }

        /// <summary>GET /text/{prompt} - the cheap single-prompt route.</summary>
        public static string TextPrompt(string prompt, string model = "", double seed = -1)
        {
            return GenerationBase + "/text/" + Uri.EscapeDataString(prompt ?? string.Empty)
                + Query(new Dictionary<string, string>
                {
                    { "model", model },
                    { "seed", Seed(seed) },
                });
        }

        /// <summary>GET /image/{prompt} - the image route.</summary>
        public static string ImagePrompt(
            string prompt,
            string model = "",
            int width = 0,
            int height = 0,
            double seed = -1,
            string safe = "",
            string quality = "",
            bool transparent = false,
            string enhance = "")
        {
            return GenerationBase + "/image/" + Uri.EscapeDataString(prompt ?? string.Empty)
                + Query(new Dictionary<string, string>
                {
                    { "model", model },
                    { "width", width > 0 ? width.ToString(System.Globalization.CultureInfo.InvariantCulture) : "" },
                    { "height", height > 0 ? height.ToString(System.Globalization.CultureInfo.InvariantCulture) : "" },
                    { "seed", Seed(seed) },
                    { "safe", safe },
                    { "quality", quality },
                    { "transparent", transparent ? "true" : "" },
                    { "enhance", enhance },
                });
        }

        /// <summary>The absolute form of a verification URL the API returned, which is usually relative.</summary>
        public static string Absolute(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return DeviceVerificationPage();
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (!url.StartsWith("/", StringComparison.Ordinal))
            {
                url = "/" + url;
            }

            return EnterBase + url;
        }

        public static string BuildQuery(IDictionary<string, string> parameters)
        {
            return Query(parameters);
        }

        private static string Query(IDictionary<string, string> parameters)
        {
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in parameters)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                builder.Append(builder.Length == 0 ? '?' : '&');
                builder.Append(Uri.EscapeDataString(pair.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(pair.Value));
            }

            return builder.ToString();
        }

        private static string Seed(double seed)
        {
            if (seed < 0)
            {
                return "";
            }

            return seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

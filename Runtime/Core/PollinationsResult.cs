using System.Collections.Generic;

namespace Pollinations
{
    /// <summary>
    /// The outcome of one call. Everything a game needs to react is here: the
    /// payload, the classification, the status code, the raw bytes and the
    /// request that produced it.
    /// </summary>
    public sealed class PollinationsResult
    {
        public bool Ok { get; internal set; }
        public PollinationsErrorKind Kind { get; internal set; }
        public string KindName { get { return PollinationsErrors.KindName(Kind); } }
        public int StatusCode { get; internal set; }
        public string Url { get; internal set; }
        public string Text { get; internal set; }
        public byte[] Bytes { get; internal set; }
        public string ContentType { get; internal set; }
        public int Attempts { get; internal set; }
        public double Seconds { get; internal set; }
        public string Error { get; internal set; }
        public string Message { get { return PollinationsErrors.Message(Kind); } }
        public Dictionary<string, string> Headers { get; internal set; }

        public PollinationsResult()
        {
            Url = "";
            Text = "";
            Bytes = new byte[0];
            ContentType = "";
            Error = "";
            Headers = new Dictionary<string, string>();
        }

        /// <summary>The body parsed as a JSON object, or null when it is not one.</summary>
        public Dictionary<string, object> Json()
        {
            object parsed;
            if (!PollinationsJson.TryParse(Text ?? "", out parsed))
            {
                return null;
            }

            return parsed as Dictionary<string, object>;
        }

        internal static PollinationsResult Success(PollinationsResponse response, int attempts, double seconds)
        {
            return new PollinationsResult
            {
                Ok = true,
                Kind = PollinationsErrorKind.None,
                StatusCode = response.StatusCode,
                Url = response.Url,
                Text = response.Text ?? "",
                Bytes = response.Bytes ?? new byte[0],
                ContentType = response.ContentType ?? "",
                Attempts = attempts,
                Seconds = seconds,
                Headers = response.Headers ?? new Dictionary<string, string>(),
            };
        }

        internal static PollinationsResult Failure(
            PollinationsErrorKind kind,
            int statusCode,
            string url,
            string error,
            int attempts = 1,
            double seconds = 0.0,
            string text = "",
            byte[] bytes = null,
            Dictionary<string, string> headers = null)
        {
            if (kind == PollinationsErrorKind.None)
            {
                kind = PollinationsErrorKind.Unknown;
            }

            return new PollinationsResult
            {
                Ok = false,
                Kind = kind,
                StatusCode = statusCode,
                Url = url ?? "",
                Text = text ?? "",
                Bytes = bytes ?? new byte[0],
                Attempts = attempts,
                Seconds = seconds,
                Error = error ?? "",
                Headers = headers ?? new Dictionary<string, string>(),
            };
        }
    }
}

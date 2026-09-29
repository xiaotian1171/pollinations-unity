using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pollinations
{
    /// <summary>One HTTP request, independent of any engine type.</summary>
    public sealed class PollinationsRequest
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; private set; }
        public byte[] Body { get; set; }
        public string ContentType { get; set; }
        public int TimeoutSeconds { get; set; }

        public PollinationsRequest()
        {
            Method = "GET";
            Url = "";
            Headers = new Dictionary<string, string>();
            Body = null;
            ContentType = "application/json";
            TimeoutSeconds = 120;
        }

        public PollinationsRequest WithHeader(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Headers[name] = value;
            }

            return this;
        }
    }

    /// <summary>One HTTP response, with the body kept as bytes.</summary>
    public sealed class PollinationsResponse
    {
        public int StatusCode { get; set; }
        public byte[] Bytes { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool TransportFailed { get; set; }
        public string Error { get; set; }
        public string Url { get; set; }

        public PollinationsResponse()
        {
            Bytes = new byte[0];
            ContentType = "";
            Headers = new Dictionary<string, string>();
            Error = "";
            Url = "";
        }

        /// <summary>True when the body should be read as text. Images and audio are binary and stay in <see cref="Bytes"/>.</summary>
        public bool IsTextual
        {
            get
            {
                if (string.IsNullOrEmpty(ContentType))
                {
                    return true;
                }

                string lowered = ContentType.ToLowerInvariant();
                if (lowered.StartsWith("image/") || lowered.StartsWith("audio/") || lowered.StartsWith("video/"))
                {
                    return false;
                }

                return !lowered.StartsWith("application/octet-stream");
            }
        }

        /// <summary>The body as text, or an empty string when it is binary.</summary>
        public string Text
        {
            get
            {
                if (!IsTextual || Bytes == null || Bytes.Length == 0)
                {
                    return "";
                }

                try
                {
                    return new System.Text.UTF8Encoding(false, false).GetString(Bytes);
                }
                catch (System.ArgumentException)
                {
                    return "";
                }
            }
        }
    }

    /// <summary>The seam that makes the whole package testable: Unity uses UnityWebRequest, tests use a scripted transport.</summary>
    public interface IPollinationsTransport
    {
        Task<PollinationsResponse> SendAsync(PollinationsRequest request, CancellationToken cancellationToken);
    }
}

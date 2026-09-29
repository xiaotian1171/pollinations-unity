using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pollinations.Tests
{
    /// <summary>
    /// A scripted transport. It replays queued answers (the last one repeats) and
    /// records every request, which is how the retry policy, the headers and the
    /// binary handling are tested without a network.
    /// </summary>
    public sealed class FakeTransport : IPollinationsTransport
    {
        private readonly List<PollinationsResponse> _responses = new List<PollinationsResponse>();
        public readonly List<PollinationsRequest> Requests = new List<PollinationsRequest>();
        public readonly List<double> Delays = new List<double>();

        public static PollinationsResponse Response(int statusCode, string body, string contentType = "application/json")
        {
            return new PollinationsResponse
            {
                StatusCode = statusCode,
                Bytes = System.Text.Encoding.UTF8.GetBytes(body ?? ""),
                ContentType = contentType,
            };
        }

        public static PollinationsResponse Binary(int statusCode, byte[] bytes, string contentType)
        {
            return new PollinationsResponse
            {
                StatusCode = statusCode,
                Bytes = bytes,
                ContentType = contentType,
            };
        }

        public static PollinationsResponse Dead(string error = "connection reset")
        {
            return new PollinationsResponse
            {
                TransportFailed = true,
                Error = error,
            };
        }

        public FakeTransport Queue(params PollinationsResponse[] responses)
        {
            _responses.AddRange(responses);
            return this;
        }

        public FakeTransport Always(PollinationsResponse response)
        {
            _responses.Clear();
            _responses.Add(response);
            return this;
        }

        public Task DelayAsync(double seconds, CancellationToken cancellationToken)
        {
            Delays.Add(seconds);
            return Task.CompletedTask;
        }

        public PollinationsRequest LastRequest
        {
            get { return Requests.Count == 0 ? null : Requests[Requests.Count - 1]; }
        }

        public Task<PollinationsResponse> SendAsync(PollinationsRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (_responses.Count == 0)
            {
                return Task.FromResult(Response(200, "{}"));
            }

            int index = Requests.Count - 1;
            if (index >= _responses.Count)
            {
                index = _responses.Count - 1;
            }

            PollinationsResponse response = _responses[index];
            response.Url = request.Url;
            return Task.FromResult(response);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Pollinations
{
    /// <summary>
    /// The one place that talks to the API. It classifies every response, retries
    /// what is worth retrying with exponential backoff, and keeps binary bodies
    /// as bytes.
    /// </summary>
    public sealed class PollinationsClient
    {
        private readonly IPollinationsTransport _transport;
        private readonly Func<double, CancellationToken, Task> _delay;
        private readonly Func<string> _apiKey;

        /// <summary>Extra attempts after the first one.</summary>
        public int MaxRetries { get; set; }

        public double RetryBaseSeconds { get; set; }

        public double RetryCapSeconds { get; set; }

        public string LastLog { get; private set; }

        public PollinationsClient(
            IPollinationsTransport transport,
            Func<double, CancellationToken, Task> delay = null,
            Func<string> apiKey = null)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            _transport = transport;
            _delay = delay ?? DefaultDelay;
            _apiKey = apiKey ?? (() => PollinationsConfig.ApiKey);
            MaxRetries = 2;
            RetryBaseSeconds = 0.75;
            RetryCapSeconds = 8.0;
            LastLog = "";
        }

        public Task<PollinationsResult> GetAsync(string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SendAsync(new PollinationsRequest { Method = "GET", Url = url }, true, cancellationToken);
        }

        /// <summary>GET with an Authorization header when a key is configured.</summary>
        public Task<PollinationsResult> GetSignedAsync(string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SendAsync(new PollinationsRequest { Method = "GET", Url = url }, true, cancellationToken);
        }

        public Task<PollinationsResult> PostJsonAsync(string url, Dictionary<string, object> payload, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new PollinationsRequest
            {
                Method = "POST",
                Url = url,
                Body = System.Text.Encoding.UTF8.GetBytes(PollinationsJson.Write(payload)),
                ContentType = "application/json",
            };
            return SendAsync(request, true, cancellationToken);
        }

        /// <summary>POST without a key: the device flow endpoints are unauthenticated.</summary>
        public Task<PollinationsResult> PostJsonAnonymousAsync(string url, Dictionary<string, object> payload, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new PollinationsRequest
            {
                Method = "POST",
                Url = url,
                Body = System.Text.Encoding.UTF8.GetBytes(PollinationsJson.Write(payload)),
                ContentType = "application/json",
            };
            return SendAsync(request, false, cancellationToken);
        }

        /// <summary>Runs a request, retrying rate limits, server failures and network failures.</summary>
        public async Task<PollinationsResult> SendAsync(PollinationsRequest request, bool signed, CancellationToken cancellationToken = default(CancellationToken))
        {
            Prepare(request, signed);
            int attempts = 0;
            var stopwatch = Stopwatch.StartNew();
            double waited = 0.0;

            while (true)
            {
                attempts++;
                PollinationsResponse response = null;
                try
                {
                    response = await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    response = new PollinationsResponse
                    {
                        Url = request.Url,
                        TransportFailed = true,
                        Error = exception.Message,
                    };
                }

                if (response == null)
                {
                    response = new PollinationsResponse { Url = request.Url, TransportFailed = true, Error = "empty response" };
                }

                response.Url = string.IsNullOrEmpty(response.Url) ? request.Url : response.Url;
                string text = response.Text;
                PollinationsErrorKind kind = PollinationsErrors.Classify(response.StatusCode, text, response.TransportFailed);

                if (kind == PollinationsErrorKind.None)
                {
                    stopwatch.Stop();
                    return PollinationsResult.Success(response, attempts, stopwatch.Elapsed.TotalSeconds);
                }

                bool canRetry = attempts <= MaxRetries && PollinationsErrors.Retryable(kind) && !cancellationToken.IsCancellationRequested;
                if (canRetry)
                {
                    double seconds = PollinationsErrors.BackoffSeconds(attempts - 1, RetryBaseSeconds, RetryCapSeconds);
                    waited += seconds;
                    LastLog = PollinationsErrors.Describe(kind, response.StatusCode, "retrying in " + seconds.ToString("0.##") + "s");
                    await _delay(seconds, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                stopwatch.Stop();
                string error = response.TransportFailed
                    ? (string.IsNullOrEmpty(response.Error) ? "the request could not be completed" : response.Error)
                    : FirstLine(text);
                LastLog = PollinationsErrors.Describe(kind, response.StatusCode, error);

                return PollinationsResult.Failure(
                    kind,
                    response.StatusCode,
                    response.Url,
                    error,
                    attempts,
                    stopwatch.Elapsed.TotalSeconds + waited,
                    text,
                    response.Bytes,
                    response.Headers);
            }
        }

        private void Prepare(PollinationsRequest request, bool signed)
        {
            if (request.Headers == null)
            {
                return;
            }

            if (signed && _apiKey != null)
            {
                string key = _apiKey();
                if (!string.IsNullOrEmpty(key))
                {
                    request.Headers["Authorization"] = "Bearer " + key;
                }
            }

            if (request.Body != null && request.Body.Length > 0 && !string.IsNullOrEmpty(request.ContentType))
            {
                request.Headers["Content-Type"] = request.ContentType;
            }
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            string trimmed = text.Trim();
            int newline = trimmed.IndexOf('\n');
            string line = newline < 0 ? trimmed : trimmed.Substring(0, newline);
            return line.Length > 300 ? line.Substring(0, 300) : line;
        }

        private static Task DefaultDelay(double seconds, CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Pollinations.Tests
{
    /// <summary>
    /// A real transport for the live check. The Unity package ships a
    /// UnityWebRequest one instead; this exists so the same core can be exercised
    /// against the real API from a console.
    /// </summary>
    public sealed class HttpClientTransport : IPollinationsTransport
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };

        public async Task<PollinationsResponse> SendAsync(PollinationsRequest request, CancellationToken cancellationToken)
        {
            try
            {
                using (var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Url))
                {
                    foreach (KeyValuePair<string, string> header in request.Headers)
                    {
                        if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            if (message.Content == null)
                            {
                                message.Content = new ByteArrayContent(new byte[0]);
                            }

                            message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }

                    if (request.Body != null && request.Body.Length > 0)
                    {
                        message.Content = new ByteArrayContent(request.Body);
                        message.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
                    }

                    using (HttpResponseMessage response = await Client.SendAsync(message, cancellationToken).ConfigureAwait(false))
                    {
                        byte[] bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        var result = new PollinationsResponse
                        {
                            StatusCode = (int)response.StatusCode,
                            Bytes = bytes,
                            Url = request.Url,
                            ContentType = response.Content.Headers.ContentType == null
                                ? ""
                                : response.Content.Headers.ContentType.ToString(),
                        };

                        foreach (var header in response.Headers)
                        {
                            result.Headers[header.Key] = string.Join(",", header.Value);
                        }

                        return result;
                    }
                }
            }
            catch (Exception exception)
            {
                return new PollinationsResponse
                {
                    TransportFailed = true,
                    Error = exception.Message,
                    Url = request.Url,
                };
            }
        }
    }
}

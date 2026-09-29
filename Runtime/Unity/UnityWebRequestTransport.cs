using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Pollinations
{
    /// <summary>
    /// The real transport: UnityWebRequest on a coroutine, wrapped in a Task so the
    /// core can await it. Nothing here interprets the payload - that is the core's job.
    /// </summary>
    public sealed class UnityWebRequestTransport : IPollinationsTransport
    {
        private readonly MonoBehaviour _runner;

        /// <summary>The runner only exists so requests are owned by a live object and stop with it.</summary>
        public UnityWebRequestTransport(MonoBehaviour runner)
        {
            _runner = runner;
        }

        public async Task<PollinationsResponse> SendAsync(PollinationsRequest request, CancellationToken cancellationToken)
        {
            using (var webRequest = new UnityWebRequest(request.Url, request.Method))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = request.TimeoutSeconds;

                if (request.Body != null && request.Body.Length > 0)
                {
                    webRequest.uploadHandler = new UploadHandlerRaw(request.Body);
                    webRequest.uploadHandler.contentType = string.IsNullOrEmpty(request.ContentType)
                        ? "application/json"
                        : request.ContentType;
                }

                if (request.Headers != null)
                {
                    foreach (KeyValuePair<string, string> header in request.Headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }

                UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();
                await WaitFor(operation);

                var response = new PollinationsResponse
                {
                    StatusCode = (int)webRequest.responseCode,
                    Url = request.Url,
                };

                byte[] data = webRequest.downloadHandler == null ? null : webRequest.downloadHandler.data;
                response.Bytes = data ?? new byte[0];
                response.ContentType = webRequest.GetResponseHeader("Content-Type") ?? "";

                Dictionary<string, string> headers = webRequest.GetResponseHeaders();
                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        response.Headers[header.Key] = header.Value;
                    }
                }

                // A protocol error still carries a body, and the body is what gets classified.
                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    response.TransportFailed = true;
                    response.Error = webRequest.error ?? "the request could not be completed";
                }
                else if (response.StatusCode == 0 && !string.IsNullOrEmpty(webRequest.error))
                {
                    response.TransportFailed = true;
                    response.Error = webRequest.error;
                }

                return response;
            }
        }

        /// <summary>
        /// Fetches audio the way Unity can decode it. WAV arrives as bytes and is
        /// decoded by the package; MP3 and OGG are streamed from a file URL, which
        /// is what Unity supports.
        /// </summary>
        public static async Task<AudioClip> LoadAudioClip(string url, AudioType type)
        {
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                if (request.downloadHandler is DownloadHandlerAudioClip)
                {
                    (request.downloadHandler as DownloadHandlerAudioClip).streamAudio = false;
                }

                await WaitFor(request.SendWebRequest());
                if (request.result != UnityWebRequest.Result.Success)
                {
                    return null;
                }

                return DownloadHandlerAudioClip.GetContent(request);
            }
        }

        private static Task WaitFor(AsyncOperation operation)
        {
            var completion = new TaskCompletionSource<bool>();
            operation.completed += _ => completion.TrySetResult(true);
            return completion.Task;
        }
    }
}

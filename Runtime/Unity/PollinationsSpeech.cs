using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Pollinations
{
    /// <summary>
    /// Speech, music and sound effects. Unity cannot play audio from raw bytes, so
    /// a WAV answer is decoded by the package and handed to AudioClip.Create, while
    /// MP3 and OGG go through Unity's own audio loader from a temporary file.
    /// </summary>
    [AddComponentMenu("Pollinations/Pollinations Speech")]
    public class PollinationsSpeech : PollinationsBehaviour
    {
        [Serializable]
        public sealed class ClipEvent : UnityEvent<AudioClip>
        {
        }

        [Header("Speech")]
        [Tooltip("Any speech, music or sound effect model from the live audio catalogue.")]
        [SerializeField] private string model = PollinationsConfig.DefaultSpeechModel;

        [SerializeField] private string voice = PollinationsConfig.DefaultVoice;

        [Tooltip("wav decodes inside the package; mp3 and ogg go through Unity's audio loader.")]
        [SerializeField] private string responseFormat = "wav";

        [Tooltip("Delivery notes for the model, e.g. \"speak slowly, warmly\".")]
        [TextArea(2, 4)]
        [SerializeField] private string instructions = "";

        [Tooltip("Called with the clip, ready for an AudioSource.")]
        public ClipEvent onGenerated = new ClipEvent();

        public event Action<PollinationsResult> Generated;

        public string Model
        {
            get { return model; }
            set { model = value; }
        }

        public string Voice
        {
            get { return voice; }
            set { voice = value; }
        }

        public string ResponseFormat
        {
            get { return responseFormat; }
            set { responseFormat = value; }
        }

        public AudioClip Clip { get; private set; }

        public async Task<PollinationsResult> GenerateAsync(string text)
        {
            var payload = new Dictionary<string, object>
            {
                { "model", model },
                { "input", text ?? "" },
                { "voice", voice },
                { "response_format", responseFormat },
            };

            if (!string.IsNullOrEmpty(instructions))
            {
                payload["instructions"] = instructions;
            }

            PollinationsResult result = await Client.PostJsonAsync(PollinationsUrls.Speech(), payload);
            AudioClip clip = null;

            if (result.Ok)
            {
                clip = await ToClip(result, text);
                if (clip == null)
                {
                    result = PollinationsResult.Failure(
                        PollinationsErrorKind.Parse,
                        result.StatusCode,
                        result.Url,
                        "the audio payload could not be decoded (" + result.ContentType + ", " + result.Bytes.Length + " bytes)");
                }
                else
                {
                    if (Clip != null)
                    {
                        Destroy(Clip);
                    }

                    Clip = clip;
                }
            }

            Report(result);
            if (result.Ok)
            {
                onGenerated.Invoke(Clip);
                if (Generated != null)
                {
                    Generated(result);
                }
            }

            return result;
        }

        private async Task<AudioClip> ToClip(PollinationsResult result, string text)
        {
            if (PollinationsWav.LooksLikeWav(result.Bytes))
            {
                return FromWav(PollinationsWav.Decode(result.Bytes), string.IsNullOrEmpty(text) ? "pollinations" : text);
            }

            AudioType type = AudioTypeFrom(result.ContentType, responseFormat);
            if (type == AudioType.UNKNOWN)
            {
                return null;
            }

            // Unity decodes MP3 and OGG from a URL, so the bytes are parked in the cache
            string extension = type == AudioType.MPEG ? ".mp3" : (type == AudioType.OGGVORBIS ? ".ogg" : ".wav");
            string path = Path.Combine(Application.temporaryCachePath, "pollinations" + extension);
            File.WriteAllBytes(path, result.Bytes);
            return await UnityWebRequestTransport.LoadAudioClip("file://" + path, type);
        }

        /// <summary>Turns decoded PCM into a playable clip.</summary>
        public static AudioClip FromWav(PollinationsAudio audio, string name)
        {
            if (audio == null || audio.Samples == null || audio.Samples.Length == 0)
            {
                return null;
            }

            AudioClip clip = AudioClip.Create(name, audio.Samples.Length / Mathf.Max(1, audio.Channels), audio.Channels, audio.SampleRate, false);
            if (!clip.SetData(audio.Samples, 0))
            {
                return null;
            }

            return clip;
        }

        /// <summary>Maps a content type or a format name onto Unity's AudioType.</summary>
        public static AudioType AudioTypeFrom(string contentType, string format)
        {
            string lowered = ((contentType ?? "") + " " + (format ?? "")).ToLowerInvariant();
            if (lowered.Contains("mpeg") || lowered.Contains("mp3"))
            {
                return AudioType.MPEG;
            }

            if (lowered.Contains("ogg") || lowered.Contains("opus"))
            {
                return AudioType.OGGVORBIS;
            }

            if (lowered.Contains("wav") || lowered.Contains("pcm"))
            {
                return AudioType.WAV;
            }

            return AudioType.UNKNOWN;
        }
    }
}

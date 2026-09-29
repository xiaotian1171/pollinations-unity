using System;

namespace Pollinations
{
    /// <summary>Decoded PCM audio, ready for AudioClip.Create.</summary>
    public sealed class PollinationsAudio
    {
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public float[] Samples { get; set; }

        public double Seconds
        {
            get { return Channels <= 0 || SampleRate <= 0 ? 0.0 : (double)Samples.Length / Channels / SampleRate; }
        }
    }

    /// <summary>
    /// A RIFF/WAVE reader. Unity cannot decode audio bytes it was not handed as a
    /// URL, so a WAV answer is decoded here and turned into an AudioClip with
    /// AudioClip.Create; MP3 and OGG go through the player instead.
    /// </summary>
    public static class PollinationsWav
    {
        public static bool LooksLikeWav(byte[] bytes)
        {
            return bytes != null
                && bytes.Length >= 12
                && Tag(bytes, 0) == "RIFF"
                && Tag(bytes, 8) == "WAVE";
        }

        public static PollinationsAudio Decode(byte[] bytes)
        {
            if (!LooksLikeWav(bytes))
            {
                return null;
            }

            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            int format = 0;
            int offset = 12;

            while (offset + 8 <= bytes.Length)
            {
                string chunkId = Tag(bytes, offset);
                int chunkSize = BitConverter.ToInt32(bytes, offset + 4);
                int body = offset + 8;
                if (chunkSize < 0 || body + chunkSize > bytes.Length)
                {
                    // A truncated final chunk: use what is there.
                    chunkSize = bytes.Length - body;
                }

                if (chunkId == "fmt ")
                {
                    if (chunkSize < 16)
                    {
                        return null;
                    }

                    format = BitConverter.ToInt16(bytes, body);
                    channels = BitConverter.ToInt16(bytes, body + 2);
                    sampleRate = BitConverter.ToInt32(bytes, body + 4);
                    bitsPerSample = BitConverter.ToInt16(bytes, body + 14);
                    if (format == unchecked((short)0xFFFE) && chunkSize >= 40)
                    {
                        // WAVE_FORMAT_EXTENSIBLE: the real format sits in the sub-format GUID.
                        format = BitConverter.ToInt16(bytes, body + 24);
                    }
                }
                else if (chunkId == "data")
                {
                    return Convert(bytes, body, chunkSize, format, channels, sampleRate, bitsPerSample);
                }

                offset = body + chunkSize + (chunkSize % 2);
            }

            // No data chunk: a bare fmt + data layout still ends up here, so try the tail.
            return null;
        }

        private static PollinationsAudio Convert(byte[] bytes, int start, int length, int format, int channels, int sampleRate, int bitsPerSample)
        {
            if (channels <= 0 || sampleRate <= 0 || bitsPerSample <= 0)
            {
                return null;
            }

            int bytesPerSample = bitsPerSample / 8;
            if (bytesPerSample <= 0)
            {
                return null;
            }

            int count = length / bytesPerSample;
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                int at = start + (i * bytesPerSample);
                switch (format)
                {
                    case 1: // integer PCM
                        if (bitsPerSample == 8)
                        {
                            samples[i] = (bytes[at] - 128) / 128f;
                        }
                        else if (bitsPerSample == 16)
                        {
                            samples[i] = BitConverter.ToInt16(bytes, at) / 32768f;
                        }
                        else if (bitsPerSample == 24)
                        {
                            int value = bytes[at] | (bytes[at + 1] << 8) | (bytes[at + 2] << 16);
                            if ((value & 0x800000) != 0)
                            {
                                value |= unchecked((int)0xFF000000);
                            }

                            samples[i] = value / 8388608f;
                        }
                        else if (bitsPerSample == 32)
                        {
                            samples[i] = BitConverter.ToInt32(bytes, at) / 2147483648f;
                        }
                        else
                        {
                            return null;
                        }

                        break;
                    case 3: // IEEE float
                        if (bitsPerSample != 32)
                        {
                            return null;
                        }

                        samples[i] = BitConverter.ToSingle(bytes, at);
                        break;
                    default:
                        return null;
                }
            }

            return new PollinationsAudio
            {
                Channels = channels,
                SampleRate = sampleRate,
                Samples = samples,
            };
        }

        /// <summary>Builds a WAV file around PCM samples; useful for tests and for caching.</summary>
        public static byte[] Encode(float[] samples, int channels, int sampleRate)
        {
            if (samples == null)
            {
                samples = new float[0];
            }

            const int headerSize = 44;
            int dataSize = samples.Length * 2;
            var bytes = new byte[headerSize + dataSize];

            WriteTag(bytes, 0, "RIFF");
            BitConverter.GetBytes(36 + dataSize).CopyTo(bytes, 4);
            WriteTag(bytes, 8, "WAVE");
            WriteTag(bytes, 12, "fmt ");
            BitConverter.GetBytes(16).CopyTo(bytes, 16);
            BitConverter.GetBytes((short)1).CopyTo(bytes, 20);
            BitConverter.GetBytes((short)channels).CopyTo(bytes, 22);
            BitConverter.GetBytes(sampleRate).CopyTo(bytes, 24);
            BitConverter.GetBytes(sampleRate * channels * 2).CopyTo(bytes, 28);
            BitConverter.GetBytes((short)(channels * 2)).CopyTo(bytes, 32);
            BitConverter.GetBytes((short)16).CopyTo(bytes, 34);
            WriteTag(bytes, 36, "data");
            BitConverter.GetBytes(dataSize).CopyTo(bytes, 40);

            for (int i = 0; i < samples.Length; i++)
            {
                float clamped = samples[i] < -1f ? -1f : (samples[i] > 1f ? 1f : samples[i]);
                BitConverter.GetBytes((short)Math.Round(clamped * 32767f)).CopyTo(bytes, headerSize + (i * 2));
            }

            return bytes;
        }

        private static string Tag(byte[] bytes, int offset)
        {
            if (bytes == null || offset + 4 > bytes.Length)
            {
                return "";
            }

            return new string(new[] { (char)bytes[offset], (char)bytes[offset + 1], (char)bytes[offset + 2], (char)bytes[offset + 3] });
        }

        private static void WriteTag(byte[] bytes, int offset, string tag)
        {
            for (int i = 0; i < 4; i++)
            {
                bytes[offset + i] = (byte)tag[i];
            }
        }
    }
}

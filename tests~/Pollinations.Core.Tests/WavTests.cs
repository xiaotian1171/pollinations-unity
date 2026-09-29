namespace Pollinations.Tests
{
    public static class WavTests
    {
        public static void Run()
        {
            Check.Suite("wav");

            float[] samples = new float[800];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = (float)System.Math.Sin(i * 0.05);
            }

            byte[] wav = PollinationsWav.Encode(samples, 2, 44100);
            Check.True(PollinationsWav.LooksLikeWav(wav), "the encoded file is a wav");
            Check.Equal("RIFF", new string(new[] { (char)wav[0], (char)wav[1], (char)wav[2], (char)wav[3] }), "the header starts with RIFF");
            Check.Equal(wav.Length, System.BitConverter.ToInt32(wav, 4) + 8, "the RIFF size covers the file");

            PollinationsAudio audio = PollinationsWav.Decode(wav);
            Check.NotNull(audio, "decodes the encoded file");
            Check.Equal(2, audio.Channels, "reads the channel count");
            Check.Equal(44100, audio.SampleRate, "reads the sample rate");
            Check.Equal(800, audio.Samples.Length, "reads every sample");
            Check.True(System.Math.Abs(audio.Samples[25] - samples[25]) < 0.001, "the waveform survives the round trip");
            Check.True(audio.Seconds > 0.009 && audio.Seconds < 0.010, "computes the duration");

            Check.False(PollinationsWav.LooksLikeWav(new byte[] { 1, 2, 3 }), "a short buffer is not a wav");
            Check.False(PollinationsWav.LooksLikeWav(null), "null is not a wav");
            Check.False(PollinationsWav.LooksLikeWav(System.Text.Encoding.UTF8.GetBytes("ID3 mp3 bytes here")), "an mp3 is not a wav");
            Check.Null(PollinationsWav.Decode(System.Text.Encoding.UTF8.GetBytes("not a wav at all")), "garbage does not decode");

            // 8 bit mono
            byte[] eight = Mono(1, 8000, 8, new byte[] { 0, 128, 255 });
            PollinationsAudio decoded = PollinationsWav.Decode(eight);
            Check.Equal(1, decoded.Channels, "reads 8 bit channels");
            Check.Equal(-1.0, (double)decoded.Samples[0], "8 bit silence mapping");
            Check.Equal(0.0, (double)decoded.Samples[1], "8 bit midpoint");
            Check.True(decoded.Samples[2] > 0.99, "8 bit maximum");

            // 24 bit
            byte[] twentyFour = Mono(1, 8000, 24, new byte[] { 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x7F });
            decoded = PollinationsWav.Decode(twentyFour);
            Check.Equal(-1.0, (double)decoded.Samples[0], "24 bit minimum");
            Check.True(decoded.Samples[1] > 0.999, "24 bit maximum");

            // 32 bit float
            byte[] floatBytes = new byte[8];
            System.BitConverter.GetBytes(-0.5f).CopyTo(floatBytes, 0);
            System.BitConverter.GetBytes(0.25f).CopyTo(floatBytes, 4);
            byte[] floatWav = Mono(1, 16000, 32, floatBytes, 3);
            decoded = PollinationsWav.Decode(floatWav);
            Check.Equal(-0.5, (double)decoded.Samples[0], "reads float samples");
            Check.Equal(0.25, (double)decoded.Samples[1], "reads a second float sample");

            // an unknown format is refused rather than guessed
            Check.Null(PollinationsWav.Decode(Mono(1, 8000, 12, new byte[] { 0, 0, 0 })), "an odd bit depth is refused");
            Check.Null(PollinationsWav.Decode(Mono(0, 8000, 16, new byte[] { 0, 0 })), "zero channels is refused");
            Check.Null(PollinationsWav.Decode(Mono(1, 0, 16, new byte[] { 0, 0 })), "zero sample rate is refused");

            // a truncated final chunk still decodes what is there
            byte[] big = PollinationsWav.Encode(new float[100], 1, 8000);
            byte[] truncated = new byte[big.Length - 20];
            System.Array.Copy(big, truncated, truncated.Length);
            decoded = PollinationsWav.Decode(truncated);
            Check.NotNull(decoded, "a truncated wav still decodes");
            Check.Equal(90, decoded.Samples.Length, "keeps the samples that arrived");

            PollinationsAudio empty = PollinationsWav.Decode(PollinationsWav.Encode(new float[0], 1, 8000));
            Check.Equal(0, empty.Samples.Length, "an empty payload decodes to no samples");
            Check.Equal(0.0, empty.Seconds, "an empty payload has no duration");
        }

        /// <summary>Builds a minimal WAV with the given PCM bytes.</summary>
        private static byte[] Mono(int channels, int sampleRate, int bits, byte[] data, int format = 1)
        {
            var bytes = new byte[44 + data.Length];
            Write(bytes, 0, "RIFF");
            System.BitConverter.GetBytes(36 + data.Length).CopyTo(bytes, 4);
            Write(bytes, 8, "WAVE");
            Write(bytes, 12, "fmt ");
            System.BitConverter.GetBytes(16).CopyTo(bytes, 16);
            System.BitConverter.GetBytes((short)format).CopyTo(bytes, 20);
            System.BitConverter.GetBytes((short)channels).CopyTo(bytes, 22);
            System.BitConverter.GetBytes(sampleRate).CopyTo(bytes, 24);
            System.BitConverter.GetBytes(sampleRate * channels * bits / 8).CopyTo(bytes, 28);
            System.BitConverter.GetBytes((short)(channels * bits / 8)).CopyTo(bytes, 32);
            System.BitConverter.GetBytes((short)bits).CopyTo(bytes, 34);
            Write(bytes, 36, "data");
            System.BitConverter.GetBytes(data.Length).CopyTo(bytes, 40);
            data.CopyTo(bytes, 44);
            return bytes;
        }

        private static void Write(byte[] bytes, int offset, string tag)
        {
            for (int i = 0; i < tag.Length; i++)
            {
                bytes[offset + i] = (byte)tag[i];
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Pollinations
{
    /// <summary>Image generation, decoded straight into a Texture2D.</summary>
    [AddComponentMenu("Pollinations/Pollinations Image")]
    public class PollinationsImage : PollinationsBehaviour
    {
        [Serializable]
        public sealed class TextureEvent : UnityEvent<Texture2D>
        {
        }

        [Header("Image")]
        [SerializeField] private string model = PollinationsConfig.DefaultImageModel;

        [Tooltip("0 keeps the server default.")]
        [SerializeField] private int width = 0;

        [Tooltip("0 keeps the server default.")]
        [SerializeField] private int height = 0;

        [Tooltip("-1 lets Pollinations pick one.")]
        [SerializeField] private int seed = -1;

        [Tooltip("Empty, \"true\" or \"false\".")]
        [SerializeField] private string safe = "";

        [Tooltip("Empty, \"low\", \"medium\", \"high\" or \"hd\".")]
        [SerializeField] private string quality = "";

        [SerializeField] private bool transparent = false;

        [Tooltip("Called with the decoded texture.")]
        public TextureEvent onGenerated = new TextureEvent();

        public event Action<PollinationsResult> Generated;

        public string Model
        {
            get { return model; }
            set { model = value; }
        }

        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public int Seed
        {
            get { return seed; }
            set { seed = value; }
        }

        public bool Transparent
        {
            get { return transparent; }
            set { transparent = value; }
        }

        /// <summary>The generated texture, or null before the first call.</summary>
        public Texture2D Texture { get; private set; }

        public async Task<PollinationsResult> GenerateAsync(string prompt)
        {
            string url = PollinationsUrls.ImagePrompt(prompt, model, width, height, seed, safe, quality, transparent);
            PollinationsResult result = await Client.GetAsync(url);

            if (result.Ok)
            {
                Texture2D texture = Decode(result.Bytes);
                if (texture == null)
                {
                    result = PollinationsResult.Failure(
                        PollinationsErrorKind.Parse,
                        result.StatusCode,
                        url,
                        "the image payload could not be decoded (" + result.ContentType + ", " + result.Bytes.Length + " bytes)");
                }
                else
                {
                    if (Texture != null)
                    {
                        Destroy(Texture);
                    }

                    Texture = texture;
                }
            }

            Report(result);
            if (result.Ok)
            {
                onGenerated.Invoke(Texture);
                if (Generated != null)
                {
                    Generated(result);
                }
            }

            return result;
        }

        /// <summary>Builds a texture from image bytes. Returns null when Unity cannot read them.</summary>
        public static Texture2D Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, true))
            {
                Destroy(texture);
                return null;
            }

            return texture;
        }
    }
}

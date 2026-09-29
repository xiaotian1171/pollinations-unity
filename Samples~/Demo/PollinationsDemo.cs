using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pollinations.Samples
{
    /// <summary>
    /// A ready-to-run demo. Drop it on an empty GameObject in any scene and press
    /// Play: it creates the five nodes, builds a small overlay and shows text,
    /// an image and speech coming back from the API.
    ///
    /// The methods are public on purpose, so a button in your own UI can call
    /// GenerateText, GenerateImage, Speak, LoadCatalog or SignIn.
    /// </summary>
    [AddComponentMenu("Pollinations/Pollinations Demo")]
    public class PollinationsDemo : MonoBehaviour
    {
        [Header("Demo")]
        [TextArea(2, 4)]
        [SerializeField] private string prompt = "Describe a watermill in one sentence.";

        [TextArea(2, 4)]
        [SerializeField] private string imagePrompt = "a wooden watermill by a stream, flat illustration";

        [TextArea(2, 4)]
        [SerializeField] private string speechText = "Welcome to Pollen Village, traveller.";

        [Tooltip("Run text, image and speech once when the scene starts.")]
        [SerializeField] private bool runOnStart = true;

        [Tooltip("Skip the image call in the demo, since it is the slowest one.")]
        [SerializeField] private bool includeImage = true;

        private PollinationsText _text;
        private PollinationsImage _image;
        private PollinationsSpeech _speech;
        private PollinationsCatalog _catalog;
        private PollinationsAuth _auth;
        private AudioSource _audio;

        private Text _status;
        private Text _output;
        private RawImage _preview;
        private Text _account;

        private readonly List<string> _lines = new List<string>();

        private void Awake()
        {
            BuildNodes();
            BuildOverlay();
            _text.onGenerated.AddListener(answer => Say("text: " + answer));
            _text.Generated += result => Say("text done in " + result.Seconds.ToString("0.00") + "s (" + result.Attempts + " attempt(s))");
            _image.onGenerated.AddListener(texture =>
            {
                _preview.texture = texture;
                _preview.color = Color.white;
                Say("image: " + texture.width + "x" + texture.height);
            });
            _speech.onGenerated.AddListener(clip =>
            {
                _audio.clip = clip;
                _audio.Play();
                Say("speech: " + clip.length.ToString("0.0") + "s");
            });
            _auth.onCodeReady.AddListener((code, url) => Say("sign in: enter " + code + " at " + url));
            _auth.onSignedIn.AddListener(username => Say("signed in as " + username));
            _auth.onFailed.AddListener(error => Say("sign in failed: " + error));
        }

        private void Start()
        {
            if (!runOnStart)
            {
                return;
            }

            GenerateText();
            if (includeImage)
            {
                GenerateImage();
            }

            Speak();
        }

        private void BuildNodes()
        {
            _text = gameObject.AddComponent<PollinationsText>();
            _text.SystemPrompt = "You are the miller of a small village. Answer in one sentence.";

            _image = gameObject.AddComponent<PollinationsImage>();
            _image.Width = 512;
            _image.Height = 512;

            _speech = gameObject.AddComponent<PollinationsSpeech>();
            _catalog = gameObject.AddComponent<PollinationsCatalog>();
            _auth = gameObject.AddComponent<PollinationsAuth>();

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
        }

        /// <summary>Generates text with the prompt from the inspector.</summary>
        public async void GenerateText()
        {
            Say("text: asking...");
            string answer = await _text.GenerateTextAsync(prompt);
            if (string.IsNullOrEmpty(answer))
            {
                Say("text: no answer");
            }
        }

        /// <summary>Generates an image and shows it in the overlay.</summary>
        public async void GenerateImage()
        {
            Say("image: drawing...");
            PollinationsResult result = await _image.GenerateAsync(imagePrompt);
            if (!result.Ok)
            {
                Say("image: " + result.KindName + " - " + result.Error);
            }
        }

        /// <summary>Generates speech and plays it.</summary>
        public async void Speak()
        {
            Say("speech: synthesising...");
            PollinationsResult result = await _speech.GenerateAsync(speechText);
            if (!result.Ok)
            {
                Say("speech: " + result.KindName + " - " + result.Message);
            }
        }

        /// <summary>Fills the overlay with the live model ids.</summary>
        public async void LoadCatalog()
        {
            PollinationsResult result = await _catalog.FetchAsync("text");
            if (result.Ok)
            {
                Say("catalogue: " + _catalog.Models.Count + " text models");
            }
            else
            {
                Say("catalogue: " + result.KindName);
            }
        }

        /// <summary>Starts the device sign-in and shows the code on screen.</summary>
        public async void SignIn()
        {
            Say("sign in: requesting a code...");
            PollinationsResult result = await _auth.SignInAsync();
            if (!result.Ok)
            {
                Say("sign in: " + result.KindName + " - " + result.Error);
            }
        }

        /// <summary>Forgets the key on this device.</summary>
        public void SignOut()
        {
            _auth.SignOut();
            Say("signed out");
        }

        private void Say(string line)
        {
            Debug.Log("[demo] " + line);
            _lines.Add(line);
            while (_lines.Count > 12)
            {
                _lines.RemoveAt(0);
            }

            string text = string.Join("\n", _lines.ToArray());
            if (_output != null)
            {
                _output.text = text;
            }

            if (_status != null)
            {
                _status.text = line;
            }
        }

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("Pollinations Demo UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Font font = BuiltinFont();

            _preview = AddRawImage(canvasObject.transform, "Preview", new Vector2(256f, 256f), new Vector2(-16f, -16f), new Vector2(1f, 1f));
            _status = AddText(canvasObject.transform, "Status", font, 18, new Vector2(16f, -16f), new Vector2(0f, 1f), TextAnchor.UpperLeft, new Vector2(620f, 28f));
            _output = AddText(canvasObject.transform, "Output", font, 14, new Vector2(16f, -52f), new Vector2(0f, 1f), TextAnchor.UpperLeft, new Vector2(620f, 320f));
            _account = AddText(canvasObject.transform, "Account", font, 13, new Vector2(16f, 16f), new Vector2(0f, 0f), TextAnchor.LowerLeft, new Vector2(620f, 40f));
            _account.text = "Text, image and speech come from Pollinations. Sign in to spend your own pollen.";
        }

        private static Font BuiltinFont()
        {
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    return font;
                }
            }
            catch (System.Exception)
            {
                // older Unity versions use Arial
            }

            try
            {
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static Text AddText(Transform parent, string name, Font font, int size, Vector2 offset, Vector2 anchor, TextAnchor alignment, Vector2 dimensions)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, anchor.y);
            rect.anchoredPosition = offset;
            rect.sizeDelta = dimensions;

            var text = gameObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static RawImage AddRawImage(Transform parent, string name, Vector2 dimensions, Vector2 offset, Vector2 anchor)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, anchor.y);
            rect.anchoredPosition = offset;
            rect.sizeDelta = dimensions;

            var image = gameObject.GetComponent<RawImage>();
            image.color = new Color(1f, 1f, 1f, 0.15f);
            return image;
        }
    }
}

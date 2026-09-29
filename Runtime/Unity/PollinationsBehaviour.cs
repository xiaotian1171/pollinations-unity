using UnityEngine;

namespace Pollinations
{
    /// <summary>
    /// Shared setup for the components: builds the client, resolves the key and
    /// keeps the request log out of release builds unless asked for.
    /// </summary>
    public abstract class PollinationsBehaviour : MonoBehaviour
    {
        [Header("Pollinations")]
        [Tooltip("Optional. Paste a development key here, or set POLLINATIONS_API_KEY, or sign in with PollinationsAuth.")]
        [SerializeField] private string apiKey = "";

        [Tooltip("Keep the key in PlayerPrefs so the player stays signed in.")]
        [SerializeField] private bool rememberApiKey = false;

        [Tooltip("Extra attempts after the first one, for rate limits, server errors and network failures.")]
        [SerializeField] private int maxRetries = 2;

        [Tooltip("Seconds before the first retry; doubles on each attempt.")]
        [SerializeField] private float retryBaseSeconds = 0.75f;

        [Tooltip("Log every request and its classification to the console.")]
        [SerializeField] private bool logRequests = false;

        private PollinationsClient _client;

        /// <summary>A client wired to UnityWebRequest, with this component's settings applied.</summary>
        public PollinationsClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new PollinationsClient(new UnityWebRequestTransport(this));
                    _client.MaxRetries = maxRetries;
                    _client.RetryBaseSeconds = retryBaseSeconds;
                }

                return _client;
            }
        }

        public bool LogRequests
        {
            get { return logRequests; }
        }

        /// <summary>A development key typed in the inspector, empty when there is none.</summary>
        public string ApiKey
        {
            get { return apiKey; }
        }

        protected virtual void Awake()
        {
            PollinationsConfig.UseKeyStore(new UnityPlayerPrefsKeyStore());
            ApplyApiKey();
        }

        /// <summary>Pushes the inspector key, when there is one, into the configuration.</summary>
        public void ApplyApiKey()
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                PollinationsConfig.SetApiKey(apiKey, rememberApiKey);
            }
        }

        /// <summary>Logs a failure once, with the classification and the player facing message.</summary>
        protected void Report(PollinationsResult result)
        {
            if (result == null)
            {
                return;
            }

            if (result.Ok)
            {
                if (logRequests)
                {
                    Debug.Log("[pollinations] " + result.KindName + " " + result.StatusCode + " " + result.Url
                        + " (" + result.Attempts + " attempt(s), " + result.Seconds.ToString("0.00") + "s)");
                }

                return;
            }

            string message = result.KindName + " " + result.StatusCode + " " + result.Url
                + " - " + result.Error + " :: " + result.Message;
            if (result.Kind == PollinationsErrorKind.Auth || result.Kind == PollinationsErrorKind.Balance)
            {
                Debug.LogWarning("[pollinations] " + message);
                return;
            }

            Debug.LogError("[pollinations] " + message);
        }
    }
}

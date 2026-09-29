using System;

namespace Pollinations
{
    /// <summary>Where the key comes from, so the core does not depend on PlayerPrefs.</summary>
    public interface IPollinationsKeyStore
    {
        string Read(string key);
        void Write(string key, string value);
        void Delete(string key);
    }

    public sealed class InMemoryKeyStore : IPollinationsKeyStore
    {
        private readonly System.Collections.Generic.Dictionary<string, string> _values =
            new System.Collections.Generic.Dictionary<string, string>();

        public string Read(string key)
        {
            string value;
            return _values.TryGetValue(key, out value) ? value : null;
        }

        public void Write(string key, string value)
        {
            _values[key] = value;
        }

        public void Delete(string key)
        {
            _values.Remove(key);
        }
    }

    /// <summary>
    /// Key resolution and defaults. A key can come from the environment (useful
    /// in a build server), from a store the game provides, or from a session
    /// value set at runtime - which is what the device flow does.
    /// </summary>
    public static class PollinationsConfig
    {
        public const string EnvApiKey = "POLLINATIONS_API_KEY";
        public const string EnvAppKey = "POLLINATIONS_APP_KEY";
        public const string StoredKeyName = "pollinations.api_key";
        public const string DefaultAppKey = "pk_unity";

        public const string DefaultTextModel = "nova-fast";
        public const string DefaultImageModel = "tongyi-mai/z-image-turbo";
        public const string DefaultSpeechModel = "elevenlabs/eleven-v3";
        public const string DefaultVoice = "alloy";

        private static string _sessionKey = "";
        private static IPollinationsKeyStore _store = new InMemoryKeyStore();

        /// <summary>The key to send. Empty means anonymous requests.</summary>
        public static string ApiKey
        {
            get
            {
                if (!string.IsNullOrEmpty(_sessionKey))
                {
                    return _sessionKey;
                }

                if (_store != null)
                {
                    string stored = _store.Read(StoredKeyName);
                    if (!string.IsNullOrEmpty(stored))
                    {
                        return stored;
                    }
                }

                return Environment.GetEnvironmentVariable(EnvApiKey) ?? "";
            }
        }

        /// <summary>The pk_ key that a device sign-in is attributed to.</summary>
        public static string AppKey
        {
            get
            {
                string configured = Environment.GetEnvironmentVariable(EnvAppKey);
                return string.IsNullOrEmpty(configured) ? DefaultAppKey : configured;
            }
        }

        public static bool HasApiKey
        {
            get { return !string.IsNullOrEmpty(ApiKey); }
        }

        /// <summary>Sets the key for this session. With <paramref name="remember"/> it survives a restart.</summary>
        public static void SetApiKey(string key, bool remember = false)
        {
            _sessionKey = key ?? "";
            if (_store == null)
            {
                return;
            }

            if (remember && !string.IsNullOrEmpty(key))
            {
                _store.Write(StoredKeyName, key);
            }
            else
            {
                _store.Delete(StoredKeyName);
            }
        }

        public static void ClearApiKey(bool keepStored = false)
        {
            _sessionKey = "";
            if (!keepStored && _store != null)
            {
                _store.Delete(StoredKeyName);
            }
        }

        public static string StoredApiKey
        {
            get { return _store == null ? "" : (_store.Read(StoredKeyName) ?? ""); }
        }

        /// <summary>Lets Unity plug PlayerPrefs in.</summary>
        public static void UseKeyStore(IPollinationsKeyStore store)
        {
            _store = store ?? new InMemoryKeyStore();
        }

        /// <summary>"Bearer sk_..." ready to put on a request, or null when there is nothing to send.</summary>
        public static string AuthorizationHeader()
        {
            string key = ApiKey;
            return string.IsNullOrEmpty(key) ? null : "Bearer " + key;
        }
    }
}

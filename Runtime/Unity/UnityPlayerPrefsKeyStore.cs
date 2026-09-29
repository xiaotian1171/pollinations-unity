using UnityEngine;

namespace Pollinations
{
    /// <summary>Keeps a remembered key in PlayerPrefs, so a signed-in player stays signed in.</summary>
    public sealed class UnityPlayerPrefsKeyStore : IPollinationsKeyStore
    {
        private readonly string _prefix;

        public UnityPlayerPrefsKeyStore(string prefix = "pollinations.")
        {
            _prefix = prefix;
        }

        public string Read(string key)
        {
            return PlayerPrefs.GetString(_prefix + key, "");
        }

        public void Write(string key, string value)
        {
            PlayerPrefs.SetString(_prefix + key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(_prefix + key);
            PlayerPrefs.Save();
        }
    }
}

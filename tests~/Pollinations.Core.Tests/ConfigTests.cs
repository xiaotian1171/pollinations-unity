using System;

namespace Pollinations.Tests
{
    public static class ConfigTests
    {
        public static void Run()
        {
            Check.Suite("config");

            // the live check runs after this suite, so the environment is restored
            string realKey = Environment.GetEnvironmentVariable(PollinationsConfig.EnvApiKey);
            string realAppKey = Environment.GetEnvironmentVariable(PollinationsConfig.EnvAppKey);

            PollinationsConfig.UseKeyStore(new InMemoryKeyStore());
            PollinationsConfig.ClearApiKey();
            Environment.SetEnvironmentVariable(PollinationsConfig.EnvApiKey, null);
            Environment.SetEnvironmentVariable(PollinationsConfig.EnvAppKey, null);

            Check.False(PollinationsConfig.HasApiKey, "no key by default");
            Check.Null(PollinationsConfig.AuthorizationHeader(), "no header without a key");
            Check.Equal(PollinationsConfig.DefaultAppKey, PollinationsConfig.AppKey, "the default app key");
            Check.Equal("nova-fast", PollinationsConfig.DefaultTextModel, "default text model");
            Check.Equal("tongyi-mai/z-image-turbo", PollinationsConfig.DefaultImageModel, "default image model");

            Environment.SetEnvironmentVariable(PollinationsConfig.EnvApiKey, "sk_from_env");
            Check.Equal("sk_from_env", PollinationsConfig.ApiKey, "reads the environment");
            Check.Equal("Bearer sk_from_env", PollinationsConfig.AuthorizationHeader(), "builds the header");

            Environment.SetEnvironmentVariable(PollinationsConfig.EnvAppKey, "pk_from_env");
            Check.Equal("pk_from_env", PollinationsConfig.AppKey, "reads the app key from the environment");
            Environment.SetEnvironmentVariable(PollinationsConfig.EnvAppKey, null);

            PollinationsConfig.SetApiKey("sk_session");
            Check.Equal("sk_session", PollinationsConfig.ApiKey, "a session key wins over the environment");
            Check.Equal("", PollinationsConfig.StoredApiKey, "an unremembered key is not stored");

            PollinationsConfig.ClearApiKey();
            Check.Equal("sk_from_env", PollinationsConfig.ApiKey, "clearing falls back to the environment");

            PollinationsConfig.SetApiKey("sk_remembered", true);
            Check.Equal("sk_remembered", PollinationsConfig.StoredApiKey, "remembering writes to the store");
            PollinationsConfig.ClearApiKey(true);
            Check.Equal("sk_remembered", PollinationsConfig.ApiKey, "a stored key is used again");
            Check.Equal("sk_remembered", PollinationsConfig.StoredApiKey, "keeping the store survives a session clear");

            PollinationsConfig.ClearApiKey();
            Check.Equal("", PollinationsConfig.StoredApiKey, "clearing without keeping forgets the key");
            Check.Equal("sk_from_env", PollinationsConfig.ApiKey, "an unremembered key falls back to the environment");

            PollinationsConfig.SetApiKey("sk_not_remembered");
            Check.Equal("", PollinationsConfig.StoredApiKey, "a session key is not stored");
            PollinationsConfig.ClearApiKey();
            Environment.SetEnvironmentVariable(PollinationsConfig.EnvApiKey, null);
            Check.False(PollinationsConfig.HasApiKey, "everything can be cleared");

            var store = new InMemoryKeyStore();
            store.Write("other", "value");
            Check.Equal("value", store.Read("other"), "the store reads what it wrote");
            store.Delete("other");
            Check.Null(store.Read("other"), "the store deletes");
            Check.Null(store.Read("missing"), "the store returns null for a missing key");

            PollinationsConfig.UseKeyStore(null);
            PollinationsConfig.SetApiKey("sk_after_null_store", true);
            Check.Equal("sk_after_null_store", PollinationsConfig.ApiKey, "a session key still works without a store");

            PollinationsConfig.UseKeyStore(new InMemoryKeyStore());
            PollinationsConfig.ClearApiKey();
            Environment.SetEnvironmentVariable(PollinationsConfig.EnvApiKey, realKey);
            Environment.SetEnvironmentVariable(PollinationsConfig.EnvAppKey, realAppKey);
        }
    }
}

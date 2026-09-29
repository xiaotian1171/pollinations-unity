using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Pollinations
{
    /// <summary>
    /// Sign-in with the player's own Pollen. The game asks for a code, shows it,
    /// the player approves it on enter.pollinations.ai, and the game receives a key
    /// that belongs to that player. Nothing secret ships inside the build, and the
    /// player's own balance pays for what their game generates.
    /// </summary>
    [AddComponentMenu("Pollinations/Pollinations Auth")]
    public class PollinationsAuth : PollinationsBehaviour
    {
        [Serializable]
        public sealed class CodeEvent : UnityEvent<string, string>
        {
        }

        [Serializable]
        public sealed class UserEvent : UnityEvent<string>
        {
        }

        [Serializable]
        public sealed class FailureEvent : UnityEvent<string>
        {
        }

        [Header("Sign in")]
        [Tooltip("The pk_ app key of your game. Empty uses POLLINATIONS_APP_KEY or the package default.")]
        [SerializeField] private string appKey = "";

        [Tooltip("Seconds between approval checks.")]
        [SerializeField] private float pollIntervalSeconds = 5f;

        [Tooltip("Stop waiting after this many seconds.")]
        [SerializeField] private float expiresInSeconds = 600f;

        [Tooltip("Keep the key on this device so the player stays signed in.")]
        [SerializeField] private bool remember = true;

        [Tooltip("Read the player's name and picture once signed in.")]
        [SerializeField] private bool fetchProfile = true;

        [Tooltip("Called with the code to show and the page to open.")]
        public CodeEvent onCodeReady = new CodeEvent();

        [Tooltip("Called once the player approved, with their username.")]
        public UserEvent onSignedIn = new UserEvent();

        [Tooltip("Called when the sign-in could not be completed.")]
        public FailureEvent onFailed = new FailureEvent();

        public bool IsSignedIn { get; private set; }

        public Dictionary<string, string> User { get; private set; }

        public string AppKey
        {
            get { return string.IsNullOrEmpty(appKey) ? PollinationsConfig.AppKey : appKey; }
            set { appKey = value; }
        }

        public PollinationsAuth()
        {
            User = new Dictionary<string, string>();
        }

        /// <summary>The last code handed to the player, for a UI that wants to show it again.</summary>
        public PollinationsDeviceCode PendingCode { get; private set; }

        /// <summary>
        /// Runs the whole flow. It returns when the player approved, refused, the
        /// code expired or the request failed - so a UI can just await it.
        /// </summary>
        public async Task<PollinationsResult> SignInAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            PollinationsResult result = await Client.PostJsonAnonymousAsync(
                PollinationsUrls.DeviceCode(),
                PollinationsDeviceFlow.BuildCodeRequest(AppKey));

            if (!result.Ok)
            {
                return Failed(result, result.Error);
            }

            string error;
            PollinationsDeviceCode code = PollinationsDeviceFlow.ParseCode(result.Text, out error);
            if (code == null)
            {
                return Failed(result, error);
            }

            PendingCode = code;
            double expiresIn = expiresInSeconds > 0f ? expiresInSeconds : code.ExpiresIn;
            onCodeReady.Invoke(code.UserCode, code.VerificationUrl);
            Debug.Log("[pollinations] sign-in code " + code.UserCode + " at " + code.VerificationUrl);

            double interval = pollIntervalSeconds > 0f ? pollIntervalSeconds : code.Interval;
            double waited = 0.0;

            while (waited < expiresIn)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
                waited += interval;

                PollinationsResult poll = await Client.PostJsonAnonymousAsync(
                    PollinationsUrls.DeviceToken(),
                    PollinationsDeviceFlow.BuildTokenRequest(code.DeviceCode));

                PollinationsTokenPoll token = PollinationsDeviceFlow.ParseToken(poll.StatusCode, poll.Text);
                switch (token.State)
                {
                    case PollinationsTokenState.Granted:
                        PollinationsConfig.SetApiKey(token.AccessToken, remember);
                        IsSignedIn = true;
                        if (fetchProfile)
                        {
                            await FetchProfileAsync(cancellationToken);
                        }

                        string username = User.ContainsKey("preferred_username") ? User["preferred_username"] : "";
                        onSignedIn.Invoke(username);
                        return PollinationsResult.Success(
                            new PollinationsResponse
                            {
                                StatusCode = poll.StatusCode,
                                Bytes = poll.Bytes,
                                ContentType = poll.ContentType,
                                Url = poll.Url,
                            },
                            poll.Attempts,
                            poll.Seconds);

                    case PollinationsTokenState.Pending:
                        continue;

                    case PollinationsTokenState.SlowDown:
                        interval = token.Interval > 0.0 ? token.Interval + 1.0 : interval * 2.0;
                        continue;

                    case PollinationsTokenState.Expired:
                    case PollinationsTokenState.Denied:
                    case PollinationsTokenState.Error:
                        return Failed(poll, token.Error);
                }
            }

            return Failed(result, "the sign-in code expired before it was approved");
        }

        /// <summary>Reads the player's profile once they are signed in.</summary>
        public async Task<Dictionary<string, string>> FetchProfileAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            PollinationsResult result = await Client.GetAsync(PollinationsUrls.DeviceUserInfo());
            User = result.Ok ? PollinationsDeviceFlow.ParseUserInfo(result.Text) : new Dictionary<string, string>();
            return User;
        }

        /// <summary>Forgets the key. The player can revoke it on the Pollinations site too.</summary>
        public void SignOut()
        {
            PollinationsConfig.ClearApiKey();
            IsSignedIn = false;
            User = new Dictionary<string, string>();
            PendingCode = null;
        }

        private PollinationsResult Failed(PollinationsResult source, string error)
        {
            PollinationsErrorKind kind = source == null ? PollinationsErrorKind.Unknown : source.Kind;
            if (kind == PollinationsErrorKind.None)
            {
                kind = PollinationsErrorKind.Auth;
            }

            string detail = string.IsNullOrEmpty(error)
                ? "the sign-in could not be completed"
                : error;

            Report(PollinationsResult.Failure(kind, source == null ? 0 : source.StatusCode, source == null ? "" : source.Url, detail));
            onFailed.Invoke(detail);
            return PollinationsResult.Failure(kind, source == null ? 0 : source.StatusCode, source == null ? "" : source.Url, detail);
        }
    }
}

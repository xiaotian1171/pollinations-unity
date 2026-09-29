using System.Collections.Generic;

namespace Pollinations
{
    /// <summary>The answer to a device code request.</summary>
    public sealed class PollinationsDeviceCode
    {
        public string DeviceCode { get; set; }
        public string UserCode { get; set; }
        public string VerificationUrl { get; set; }
        public double Interval { get; set; }
        public double ExpiresIn { get; set; }

        public PollinationsDeviceCode()
        {
            DeviceCode = "";
            UserCode = "";
            VerificationUrl = PollinationsUrls.DeviceVerificationPage();
            Interval = 5.0;
            ExpiresIn = 600.0;
        }
    }

    public enum PollinationsTokenState
    {
        Pending = 0,
        Granted = 1,
        SlowDown = 2,
        Expired = 3,
        Denied = 4,
        Error = 5,
    }

    /// <summary>The answer to a token poll.</summary>
    public sealed class PollinationsTokenPoll
    {
        public PollinationsTokenState State { get; set; }
        public string AccessToken { get; set; }
        public string Error { get; set; }
        public double Interval { get; set; }

        public PollinationsTokenPoll()
        {
            State = PollinationsTokenState.Error;
            AccessToken = "";
            Error = "";
            Interval = 0.0;
        }

        public bool Granted
        {
            get { return State == PollinationsTokenState.Granted; }
        }

        public bool Terminal
        {
            get { return State == PollinationsTokenState.Granted || State == PollinationsTokenState.Expired || State == PollinationsTokenState.Denied || State == PollinationsTokenState.Error; }
        }
    }

    /// <summary>
    /// The parsing half of the device flow, so the state machine can be tested
    /// without a server. The endpoints are unauthenticated and use a pk_ app key.
    /// </summary>
    public static class PollinationsDeviceFlow
    {
        /// <summary>POST https://enter.pollinations.ai/api/device/code {"client_id": "pk_..."}</summary>
        public static Dictionary<string, object> BuildCodeRequest(string appKey)
        {
            return new Dictionary<string, object>
            {
                { "client_id", string.IsNullOrEmpty(appKey) ? PollinationsConfig.DefaultAppKey : appKey },
            };
        }

        /// <summary>POST .../api/device/token {"device_code": "..."}</summary>
        public static Dictionary<string, object> BuildTokenRequest(string deviceCode)
        {
            return new Dictionary<string, object>
            {
                { "device_code", deviceCode ?? "" },
            };
        }

        public static PollinationsDeviceCode ParseCode(string json, out string error)
        {
            error = "";
            object parsed;
            if (!PollinationsJson.TryParse(json, out parsed))
            {
                error = "the device code response was not JSON";
                return null;
            }

            var payload = parsed as Dictionary<string, object>;
            if (payload == null)
            {
                error = "the device code response was not an object";
                return null;
            }

            string code = PollinationsJson.GetString(payload, "device_code");
            if (string.IsNullOrEmpty(code))
            {
                error = PollinationsJson.GetString(payload, "error", "the response carried no device_code");
                return null;
            }

            string verification = PollinationsJson.GetString(payload, "verification_uri");
            if (string.IsNullOrEmpty(verification))
            {
                verification = PollinationsJson.GetString(payload, "verification_url");
            }

            var result = new PollinationsDeviceCode
            {
                DeviceCode = code,
                UserCode = PollinationsJson.GetString(payload, "user_code"),
                VerificationUrl = PollinationsUrls.Absolute(verification),
                Interval = PollinationsJson.GetNumber(payload, "interval", 5.0),
                ExpiresIn = PollinationsJson.GetNumber(payload, "expires_in", 600.0),
            };

            if (result.Interval < 1.0)
            {
                result.Interval = 1.0;
            }

            return result;
        }

        /// <summary>
        /// Reads a token poll. A pending approval comes back as HTTP 400 with
        /// {"error":"authorization_pending"}, which is not a failure.
        /// </summary>
        public static PollinationsTokenPoll ParseToken(int statusCode, string json)
        {
            var poll = new PollinationsTokenPoll();
            object parsed;
            var payload = PollinationsJson.TryParse(json, out parsed) ? parsed as Dictionary<string, object> : null;
            string error = payload == null ? "" : PollinationsJson.GetString(payload, "error");
            string lowered = (error ?? "").ToLowerInvariant();

            if (payload != null && (statusCode == 200 || statusCode == 201))
            {
                string token = PollinationsJson.GetString(payload, "access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    poll.State = PollinationsTokenState.Granted;
                    poll.AccessToken = token;
                    return poll;
                }
            }

            if (lowered.Contains("authorization_pending") || lowered.Contains("pending"))
            {
                poll.State = PollinationsTokenState.Pending;
                poll.Error = error;
                return poll;
            }

            if (lowered.Contains("slow_down") || lowered.Contains("slow down"))
            {
                poll.State = PollinationsTokenState.SlowDown;
                poll.Error = error;
                poll.Interval = payload == null ? 0.0 : PollinationsJson.GetNumber(payload, "interval", 0.0);
                return poll;
            }

            if (lowered.Contains("expired"))
            {
                poll.State = PollinationsTokenState.Expired;
                poll.Error = string.IsNullOrEmpty(error) ? "the code expired before it was approved" : error;
                return poll;
            }

            if (lowered.Contains("denied") || lowered.Contains("access_denied"))
            {
                poll.State = PollinationsTokenState.Denied;
                poll.Error = string.IsNullOrEmpty(error) ? "the sign-in was refused" : error;
                return poll;
            }

            poll.State = PollinationsTokenState.Error;
            poll.Error = string.IsNullOrEmpty(error)
                ? "the token request failed with status " + statusCode
                : error;
            return poll;
        }

        /// <summary>Reads /api/device/userinfo into a few friendly fields.</summary>
        public static Dictionary<string, string> ParseUserInfo(string json)
        {
            var result = new Dictionary<string, string>();
            object parsed;
            if (!PollinationsJson.TryParse(json, out parsed))
            {
                return result;
            }

            var payload = parsed as Dictionary<string, object>;
            if (payload == null)
            {
                return result;
            }

            string[] keys = { "sub", "preferred_username", "name", "picture", "email" };
            foreach (string key in keys)
            {
                string value = PollinationsJson.GetString(payload, key);
                if (!string.IsNullOrEmpty(value))
                {
                    result[key] = value;
                }
            }

            return result;
        }
    }
}

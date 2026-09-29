namespace Pollinations.Tests
{
    public static class DeviceFlowTests
    {
        public static void Run()
        {
            Check.Suite("device flow");

            var codeRequest = PollinationsDeviceFlow.BuildCodeRequest("pk_game");
            Check.Equal("pk_game", PollinationsJson.GetString(codeRequest, "client_id"), "the code request carries the app key");
            Check.Equal(PollinationsConfig.DefaultAppKey, PollinationsJson.GetString(PollinationsDeviceFlow.BuildCodeRequest(""), "client_id"), "an empty app key falls back to the default");
            Check.Equal(PollinationsConfig.DefaultAppKey, PollinationsJson.GetString(PollinationsDeviceFlow.BuildCodeRequest(null), "client_id"), "a null app key falls back to the default");
            Check.Equal("abc", PollinationsJson.GetString(PollinationsDeviceFlow.BuildTokenRequest("abc"), "device_code"), "the token request carries the code");
            Check.Equal("", PollinationsJson.GetString(PollinationsDeviceFlow.BuildTokenRequest(null), "device_code"), "a null code becomes an empty string");

            string error;
            PollinationsDeviceCode code = PollinationsDeviceFlow.ParseCode(
                "{\"device_code\":\"dc_1\",\"user_code\":\"5QFNST88\",\"verification_uri\":\"/device\",\"interval\":5,\"expires_in\":1800}",
                out error);
            Check.NotNull(code, "parses a code response");
            Check.Equal("", error, "no error for a good response");
            Check.Equal("dc_1", code.DeviceCode, "reads the device code");
            Check.Equal("5QFNST88", code.UserCode, "reads the user code");
            Check.Equal("https://enter.pollinations.ai/device", code.VerificationUrl, "absolutizes the verification url");
            Check.Equal(5.0, code.Interval, "reads the interval");
            Check.Equal(1800.0, code.ExpiresIn, "reads the expiry");

            code = PollinationsDeviceFlow.ParseCode(
                "{\"device_code\":\"dc_2\",\"verification_url\":\"https://enter.pollinations.ai/device?user_code=X\"}",
                out error);
            Check.NotNull(code, "accepts verification_url");
            Check.Equal("https://enter.pollinations.ai/device?user_code=X", code.VerificationUrl, "keeps an absolute url");
            Check.Equal(5.0, code.Interval, "a default interval");
            Check.Equal(600.0, code.ExpiresIn, "a default expiry");

            code = PollinationsDeviceFlow.ParseCode("{\"device_code\":\"dc_3\",\"interval\":0.2}", out error);
            Check.Equal(1.0, code.Interval, "the interval has a floor");

            Check.Null(PollinationsDeviceFlow.ParseCode("", out error), "an empty response is not a code");
            Check.Contains(error, "not JSON", "the error explains the failure");
            Check.Null(PollinationsDeviceFlow.ParseCode("[1]", out error), "an array is not a code");
            Check.Null(PollinationsDeviceFlow.ParseCode("{\"error\":\"invalid_client\"}", out error), "an error body is not a code");
            Check.Equal("invalid_client", error, "the API error is passed through");

            // a pending approval is an HTTP 400, not a failure
            PollinationsTokenPoll poll = PollinationsDeviceFlow.ParseToken(400, "{\"error\":\"authorization_pending\"}");
            Check.Equal(PollinationsTokenState.Pending, poll.State, "400 pending is pending");
            Check.False(poll.Granted, "pending is not granted");
            Check.False(poll.Terminal, "pending is not terminal");

            poll = PollinationsDeviceFlow.ParseToken(200, "{\"error\":\"authorization_pending\"}");
            Check.Equal(PollinationsTokenState.Pending, poll.State, "a pending body wins even on 200");

            poll = PollinationsDeviceFlow.ParseToken(200, "{\"access_token\":\"sk_player\",\"token_type\":\"bearer\"}");
            Check.Equal(PollinationsTokenState.Granted, poll.State, "a token means granted");
            Check.True(poll.Granted, "granted");
            Check.True(poll.Terminal, "granted is terminal");
            Check.Equal("sk_player", poll.AccessToken, "reads the access token");

            poll = PollinationsDeviceFlow.ParseToken(200, "{\"error\":\"slow_down\",\"interval\":10}");
            Check.Equal(PollinationsTokenState.SlowDown, poll.State, "slow_down is recognised");
            Check.Equal(10.0, poll.Interval, "slow_down carries a new interval");
            Check.False(poll.Terminal, "slow_down is not terminal");

            poll = PollinationsDeviceFlow.ParseToken(400, "{\"error\":\"expired_token\"}");
            Check.Equal(PollinationsTokenState.Expired, poll.State, "an expired code");
            Check.True(poll.Terminal, "expiry is terminal");

            poll = PollinationsDeviceFlow.ParseToken(403, "{\"error\":\"access_denied\"}");
            Check.Equal(PollinationsTokenState.Denied, poll.State, "a refused sign-in");
            Check.True(poll.Terminal, "denial is terminal");

            poll = PollinationsDeviceFlow.ParseToken(500, "not json");
            Check.Equal(PollinationsTokenState.Error, poll.State, "an unreadable answer is an error");
            Check.Contains(poll.Error, "500", "the error carries the status");

            var user = PollinationsDeviceFlow.ParseUserInfo(
                "{\"sub\":\"u_1\",\"preferred_username\":\"xiaotian\",\"picture\":\"https://x/y.png\",\"unused\":1}");
            Check.Equal("u_1", user["sub"], "reads sub");
            Check.Equal("xiaotian", user["preferred_username"], "reads the username");
            Check.Equal(3, user.Count, "ignores unknown fields");
            Check.Equal(0, PollinationsDeviceFlow.ParseUserInfo("nope").Count, "garbage gives no profile");
            Check.Equal(0, PollinationsDeviceFlow.ParseUserInfo("").Count, "empty gives no profile");
        }
    }
}

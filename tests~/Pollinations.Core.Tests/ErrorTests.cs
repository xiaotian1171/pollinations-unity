namespace Pollinations.Tests
{
    public static class ErrorTests
    {
        public static void Run()
        {
            Check.Suite("errors");

            Check.Equal(PollinationsErrorKind.None, PollinationsErrors.Classify(200, "{}"), "200 is fine");
            Check.Equal(PollinationsErrorKind.None, PollinationsErrors.Classify(201, ""), "201 is fine");
            Check.Equal(PollinationsErrorKind.Auth, PollinationsErrors.Classify(401, "{\"error\":\"A valid API key is required\"}"), "401 is auth");
            Check.Equal(PollinationsErrorKind.Auth, PollinationsErrors.Classify(403, ""), "403 is auth");
            Check.Equal(PollinationsErrorKind.Balance, PollinationsErrors.Classify(402, ""), "402 is balance");
            Check.Equal(PollinationsErrorKind.Balance, PollinationsErrors.Classify(400, "Insufficient balance. This request costs ~0.0136 pollen"), "a balance body on 400");
            Check.Equal(PollinationsErrorKind.Balance, PollinationsErrors.Classify(500, "insufficient_balance"), "a balance body on 500");
            Check.Equal(PollinationsErrorKind.Balance, PollinationsErrors.Classify(402, "your key is out of pollen"), "out of pollen wording");
            Check.Equal(PollinationsErrorKind.RateLimit, PollinationsErrors.Classify(429, ""), "429 is a rate limit");
            Check.Equal(PollinationsErrorKind.BadRequest, PollinationsErrors.Classify(400, "unknown model"), "400 is a bad request");
            Check.Equal(PollinationsErrorKind.BadRequest, PollinationsErrors.Classify(404, ""), "404 is a bad request");
            Check.Equal(PollinationsErrorKind.Server, PollinationsErrors.Classify(500, ""), "500 is server");
            Check.Equal(PollinationsErrorKind.Server, PollinationsErrors.Classify(503, ""), "503 is server");
            Check.Equal(PollinationsErrorKind.Network, PollinationsErrors.Classify(0, "", true), "a transport failure is network");
            Check.Equal(PollinationsErrorKind.Network, PollinationsErrors.Classify(0, ""), "status 0 is network");
            Check.Equal(PollinationsErrorKind.Parse, PollinationsErrors.Classify(200, "", false, true), "a parse failure wins");
            Check.Equal(PollinationsErrorKind.Unknown, PollinationsErrors.Classify(302, ""), "an odd status is unknown");

            Check.True(PollinationsErrors.Retryable(PollinationsErrorKind.RateLimit), "rate limits are retried");
            Check.True(PollinationsErrors.Retryable(PollinationsErrorKind.Server), "server errors are retried");
            Check.True(PollinationsErrors.Retryable(PollinationsErrorKind.Network), "network errors are retried");
            Check.False(PollinationsErrors.Retryable(PollinationsErrorKind.Auth), "auth is not retried");
            Check.False(PollinationsErrors.Retryable(PollinationsErrorKind.Balance), "balance is not retried");
            Check.False(PollinationsErrors.Retryable(PollinationsErrorKind.BadRequest), "bad requests are not retried");
            Check.False(PollinationsErrors.Retryable(PollinationsErrorKind.Parse), "parse failures are not retried");

            Check.Equal(0.75, PollinationsErrors.BackoffSeconds(0), "first backoff");
            Check.Equal(1.5, PollinationsErrors.BackoffSeconds(1), "second backoff");
            Check.Equal(3.0, PollinationsErrors.BackoffSeconds(2), "third backoff");
            Check.Equal(8.0, PollinationsErrors.BackoffSeconds(9), "backoff is capped");
            Check.Equal(0.5, PollinationsErrors.BackoffSeconds(0, 0.5), "custom base");

            Check.Equal("rate_limit", PollinationsErrors.KindName(PollinationsErrorKind.RateLimit), "kind names");
            Check.Contains(PollinationsErrors.Message(PollinationsErrorKind.Balance), "pollen", "the balance message mentions pollen");
            Check.Contains(PollinationsErrors.Message(PollinationsErrorKind.Auth), "key", "the auth message mentions the key");
            Check.Contains(PollinationsErrors.Describe(PollinationsErrorKind.Server, 500, "boom"), "status=500", "describe carries the status");
            Check.Contains(PollinationsErrors.Describe(PollinationsErrorKind.Server, 500, "boom"), "boom", "describe carries the detail");
            Check.Equal("", PollinationsErrors.Message(PollinationsErrorKind.None), "no message for success");
        }
    }
}

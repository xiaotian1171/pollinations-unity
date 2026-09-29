using System;

namespace Pollinations
{
    /// <summary>What went wrong, in terms a game can act on.</summary>
    public enum PollinationsErrorKind
    {
        None = 0,
        Auth = 1,
        Balance = 2,
        RateLimit = 3,
        BadRequest = 4,
        Server = 5,
        Network = 6,
        Parse = 7,
        Unknown = 8,
    }

    public static class PollinationsErrors
    {
        /// <summary>Classifies a response. <paramref name="transportFailed"/> means the request never reached the API.</summary>
        public static PollinationsErrorKind Classify(int statusCode, string body, bool transportFailed = false, bool parseFailed = false)
        {
            if (parseFailed)
            {
                return PollinationsErrorKind.Parse;
            }

            if (transportFailed || statusCode == 0)
            {
                return PollinationsErrorKind.Network;
            }

            if (statusCode == 401 || statusCode == 403)
            {
                return PollinationsErrorKind.Auth;
            }

            if (statusCode == 402)
            {
                return PollinationsErrorKind.Balance;
            }

            // A drained balance can also surface as 400 or 500 with an explanatory body.
            if (statusCode >= 400 && MentionsBalance(body))
            {
                return PollinationsErrorKind.Balance;
            }

            if (statusCode == 429)
            {
                return PollinationsErrorKind.RateLimit;
            }

            if (statusCode >= 400 && statusCode < 500)
            {
                return PollinationsErrorKind.BadRequest;
            }

            if (statusCode >= 500)
            {
                return PollinationsErrorKind.Server;
            }

            if (statusCode >= 200 && statusCode < 300)
            {
                return PollinationsErrorKind.None;
            }

            return PollinationsErrorKind.Unknown;
        }

        public static bool Retryable(PollinationsErrorKind kind)
        {
            return kind == PollinationsErrorKind.RateLimit
                || kind == PollinationsErrorKind.Server
                || kind == PollinationsErrorKind.Network
                || kind == PollinationsErrorKind.Unknown;
        }

        /// <summary>0.75s, 1.5s, 3s ... capped, so a burst of failures cannot hammer the API.</summary>
        public static double BackoffSeconds(int attempt, double baseSeconds = 0.75, double capSeconds = 8.0)
        {
            double seconds = baseSeconds * Math.Pow(2.0, Math.Max(0, attempt));
            return Math.Min(seconds, capSeconds);
        }

        public static string KindName(PollinationsErrorKind kind)
        {
            switch (kind)
            {
                case PollinationsErrorKind.None: return "none";
                case PollinationsErrorKind.Auth: return "auth";
                case PollinationsErrorKind.Balance: return "balance";
                case PollinationsErrorKind.RateLimit: return "rate_limit";
                case PollinationsErrorKind.BadRequest: return "bad_request";
                case PollinationsErrorKind.Server: return "server";
                case PollinationsErrorKind.Network: return "network";
                case PollinationsErrorKind.Parse: return "parse";
                default: return "unknown";
            }
        }

        /// <summary>A sentence to show a player.</summary>
        public static string Message(PollinationsErrorKind kind)
        {
            switch (kind)
            {
                case PollinationsErrorKind.None: return "";
                case PollinationsErrorKind.Auth: return "The Pollinations key is missing, expired or was revoked.";
                case PollinationsErrorKind.Balance: return "This key is out of pollen. Add pollen to the account, or let the player pay with their own Pollen through PollinationsAuth.";
                case PollinationsErrorKind.RateLimit: return "Too many requests right now. Try again in a moment.";
                case PollinationsErrorKind.BadRequest: return "The request was rejected. Check the model id and the parameters.";
                case PollinationsErrorKind.Server: return "Pollinations or the model behind it had a problem. Try again.";
                case PollinationsErrorKind.Network: return "The API could not be reached. Check the connection.";
                case PollinationsErrorKind.Parse: return "The API answered with something this package could not read.";
                default: return "The request failed.";
            }
        }

        public static string Describe(PollinationsErrorKind kind, int statusCode = 0, string detail = "")
        {
            string message = "[pollinations] " + KindName(kind);
            if (statusCode != 0)
            {
                message += " (status=" + statusCode + ")";
            }

            if (!string.IsNullOrEmpty(detail))
            {
                message += ": " + detail;
            }

            return message;
        }

        private static bool MentionsBalance(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return false;
            }

            string lowered = body.ToLowerInvariant();
            return lowered.Contains("insufficient balance")
                || lowered.Contains("insufficient_balance")
                || lowered.Contains("no pollen")
                || lowered.Contains("out of pollen");
        }
    }
}

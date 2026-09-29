# Changelog

## 1.0.0

First release.

- `PollinationsText`, `PollinationsImage`, `PollinationsSpeech`, `PollinationsAuth` and
  `PollinationsCatalog` components, with `UnityEvent` hooks for the inspector.
- The BYOP device sign-in flow: a player approves a short code and their own pollen
  pays for what their game generates.
- Retries with exponential backoff, a stable error classification and player-facing
  messages.
- Pure C# core (own JSON reader, URL builder, catalogue parsing, WAV decoder) covered
  by a headless test suite.

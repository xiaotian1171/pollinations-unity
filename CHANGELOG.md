# Changelog

## Unreleased

- The live check takes `POLLINATIONS_TEST_SPEECH_MODEL`, so `--live` can show speech
  generation succeeding on an account whose pollen is the ordinary kind rather than
  the paid kind.
- README: the speech limit now names the models that bill to paid pollen instead of
  saying that all speech models do.

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

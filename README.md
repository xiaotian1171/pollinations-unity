# Pollinations for Unity

Text, image and speech generation inside a Unity game, through the
[Pollinations](https://pollinations.ai) API. Includes a **device sign-in flow** so
each player can pay with their own Pollen instead of your key.

```csharp
using Pollinations;
using UnityEngine;

public class Miller : MonoBehaviour
{
    private async void Start()
    {
        var text = gameObject.AddComponent<PollinationsText>();
        string answer = await text.GenerateTextAsync("Describe a watermill in one sentence.");
        Debug.Log(answer);
    }
}
```

## What you get

| Component | What it does |
| --- | --- |
| `PollinationsText` | Chat and single-prompt text generation |
| `PollinationsImage` | Image generation, decoded into a `Texture2D` |
| `PollinationsSpeech` | Speech, music and sound effects, decoded into an `AudioClip` |
| `PollinationsAuth` | Device sign-in so players pay with their own Pollen (BYOP) |
| `PollinationsCatalog` | The **live** model catalogue, so a game offers whatever exists today |
| `PollinationsClient` | One retrying HTTP layer shared by all of the above |

Everything is async/await, every component exposes `UnityEvent` hooks for the
inspector, and the logic lives in a pure C# core (own JSON reader, URL builder,
catalogue parser, WAV decoder) that is covered by a headless test suite.

## Requirements

- Unity **2021.3** or newer. Built-in uGUI is a package dependency; nothing else.
- No JSON library, no external package, no native plugin.
- The API key is read from, in order: a key typed in the inspector, a session key
  (the device flow writes one), `PlayerPrefs` when "remember" was used, then the
  `POLLINATIONS_API_KEY` environment variable.

## Install

**From the Package Manager (recommended)**

1. `Window > Package Manager > + > Add package from git URL...`
2. Paste `https://github.com/xiaotian1171/pollinations-unity.git` and press Add.
3. Optional: import the demo sample from the package page (see *The demo*).

**By copying the folder**

Copy `Runtime/` into your project as `Assets/Pollinations/` (keep the `.asmdef`
with it). Nothing else is needed.

**By path, for a fork or a local checkout**

Add `"com.xiaotian1171.pollinations": "file:../pollinations-unity"` to
`Packages/manifest.json` next to the other dependencies.

Then set the app key your game signs players in with:

```csharp
// once, at startup - or use POLLINATIONS_APP_KEY in the environment
PollinationsConfig.UseKeyStore(new UnityPlayerPrefsKeyStore());
```

## Quick start

### Text

```csharp
var text = gameObject.AddComponent<PollinationsText>();
text.SystemPrompt = "You are the miller of a small village. Answer in one sentence.";
text.Model = "nova-fast";                              // or anything from the live catalogue

PollinationsResult result = await text.GenerateAsync("What is the mill like?");
if (result.Ok)
{
    label.text = PollinationsChat.ExtractText(result.Json());   // result.Text holds the raw body
}
else
{
    label.text = result.Message;                                // a sentence for a player
}
```

`QuickAsync(prompt, seed)` uses the cheaper `GET /text/{prompt}` route for signs,
hints and one-line flavour text. `ChatAsync(messages)` sends a whole conversation,
so a character can remember the last few turns.

### Image

```csharp
var image = gameObject.AddComponent<PollinationsImage>();
image.Width = 512;
image.Height = 512;

PollinationsResult result = await image.GenerateAsync("a wooden watermill, flat illustration");
if (result.Ok)
{
    GetComponent<Renderer>().material.mainTexture = image.Texture;   // Texture2D
}
```

### Speech

```csharp
var speech = gameObject.AddComponent<PollinationsSpeech>();
speech.Voice = "alloy";
speech.ResponseFormat = "wav";                          // wav decodes inside the package

PollinationsResult result = await speech.GenerateAsync("Welcome to Pollen Village, traveller.");
if (result.Ok)
{
    audioSource.clip = speech.Clip;
    audioSource.Play();
}
```

`mp3` and `ogg` work too: the bytes are parked in `Application.temporaryCachePath`
and decoded by Unity's own audio loader, because Unity cannot build an `AudioClip`
from raw compressed bytes.

### Live model catalogue

```csharp
var catalog = gameObject.AddComponent<PollinationsCatalog>();
catalog.Endpoint = "/v1/chat/completions";              // only the models that accept chat
catalog.onModel.AddListener(id => dropdown.options.Add(new Dropdown.OptionData(id)));

await catalog.FetchAsync("text");                       // text, image, audio or embeddings
foreach (string id in catalog.Ids())
{
    Debug.Log(id);
}
```

### Sign-in with the player's own Pollen (BYOP)

A shipped game must not contain a secret: anyone can extract a key from a build and
spend your pollen. Instead, let the player sign in once. The game shows a short code,
the player approves it in their browser, and the game receives a key that belongs to
that player - their own balance pays for what their game generates.

```csharp
var auth = gameObject.AddComponent<PollinationsAuth>();
auth.onCodeReady.AddListener((code, url) => ShowSignInPanel(code, url));   // https://enter.pollinations.ai/device
auth.onSignedIn.AddListener(username => Debug.Log("signed in as " + username));
auth.onFailed.AddListener(error => ShowError(error));

// from a "Sign in" button
await auth.SignInAsync();
```

Under the hood the component asks `/api/device/code` for a code, polls
`/api/device/token` every 5 seconds until the player approves, stores the key on the
device when "remember" is on, and reads `/api/device/userinfo` for the player's name.
`SignOut()` forgets the key; the player can also revoke it on the Pollinations site.

## Error handling

Every call returns a `PollinationsResult` with `Ok`, `Kind`, `StatusCode`, `Text`,
`Bytes`, `ContentType`, `Attempts`, `Seconds`, `Error` and `Message`. `Kind` is one of:

| Kind | Meaning | Retried |
| --- | --- | --- |
| `None` | Success | - |
| `Auth` | Missing, invalid or revoked key | no |
| `Balance` | Key valid, no pollen left (`402`, or a body that says so) | no |
| `RateLimit` | `429` | yes, with backoff |
| `BadRequest` | Unknown model id, bad parameters | no |
| `Server` | `5xx` from Pollinations or the upstream model | yes, with backoff |
| `Network` | DNS, TLS, timeout, dead connection | yes, with backoff |
| `Parse` | `2xx` with a payload that cannot be used | no |

`PollinationsErrors.Message(kind)` returns a sentence you can put in front of a
player. Rate limits, server errors and network failures are retried twice by default
(`maxRetries`), waiting 0.75 s and then 1.5 s. A component logs failures once with the
classification, and logs successes only when `logRequests` is on.

## The demo

Import the **Demo** sample from the package page in the Package Manager (or copy
`Samples~/Demo/PollinationsDemo.cs` into your project), add `PollinationsDemo` to an
empty GameObject in an empty scene, and press Play. The demo creates the five
components, builds a small overlay in code and shows a sentence, an image and a
spoken line coming back from the API. Its public methods (`GenerateText`,
`GenerateImage`, `Speak`, `LoadCatalog`, `SignIn`, `SignOut`) can be wired to buttons
in your own UI.

The demo builds its overlay in code rather than shipping a `.unity` scene: a scene
asset only works if its component references line up with your project, while a
script that builds itself runs anywhere.

## Tests

The core is plain C#, so it is tested without an editor. From the repository root:

```bash
# 1. the offline suite: no network, no keys, no Unity
dotnet run --project tests~/Pollinations.Core.Tests        # 317 checks

# 2. the live check: talks to the real API, needs a key for the signed steps
POLLINATIONS_API_KEY=sk_... dotnet run --project tests~/Pollinations.Core.Tests -- --live

# 3. compile the Unity facing code against minimal UnityEngine stubs
dotnet build tests~/Pollinations.Unity.Compile
```

The offline suite covers JSON parsing and writing, URL building, error
classification, the catalogue parser, key resolution, the retry policy through a
scripted transport, chat payloads and answer extraction, the whole device flow with
scripted `authorization_pending` answers, WAV decoding, and response body handling.
Both projects run in CI on pull requests - see `.github/workflows/tests.yml`.

`--live` is the evidence script: it prints real status codes, latencies and payload
shapes. A run against the live API produced, among others:

- `chat` `200` in 2.30 s, model `us.amazon.nova-micro-v1:0`, usage
  `{"prompt_tokens":12,"completion_tokens":28,"total_tokens":40}`
- the prompt route `200` in 3.01 s with a plain answer
- `image` `200` in 2.96 s: `image/jpeg`, 28 813 bytes
- catalogues: 110 text, 18 image and 4 audio models
- `speech` `402` on a free account, classified as a balance failure with the message
  the API sent (speech models need paid pollen)
- the device flow: `POST /api/device/code` `200` with `user_code`, `interval=5`,
  `expires_in=1800`, then `POST /api/device/token` `400`
  `{"error":"authorization_pending"}` while nobody had approved

## Layout

```
Runtime/
  Core/                 pure C#, no UnityEngine types, fully testable
    PollinationsJson.cs        own JSON reader and writer
    PollinationsUrls.cs        every URL in one place
    PollinationsErrors.cs      classification, backoff, player-facing messages
    PollinationsModels.cs      the live catalogue
    PollinationsChat.cs        chat payloads and answer extraction
    PollinationsDeviceFlow.cs  the BYOP flow, parsing half
    PollinationsClient.cs      retries and the transport seam
    PollinationsWav.cs         WAV decoding for AudioClip.Create
    PollinationsTransport.cs   request/response types and IPollinationsTransport
    PollinationsConfig.cs      key resolution and defaults
  Unity/                the Unity facing layer
    UnityWebRequestTransport.cs, UnityPlayerPrefsKeyStore.cs
    PollinationsBehaviour.cs and the five components
Samples~/Demo/          the runnable demo
tests~/                 the offline suite, the live check and the compile stubs
```

Folders ending in `~` are ignored by Unity, which is why the test harness can live
inside the package without ever reaching a build.

## Notes and limits

- **Never ship a server-side key** in a released game. Use the device flow, or a key
  the player pastes.
- Speech models require **paid pollen**; text and image models answered on a free
  account in the run above.
- Anonymous requests are rate limited, and were answered `200` and `401` at different
  times; treat a key as required.
- `Task.Delay` and threads are unavailable on WebGL, so the device flow should be
  driven by a coroutine there, or the player can paste a key instead.
- On WebGL the browser enforces CORS on `gen.pollinations.ai`; if your origin is not
  allowed, route the requests through your own backend. Desktop, mobile and console
  builds are not affected.
- The audio catalogue sometimes lists only transcription models; filter with
  `Endpoint = "/v1/audio/speech"` and keep a default model as a fallback.

## License

MIT, see [LICENSE.md](LICENSE.md).

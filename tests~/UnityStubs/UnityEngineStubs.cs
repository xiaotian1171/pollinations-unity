// Minimal stand-ins for the UnityEngine API this package touches.
//
// They exist so the Unity-facing code can be compiled by the .NET SDK outside an
// editor, which catches syntax and name mistakes. Signatures follow the Unity
// 2021.3 scripting reference. This file is never part of the package and is not
// a substitute for opening the project in Unity.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object : IDisposable
    {
        public string name;
        public HideFlags hideFlags;

        public static void Destroy(Object target) { }
        public static void Destroy(Object target, float delay) { }
        public static void DestroyImmediate(Object target) { }

        public static implicit operator bool(Object value) { return !ReferenceEquals(value, null); }

        public void Dispose() { }
    }

    public enum HideFlags { None = 0 }

    public class Component : Object
    {
        public GameObject gameObject { get { return null; } }
        public Transform transform { get { return null; } }
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T GetComponentInChildren<T>() { return default(T); }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get { return true; } }
    }

    public class MonoBehaviour : Behaviour
    {
        public bool useGUILayout { get; set; }
        public void StartCoroutine(System.Collections.IEnumerator routine) { }
        public void StopAllCoroutines() { }
        public void StopCoroutine(System.Collections.IEnumerator routine) { }
        public void CancelInvoke() { }
        public void Invoke(string methodName, float time) { }
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }

        public Transform transform { get { return null; } }
        public bool activeSelf { get { return true; } }
        public bool activeInHierarchy { get { return true; } }

        public T AddComponent<T>() where T : Component { return default(T); }
        public Component AddComponent(Type componentType) { return null; }
        public T GetComponent<T>() { return default(T); }
        public void SetActive(bool value) { }
        public static GameObject Find(string name) { return null; }
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Transform parent { get; set; }
        public int childCount { get { return 0; } }
        public void SetParent(Transform value) { }
        public void SetParent(Transform value, bool worldPositionStays) { }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
    }

    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0f, 0f); } }
        public static Vector2 one { get { return new Vector2(1f, 1f); } }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0f, 0f, 0f); } }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1f, 1f, 1f, 1f); } }
        public static Color black { get { return new Color(0f, 0f, 0f, 1f); } }
    }

    public static class Mathf
    {
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static float Clamp(float value, float min, float max) { return value; }
        public static float Clamp01(float value) { return value; }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public static class PlayerPrefs
    {
        public static string GetString(string key, string defaultValue) { return defaultValue; }
        public static void SetString(string key, string value) { }
        public static void DeleteKey(string key) { }
        public static void Save() { }
    }

    public static class Application
    {
        public static string temporaryCachePath { get { return "/tmp"; } }
        public static string persistentDataPath { get { return "/tmp"; } }
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object { return default(T); }
    }

    public class Font : Object { }

    public class Texture : Object { }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public int width { get { return 0; } }
        public int height { get { return 0; } }
    }

    public enum TextureFormat { RGBA32 = 4, RGB24 = 3 }

    public static class ImageConversion
    {
        public static bool LoadImage(Texture2D texture, byte[] data) { return false; }
        public static bool LoadImage(Texture2D texture, byte[] data, bool markNonReadable) { return false; }
    }

    public class AudioClip : Object
    {
        public float length { get { return 0f; } }
        public int channels { get { return 0; } }
        public int frequency { get { return 0; } }
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) { return null; }
        public bool SetData(float[] data, int offsetSamples) { return false; }
    }

    public enum AudioType
    {
        UNKNOWN = 0, ACC = 1, AIFF = 2, IT = 10, MOD = 12, MPEG = 13, OGGVORBIS = 14, S3M = 17, WAV = 20, XM = 21
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public bool playOnAwake { get; set; }
        public bool loop { get; set; }
        public float volume { get; set; }
        public void Play() { }
        public void Stop() { }
    }

    public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }

    public enum TextAnchor
    {
        UpperLeft = 0, UpperCenter = 1, UpperRight = 2,
        MiddleLeft = 3, MiddleCenter = 4, MiddleRight = 5,
        LowerLeft = 6, LowerCenter = 7, LowerRight = 8
    }

    public enum HorizontalWrapMode { Wrap = 0, Overflow = 1 }

    public enum VerticalWrapMode { Truncate = 0, Overflow = 1 }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
    }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize = 0, ScaleWithScreenSize = 1, ConstantPhysicalSize = 2 }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
    }

    public class GraphicRaycaster : Behaviour { }

    public class AsyncOperation : Object
    {
        public event Action<AsyncOperation> completed;
        public bool isDone { get { return false; } }
        public float progress { get { return 0f; } }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class AddComponentMenu : Attribute
    {
        public AddComponentMenu(string menuName) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class TextAreaAttribute : Attribute
    {
        public TextAreaAttribute() { }
        public TextAreaAttribute(int minLines, int maxLines) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class HideInInspector : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class SpaceAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class RequireComponent : Attribute
    {
        public RequireComponent(Type type) { }
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);
    public delegate void UnityAction<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2);

    public class UnityEventBase
    {
        public void RemoveAllListeners() { }
    }

    public class UnityEvent : UnityEventBase
    {
        public void AddListener(UnityAction call) { }
        public void RemoveListener(UnityAction call) { }
        public void Invoke() { }
    }

    public class UnityEvent<T0> : UnityEventBase
    {
        public void AddListener(UnityAction<T0> call) { }
        public void RemoveListener(UnityAction<T0> call) { }
        public void Invoke(T0 arg0) { }
    }

    public class UnityEvent<T0, T1> : UnityEventBase
    {
        public void AddListener(UnityAction<T0, T1> call) { }
        public void RemoveListener(UnityAction<T0, T1> call) { }
        public void Invoke(T0 arg0, T1 arg1) { }
    }

    public class UnityEvent<T0, T1, T2> : UnityEventBase
    {
        public void AddListener(UnityAction<T0, T1, T2> call) { }
        public void Invoke(T0 arg0, T1 arg1, T2 arg2) { }
    }
}

namespace UnityEngine.UI
{
    public class Graphic : MonoBehaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
    }

    public class Text : Graphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool resizeTextForBestFit { get; set; }
    }

    public class RawImage : Graphic
    {
        public Texture texture { get; set; }
    }

    public class Image : Graphic
    {
        public Sprite sprite { get; set; }
    }

    public class Sprite : Object { }

    public class Button : MonoBehaviour
    {
        public class ButtonClickedEvent : UnityEngine.Events.UnityEvent { }
        public ButtonClickedEvent onClick { get { return null; } }
    }

    public class InputField : MonoBehaviour
    {
        public string text { get; set; }
    }
}

namespace UnityEngine.Networking
{
    public class DownloadHandler : IDisposable
    {
        public byte[] data { get { return null; } }
        public string text { get { return ""; } }
        public void Dispose() { }
    }

    public class DownloadHandlerBuffer : DownloadHandler { }

    public class UploadHandler : IDisposable
    {
        public string contentType { get; set; }
        public void Dispose() { }
    }

    public class UploadHandlerRaw : UploadHandler
    {
        public UploadHandlerRaw(byte[] data) { }
    }

    public class DownloadHandlerAudioClip : DownloadHandler
    {
        public bool streamAudio { get; set; }
        public static AudioClip GetContent(UnityWebRequest request) { return null; }
    }

    public class UnityWebRequestAsyncOperation : AsyncOperation { }

    public class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress = 0, Success = 1, ConnectionError = 2, ProtocolError = 3, DataProcessingError = 4 }

        public UnityWebRequest() { }
        public UnityWebRequest(string url) { }
        public UnityWebRequest(string url, string method) { }
        public UnityWebRequest(string url, string method, DownloadHandler downloadHandler, UploadHandler uploadHandler) { }

        public string url { get; set; }
        public string method { get; set; }
        public int timeout { get; set; }
        public DownloadHandler downloadHandler { get; set; }
        public UploadHandler uploadHandler { get; set; }
        public long responseCode { get { return 200; } }
        public Result result { get { return Result.Success; } }
        public string error { get { return null; } }
        public bool isDone { get { return true; } }

        public void SetRequestHeader(string name, string value) { }
        public string GetResponseHeader(string name) { return null; }
        public Dictionary<string, string> GetResponseHeaders() { return new Dictionary<string, string>(); }
        public UnityWebRequestAsyncOperation SendWebRequest() { return null; }
        public void Abort() { }
        public void Dispose() { }

        public static UnityWebRequest Get(string uri) { return new UnityWebRequest(uri, "GET"); }
        public static UnityWebRequest Post(string uri, string postData) { return new UnityWebRequest(uri, "POST"); }
    }

    public static class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string uri, AudioType audioType)
        {
            return new UnityWebRequest(uri, "GET");
        }
    }
}

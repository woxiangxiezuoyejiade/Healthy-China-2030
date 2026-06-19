using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialVideoDisplay : MonoBehaviour
{
    const string VideoFolder = "TutorialVideos";

    [Header("Placement")]
    public bool attachToMainCamera = true;
    public Vector3 cameraLocalPosition = new Vector3(-0.62f, 0.04f, 1.18f);
    public Vector3 worldPosition = new Vector3(-1.1f, 1.45f, 1.45f);
    public Vector2 panelSize = new Vector2(250f, 445f);
    public float panelScale = 0.00145f;

    [Header("Look")]
    public Color frameColor = new Color(1f, 1f, 1f, 0.72f);
    public Color backgroundColor = new Color(0.04f, 0.12f, 0.13f, 0.68f);
    public int renderTextureWidth = 720;
    public int renderTextureHeight = 1280;

    GameObject root;
    RawImage videoImage;
    VideoPlayer videoPlayer;
    AudioSource audioSource;
    RenderTexture renderTexture;
    Coroutine autoStopRoutine;

    public bool IsPlaying { get; private set; }

    public IEnumerator PlayUntilFinished(string videoFileName)
    {
        Play(videoFileName, 0f);
        while (IsPlaying)
        {
            yield return null;
        }
    }

    public void Play(string videoFileName, float maxDuration)
    {
        if (string.IsNullOrWhiteSpace(videoFileName))
        {
            Stop();
            return;
        }

        EnsureView();

        string url = Application.streamingAssetsPath.TrimEnd('/', '\\') + "/" + VideoFolder + "/" + videoFileName;
        root.SetActive(true);
        videoPlayer.Stop();
        videoPlayer.url = url;
        videoPlayer.isLooping = false;
        IsPlaying = true;
        videoPlayer.Play();

        if (autoStopRoutine != null) StopCoroutine(autoStopRoutine);
        if (maxDuration > 0f)
        {
            autoStopRoutine = StartCoroutine(AutoStopRoutine(maxDuration));
        }
    }

    public void Stop()
    {
        if (autoStopRoutine != null)
        {
            StopCoroutine(autoStopRoutine);
            autoStopRoutine = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        IsPlaying = false;

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    IEnumerator AutoStopRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        Stop();
    }

    void EnsureView()
    {
        if (root != null)
        {
            UpdatePlacement();
            return;
        }

        root = new GameObject("Tutorial_Video_Display");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 60;
        root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = panelSize;
        rootRect.localScale = Vector3.one * panelScale;

        GameObject backgroundObject = new GameObject("Video_Background");
        backgroundObject.transform.SetParent(root.transform, false);
        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image background = backgroundObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;
        Outline outline = backgroundObject.AddComponent<Outline>();
        outline.effectColor = frameColor;
        outline.effectDistance = new Vector2(4f, -4f);

        GameObject videoObject = new GameObject("Video_Image");
        videoObject.transform.SetParent(root.transform, false);
        RectTransform videoRect = videoObject.AddComponent<RectTransform>();
        videoRect.anchorMin = Vector2.zero;
        videoRect.anchorMax = Vector2.one;
        videoRect.offsetMin = new Vector2(8f, 8f);
        videoRect.offsetMax = new Vector2(-8f, -8f);
        videoImage = videoObject.AddComponent<RawImage>();
        videoImage.color = Color.white;
        videoImage.raycastTarget = false;

        renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        videoImage.texture = renderTexture;

        videoPlayer = root.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.loopPointReached += HandleVideoFinished;
        videoPlayer.errorReceived += HandleVideoError;

        audioSource = root.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.65f;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        UpdatePlacement();
        root.SetActive(false);
    }

    void UpdatePlacement()
    {
        Transform parent = null;
        Camera mainCamera = Camera.main;
        if (attachToMainCamera && mainCamera != null)
        {
            parent = mainCamera.transform;
        }

        root.transform.SetParent(parent, false);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * panelScale;
        root.GetComponent<RectTransform>().sizeDelta = panelSize;

        if (parent != null)
        {
            root.transform.localPosition = cameraLocalPosition;
        }
        else
        {
            root.transform.position = worldPosition;
            root.transform.rotation = Quaternion.identity;
        }
    }

    void OnDisable()
    {
        Stop();
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    void HandleVideoFinished(VideoPlayer player)
    {
        Stop();
    }

    void HandleVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning("Tutorial video failed to play: " + message);
        Stop();
    }
}

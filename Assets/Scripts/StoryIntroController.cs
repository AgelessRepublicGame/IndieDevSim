using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;

[System.Serializable]
public class StoryIntroPage
{
    public string name;
    public Sprite image;
    public VideoClip videoClip;
    public string videoUrl;
    public bool loopVideo = true;
    [TextArea(3, 8)] public string text;
}

public class StoryIntroController : MonoBehaviour
{
    public const string StoryIntroCompleteKey = "StoryIntroComplete";

    [Header("Media")]
    [SerializeField] private StoryIntroPage[] pages;
    [SerializeField] private Image imageDisplay;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private CanvasGroup mediaCanvasGroup;
    [SerializeField] private Image blackFadeOverlay;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource videoAudioSource;
    [SerializeField] private bool createMissingMediaUi = true;
    [SerializeField] private Color fallbackBackgroundColor = Color.black;
    [SerializeField] private Color transitionBlackColor = Color.black;

    [Header("Text")]
    [SerializeField] private TMP_Text storyText;

    [Header("Controls")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private string officeSceneName = "Workspace";

    [Header("Animation")]
    [SerializeField] private float charactersPerSecond = 45f;
    [SerializeField] private float transitionSeconds = 0.35f;
    [SerializeField] private float blackFadeSeconds = 0.35f;
    [SerializeField] private float blackHoldSeconds = 0.08f;
    [SerializeField] private bool autoAdvanceWhenVideoEnds = true;
    [SerializeField] private float videoEndFadeSeconds = 0.35f;
    [SerializeField] private float mediaStartScale = 1.06f;
    [SerializeField] private float mediaEndScale = 1f;

    [Header("Legacy Text Pages")]
    [TextArea(3, 8)]
    [SerializeField] private string[] storyPages =
    {
       "The early morning sun filters through the dusty windows of your cramped garage. Cardboard boxes, old monitors, and scattered cables form a chaotic nest around you. This humble space half office, half studio, half home is where your dreams of making games are born.",
       "Today, a crisp envelope lands on your workbench, the manager’s letter inside brimming with opportunity. A small project, a tiny prototype—but it’s enough to set the wheels in motion.",
       "You grasp the flash drive, its contents a promise of possibility. Every click, every line of code, every completed order is a step toward transforming this cluttered garage into a real, thriving game studio. The journey starts here."
    };

    private int pageIndex;
    private Coroutine typingRoutine;
    private Coroutine transitionRoutine;
    private Coroutine blackFadeRoutine;
    private Coroutine videoEndRoutine;
    private bool isTyping;
    private RectTransform mediaRoot;
    private bool waitingForFirstVideoFrame;

    private void Awake()
    {
        EnsureMediaReferences();

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(Next);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(FinishStory);
        }
    }

    private void Start()
    {
        pageIndex = 0;
        ShowCurrentPage();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(Next);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(FinishStory);
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandleVideoPrepared;
            videoPlayer.frameReady -= HandleVideoFrameReady;
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.Stop();
        }
    }

    public void Next()
    {
        if (GetPageCount() == 0)
        {
            FinishStory();
            return;
        }

        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        pageIndex++;
        if (pageIndex >= GetPageCount())
        {
            FinishStory();
            return;
        }

        ShowCurrentPage();
    }

    public void FinishStory()
    {
        PlayerPrefs.SetInt(StoryIntroCompleteKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(officeSceneName);
    }

    private void ShowCurrentPage()
    {
        StoryIntroPage page = GetCurrentPage();

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        ShowMedia(page);

        string text = page != null
            ? page.text
            : GetLegacyPageText(pageIndex);

        if (storyText != null)
        {
            typingRoutine = StartCoroutine(TypePage(text));
        }
    }

    private void ShowMedia(StoryIntroPage page)
    {
        EnsureMediaReferences();
        ShowBlackOverlay();
        waitingForFirstVideoFrame = false;

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandleVideoPrepared;
            videoPlayer.frameReady -= HandleVideoFrameReady;
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.Stop();
        }

        if (videoEndRoutine != null)
        {
            StopCoroutine(videoEndRoutine);
            videoEndRoutine = null;
        }

        bool hasVideo = page != null
            && (page.videoClip != null || !string.IsNullOrWhiteSpace(page.videoUrl));
        bool hasImage = page != null && page.image != null;

        if (imageDisplay != null)
        {
            imageDisplay.gameObject.SetActive(!hasVideo);
            imageDisplay.sprite = hasImage ? page.image : null;
            imageDisplay.color = hasImage ? Color.white : fallbackBackgroundColor;
        }

        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(hasVideo);
            if (hasVideo)
            {
                videoDisplay.texture = null;
                videoDisplay.color = Color.clear;
            }
        }

        if (hasVideo)
        {
            PlayVideo(page);
        }
        else
        {
            StartBlackFadeOut();
        }

        PlayMediaTransition();
    }

    private void PlayVideo(StoryIntroPage page)
    {
        if (videoPlayer == null || page == null)
        {
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = page.loopVideo && !autoAdvanceWhenVideoEnds;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.source = page.videoClip != null
            ? VideoSource.VideoClip
            : VideoSource.Url;
        videoPlayer.clip = page.videoClip;
        videoPlayer.url = page.videoClip == null ? page.videoUrl : "";
        videoPlayer.time = 0d;
        videoPlayer.frame = 0;

        if (videoAudioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        }

        waitingForFirstVideoFrame = true;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.frameReady -= HandleVideoFrameReady;
        videoPlayer.frameReady += HandleVideoFrameReady;
        videoPlayer.prepareCompleted -= HandleVideoPrepared;
        videoPlayer.prepareCompleted += HandleVideoPrepared;
        videoPlayer.loopPointReached -= HandleVideoFinished;
        videoPlayer.loopPointReached += HandleVideoFinished;
        videoPlayer.Prepare();
    }

    private void HandleVideoFinished(VideoPlayer finishedPlayer)
    {
        if (!autoAdvanceWhenVideoEnds || videoEndRoutine != null)
        {
            return;
        }

        videoEndRoutine = StartCoroutine(AdvanceAfterVideoEndFade());
    }

    private IEnumerator AdvanceAfterVideoEndFade()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;
        yield return FadeBlackOverlayIn();
        videoEndRoutine = null;
        Next();
    }

    private void HandleVideoPrepared(VideoPlayer preparedPlayer)
    {
        if (videoDisplay != null)
        {
            videoDisplay.texture = preparedPlayer.texture;
        }

        preparedPlayer.time = 0d;
        preparedPlayer.frame = 0;
        preparedPlayer.Play();
    }

    private void HandleVideoFrameReady(VideoPlayer source, long frameIdx)
    {
        if (!waitingForFirstVideoFrame)
        {
            return;
        }

        waitingForFirstVideoFrame = false;
        source.frameReady -= HandleVideoFrameReady;
        source.sendFrameReadyEvents = false;

        if (videoDisplay != null)
        {
            videoDisplay.texture = source.texture;
            videoDisplay.color = Color.white;
        }

        StartBlackFadeOut();
    }

    private void PlayMediaTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        if (mediaRoot == null && imageDisplay != null)
        {
            mediaRoot = imageDisplay.rectTransform.parent as RectTransform;
        }

        if (mediaCanvasGroup == null && mediaRoot != null)
        {
            mediaCanvasGroup = mediaRoot.GetComponent<CanvasGroup>();
        }

        if (mediaRoot == null || mediaCanvasGroup == null)
        {
            return;
        }

        transitionRoutine = StartCoroutine(AnimateMediaTransition());
    }

    private IEnumerator AnimateMediaTransition()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionSeconds);
        mediaCanvasGroup.alpha = 0f;
        mediaRoot.localScale = new Vector3(mediaStartScale, mediaStartScale, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            mediaCanvasGroup.alpha = eased;
            float scale = Mathf.Lerp(mediaStartScale, mediaEndScale, eased);
            mediaRoot.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        mediaCanvasGroup.alpha = 1f;
        mediaRoot.localScale = new Vector3(mediaEndScale, mediaEndScale, 1f);
        transitionRoutine = null;
    }

    private void EnsureMediaReferences()
    {
        if (!createMissingMediaUi)
        {
            if (mediaRoot == null && imageDisplay != null)
            {
                mediaRoot = imageDisplay.rectTransform.parent as RectTransform;
            }
            if (mediaRoot == null && videoDisplay != null)
            {
                mediaRoot = videoDisplay.rectTransform.parent as RectTransform;
            }

            if (mediaCanvasGroup == null && mediaRoot != null)
            {
                mediaCanvasGroup = mediaRoot.GetComponent<CanvasGroup>();
            }

            EnsureVideoPlayer();
            EnsureBlackFadeOverlay();
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(
                "StoryIntroCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (imageDisplay != null && videoDisplay != null)
        {
            mediaRoot = imageDisplay.rectTransform.parent as RectTransform;
            if (mediaCanvasGroup == null && mediaRoot != null)
            {
                mediaCanvasGroup = mediaRoot.GetComponent<CanvasGroup>();
            }

            EnsureVideoPlayer();
            EnsureBlackFadeOverlay();
            return;
        }

        GameObject mediaObject = new GameObject(
            "StoryIntroMedia",
            typeof(RectTransform),
            typeof(CanvasGroup));
        mediaObject.transform.SetParent(canvas.transform, false);
        mediaRoot = mediaObject.GetComponent<RectTransform>();
        mediaRoot.anchorMin = Vector2.zero;
        mediaRoot.anchorMax = Vector2.one;
        mediaRoot.offsetMin = Vector2.zero;
        mediaRoot.offsetMax = Vector2.zero;
        mediaRoot.SetAsFirstSibling();
        mediaCanvasGroup = mediaObject.GetComponent<CanvasGroup>();

        if (imageDisplay == null)
        {
            GameObject imageObject = new GameObject(
                "ImageDisplay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(mediaRoot, false);
            imageDisplay = imageObject.GetComponent<Image>();
            StretchToFullScreen(imageDisplay.rectTransform);
            imageDisplay.color = fallbackBackgroundColor;
            imageDisplay.preserveAspect = false;
        }

        if (videoDisplay == null)
        {
            GameObject videoObject = new GameObject(
                "VideoDisplay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            videoObject.transform.SetParent(mediaRoot, false);
            videoDisplay = videoObject.GetComponent<RawImage>();
            StretchToFullScreen(videoDisplay.rectTransform);
            videoDisplay.color = Color.white;
            videoDisplay.gameObject.SetActive(false);
        }

        EnsureVideoPlayer();
        EnsureBlackFadeOverlay();
    }

    private void EnsureBlackFadeOverlay()
    {
        if (mediaRoot == null)
        {
            return;
        }

        if (blackFadeOverlay == null)
        {
            Transform existing = mediaRoot.Find("Black Fade Overlay");
            if (existing != null)
            {
                blackFadeOverlay = existing.GetComponent<Image>();
            }
        }

        if (blackFadeOverlay == null)
        {
            GameObject overlayObject = new GameObject(
                "Black Fade Overlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            overlayObject.transform.SetParent(mediaRoot, false);
            blackFadeOverlay = overlayObject.GetComponent<Image>();
            StretchToFullScreen(blackFadeOverlay.rectTransform);
        }

        blackFadeOverlay.raycastTarget = false;
        blackFadeOverlay.transform.SetAsLastSibling();
    }

    private void ShowBlackOverlay()
    {
        EnsureBlackFadeOverlay();
        if (blackFadeOverlay == null)
        {
            return;
        }

        if (blackFadeRoutine != null)
        {
            StopCoroutine(blackFadeRoutine);
            blackFadeRoutine = null;
        }

        blackFadeOverlay.gameObject.SetActive(true);
        blackFadeOverlay.transform.SetAsLastSibling();
        Color color = transitionBlackColor;
        color.a = 1f;
        blackFadeOverlay.color = color;
    }

    private void StartBlackFadeOut()
    {
        EnsureBlackFadeOverlay();
        if (blackFadeOverlay == null)
        {
            return;
        }

        if (blackFadeRoutine != null)
        {
            StopCoroutine(blackFadeRoutine);
        }

        blackFadeRoutine = StartCoroutine(FadeBlackOverlayOut());
    }

    private IEnumerator FadeBlackOverlayOut()
    {
        if (blackHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(blackHoldSeconds);
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, blackFadeSeconds);
        Color color = transitionBlackColor;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = 1f - Mathf.Clamp01(elapsed / duration);
            blackFadeOverlay.color = color;
            yield return null;
        }

        color.a = 0f;
        blackFadeOverlay.color = color;
        blackFadeOverlay.gameObject.SetActive(false);
        blackFadeRoutine = null;
    }

    private IEnumerator FadeBlackOverlayIn()
    {
        EnsureBlackFadeOverlay();
        if (blackFadeOverlay == null)
        {
            yield break;
        }

        if (blackFadeRoutine != null)
        {
            StopCoroutine(blackFadeRoutine);
            blackFadeRoutine = null;
        }

        blackFadeOverlay.gameObject.SetActive(true);
        blackFadeOverlay.transform.SetAsLastSibling();

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, videoEndFadeSeconds);
        Color color = transitionBlackColor;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            blackFadeOverlay.color = color;
            yield return null;
        }

        color.a = 1f;
        blackFadeOverlay.color = color;
    }

    private void EnsureVideoPlayer()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
        }

        if (videoAudioSource == null)
        {
            videoAudioSource = GetComponent<AudioSource>();
            if (videoAudioSource == null)
            {
                videoAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private static void StretchToFullScreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private IEnumerator TypePage(string page)
    {
        isTyping = true;
        storyText.text = "";

        if (charactersPerSecond <= 0f)
        {
            storyText.text = page;
            isTyping = false;
            yield break;
        }

        float delay = 1f / charactersPerSecond;
        for (int i = 0; i < page.Length; i++)
        {
            storyText.text += page[i];
            yield return new WaitForSecondsRealtime(delay);
        }

        isTyping = false;
    }

    private void CompleteTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (storyText != null)
        {
            StoryIntroPage page = GetCurrentPage();
            storyText.text = page != null
                ? page.text
                : GetLegacyPageText(pageIndex);
        }

        isTyping = false;
    }

    private int GetPageCount()
    {
        if (pages != null && pages.Length > 0)
        {
            return pages.Length;
        }

        return storyPages != null ? storyPages.Length : 0;
    }

    private StoryIntroPage GetCurrentPage()
    {
        if (pages == null || pages.Length == 0)
        {
            return null;
        }

        return pages[Mathf.Clamp(pageIndex, 0, pages.Length - 1)];
    }

    private string GetLegacyPageText(int index)
    {
        if (storyPages == null || storyPages.Length == 0)
        {
            return "";
        }

        return storyPages[Mathf.Clamp(index, 0, storyPages.Length - 1)];
    }
}

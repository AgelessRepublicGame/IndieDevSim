using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdaptiveDialogWindow : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image background;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.78f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 50f;
    [SerializeField] private Vector2 padding = new Vector2(28f, 35f);
    [SerializeField] [Range(0.2f, 1f)] private float maxWidthPercent = 0.86f;
    [SerializeField] private float bottomOffset = 200f;
    [SerializeField] private float minWidth = 260f;
    [SerializeField] private float minHeight = 70f;
    [SerializeField] private float animationSeconds = 0.12f;

    private static AdaptiveDialogWindow instance;
    private object currentOwner;
    private int currentPriority;
    private Coroutine animationRoutine;

    public static AdaptiveDialogWindow Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AdaptiveDialogWindow>();
            }

            if (instance == null)
            {
                instance = CreateRuntimeWindow();
            }

            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
        EnsureReferences();
        HideImmediate();
    }

    public static void Show(object owner, string message)
    {
        Show(owner, message, 0);
    }

    public static void Show(object owner, string message, int priority)
    {
        if (MissionObjects.IsInMission)
        {
            Instance.HideInternal(Instance.currentOwner);
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Hide(owner);
            return;
        }

        Instance.ShowInternal(owner, message, priority);
    }

    public static void Hide(object owner)
    {
        if (instance == null)
        {
            return;
        }

        if (instance.currentOwner == null || instance.currentOwner == owner)
        {
            instance.HideInternal(owner);
        }
    }

    private static AdaptiveDialogWindow CreateRuntimeWindow()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(
                "AdaptiveDialogCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject root = new GameObject("AdaptiveDialogWindow", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        AdaptiveDialogWindow window = root.AddComponent<AdaptiveDialogWindow>();
        window.EnsureReferences();
        return window;
    }

    private void EnsureReferences()
    {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        if (panel == null)
        {
            GameObject panelObject = new GameObject(
                "Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            panelObject.transform.SetParent(transform, false);
            panel = panelObject.GetComponent<RectTransform>();
            background = panelObject.GetComponent<Image>();
            canvasGroup = panelObject.GetComponent<CanvasGroup>();
        }

        if (background == null)
        {
            background = panel.GetComponent<Image>();
            if (background == null)
            {
                background = panel.gameObject.AddComponent<Image>();
            }
        }

        if (canvasGroup == null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (dialogText == null)
        {
            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel, false);
            dialogText = textObject.GetComponent<TextMeshProUGUI>();
        }

        background.color = backgroundColor;
        background.raycastTarget = false;

        dialogText.color = textColor;
        dialogText.fontSize = fontSize;
        dialogText.enableWordWrapping = true;
        dialogText.alignment = TextAlignmentOptions.Left;
        dialogText.raycastTarget = false;

        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, bottomOffset);

        RectTransform textRect = dialogText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = padding;
        textRect.offsetMax = -padding;
    }

    private void ShowInternal(object owner, string message, int priority)
    {
        EnsureReferences();
        if (currentOwner != null && currentOwner != owner && priority < currentPriority)
        {
            return;
        }

        currentOwner = owner;
        currentPriority = priority;
        dialogText.text = message;
        ResizeToText();
        panel.gameObject.SetActive(true);
        PlayVisibilityAnimation(true);
    }

    private void HideInternal(object owner)
    {
        currentOwner = null;
        currentPriority = 0;
        PlayVisibilityAnimation(false);
    }

    private void ResizeToText()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        float canvasWidth = canvasRect != null ? canvasRect.rect.width : Screen.width;
        float maxWidth = Mathf.Max(minWidth, canvasWidth * maxWidthPercent);

        Vector2 preferred = dialogText.GetPreferredValues(dialogText.text, maxWidth - padding.x * 2f, 0f);
        float width = Mathf.Clamp(preferred.x + padding.x * 2f, minWidth, maxWidth);
        preferred = dialogText.GetPreferredValues(dialogText.text, width - padding.x * 2f, 0f);
        float height = Mathf.Max(minHeight, preferred.y + padding.y * 2f);

        panel.sizeDelta = new Vector2(width, height);
        panel.anchoredPosition = new Vector2(0f, bottomOffset);
    }

    private void PlayVisibilityAnimation(bool show)
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(AnimateVisibility(show));
    }

    private IEnumerator AnimateVisibility(bool show)
    {
        float startAlpha = canvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        Vector3 startScale = panel.localScale;
        Vector3 endScale = show ? Vector3.one : new Vector3(0.98f, 0.98f, 1f);
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, animationSeconds);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            panel.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        panel.localScale = endScale;
        panel.gameObject.SetActive(show);
        animationRoutine = null;
    }

    private void HideImmediate()
    {
        EnsureReferences();
        currentOwner = null;
        currentPriority = 0;
        canvasGroup.alpha = 0f;
        panel.localScale = new Vector3(0.98f, 0.98f, 1f);
        panel.gameObject.SetActive(false);
    }
}

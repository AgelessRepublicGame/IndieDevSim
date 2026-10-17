using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameProgressHud : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image timeFill;
    [SerializeField] private Image taskFill;
    [SerializeField] private TextMeshProUGUI timeLabel;
    [SerializeField] private TextMeshProUGUI taskLabel;
    [SerializeField] private Color timeColor = new Color(0.25f, 0.75f, 1f, 0.95f);
    [SerializeField] private Color taskColor = new Color(0.35f, 1f, 0.45f, 0.95f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -28f);
    [SerializeField] private Vector2 size = new Vector2(520f, 46f);
    [SerializeField] private float barHeight = 8f;

    public static MiniGameProgressHud Attach(GameObject minigameObject)
    {
        if (minigameObject == null)
        {
            return null;
        }

        MiniGameProgressHud existing = minigameObject.GetComponentInChildren<MiniGameProgressHud>(true);
        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = minigameObject.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return null;
        }

        GameObject hudObject = new GameObject("MiniGameProgressHud", typeof(RectTransform));
        hudObject.transform.SetParent(canvas.transform, false);
        MiniGameProgressHud hud = hudObject.AddComponent<MiniGameProgressHud>();
        hud.Build();
        return hud;
    }

    private void Awake()
    {
        Build();
    }

    public void SetProgress(float timePercent, string timeText, float taskPercent, string taskText)
    {
        Build();
        if (timeFill != null)
        {
            ApplyFill(timeFill, timePercent);
        }

        if (taskFill != null)
        {
            ApplyFill(taskFill, taskPercent);
        }

        if (timeLabel != null)
        {
            timeLabel.text = timeText;
        }

        if (taskLabel != null)
        {
            taskLabel.text = taskText;
        }
    }

    private void Build()
    {
        if (root != null)
        {
            return;
        }

        root = transform as RectTransform;
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = anchoredPosition;
        root.sizeDelta = size;
        transform.SetAsLastSibling();

        timeLabel = CreateLabel("Time Label", new Vector2(0f, -2f), "Time");
        RectTransform timeBar = CreateBar("Time Bar", new Vector2(0f, -18f), timeColor, out timeFill);
        taskLabel = CreateLabel("Task Label", new Vector2(0f, -24f), "Progress");
        RectTransform taskBar = CreateBar("Task Bar", new Vector2(0f, -40f), taskColor, out taskFill);
    }

    private TextMeshProUGUI CreateLabel(string objectName, Vector2 anchored, string text)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchored;
        rect.sizeDelta = new Vector2(0f, 16f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 13f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        return label;
    }

    private RectTransform CreateBar(string objectName, Vector2 anchored, Color fillColor, out Image fill)
    {
        GameObject barObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barObject.transform.SetParent(root, false);
        RectTransform rect = barObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchored;
        rect.sizeDelta = new Vector2(0f, barHeight);

        Image background = barObject.GetComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(barObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fill = fillObject.GetComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Simple;
        fill.raycastTarget = false;
        ApplyFill(fill, 1f);
        return rect;
    }

    private void ApplyFill(Image fill, float amount)
    {
        if (fill == null)
        {
            return;
        }

        float clampedAmount = Mathf.Clamp01(amount);
        fill.type = Image.Type.Simple;
        fill.fillAmount = clampedAmount;

        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(clampedAmount, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }
}

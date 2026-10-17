using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class ObjectiveBoardItem
{
    [Header("Content")]
    [TextArea(2, 4)] public string content;
    [TextArea(1, 3)] public string finishCondition;
    [Min(0f)] public float currentProgress;
    [Min(0.01f)] public float requiredProgress = 1f;
    public bool isFinished;
    public bool isVisible = true;

    [Header("Per Objective Style")]
    [Min(1f)] public float rowHeight = 220f;
    [Min(1f)] public float numberFontSize = 42f;
    [Min(1f)] public float contentFontSize = 42f;
    [Min(1f)] public float conditionFontSize = 30f;
    [Min(1f)] public float progressStateFontSize = 28f;
    public Vector2 numberPosition = new Vector2(28f, -28f);
    public Vector2 numberSize = new Vector2(70f, 70f);
    public Vector2 contentPosition = new Vector2(126f, -28f);
    public Vector2 contentSize = new Vector2(-220f, 76f);
    public Vector2 conditionPosition = new Vector2(126f, -108f);
    public Vector2 conditionSize = new Vector2(-220f, 52f);
    public Vector2 progressBarPosition = new Vector2(0f, 20f);
    public Vector2 progressBarSize = new Vector2(-250f, 18f);
    public Vector2 progressStatePosition = new Vector2(-212f, 20f);
    public Vector2 progressStateSize = new Vector2(180f, 42f);

    [Header("Completion Tick")]
    public Sprite tickSprite;
    public Vector2 tickPosition = new Vector2(-42f, 24f);
    public Vector2 tickSize = new Vector2(78f, 78f);

    [Header("Bonus")]
    public bool hasBonus;
    public string bonusButtonText = "Claim";
    public int bonusMoney;
    public float bonusExperience;
    public bool bonusClaimed;
    [Min(1f)] public float bonusButtonFontSize = 30f;
    public Sprite bonusReadySprite;
    public Vector2 bonusButtonPosition = new Vector2(-36f, -64f);
    public Vector2 bonusButtonSize = new Vector2(170f, 58f);
    public Vector2 bonusIconPosition = new Vector2(16f, 0f);
    public Vector2 bonusIconSize = new Vector2(34f, 34f);
    public Vector2 bonusTextOffsetMin = new Vector2(48f, 0f);
    public Vector2 bonusTextOffsetMax = new Vector2(-8f, 0f);
    public UnityEvent onBonusClaimed = new UnityEvent();

    public float Progress01 => isFinished
        ? 1f
        : Mathf.Clamp01(currentProgress / Mathf.Max(0.01f, requiredProgress));
}

public class ObjectiveBoardController : MonoBehaviour
{
    [Header("Objectives")]
    [SerializeField] private ObjectiveBoardItem[] objectives;

    [Header("References")]
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private RectTransform objectiveContainer;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button objectiveBoardButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool createMissingUi = true;
    [SerializeField] private bool showBoardOnStart;

    [Header("Persistence")]
    [SerializeField] private bool persistObjectiveState = true;
    [SerializeField] private string persistenceKey = "Default";

    [Header("Perfect Order Objective")]
    [SerializeField] private bool trackPerfectOrderObjective = true;
    [SerializeField] [Min(1)] private int perfectOrderObjectiveNumber = 1;
    [SerializeField] [Min(1)] private int perfectOrdersRequired = 10;

    [Header("Layout")]
    [SerializeField] private string boardTitle = "Objectives";
    [SerializeField] private Vector2 anchoredPosition = Vector2.zero;
    [SerializeField] private Vector2 boardSize = new Vector2(1000f, 1500f);
    [SerializeField] private Vector2 titlePosition = new Vector2(44f, -34f);
    [SerializeField] private Vector2 titleSize = new Vector2(-88f, 86f);
    [SerializeField] private float titleFontSize = 58f;
    [SerializeField] private Vector2 closeButtonPosition = new Vector2(-36f, -36f);
    [SerializeField] private Vector2 closeButtonSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 objectiveListPosition = new Vector2(44f, -144f);
    [SerializeField] private Vector2 objectiveListSize = new Vector2(-88f, 1312f);
    [SerializeField] private float rowHeight = 220f;
    [SerializeField] private float rowSpacing = 22f;
    [SerializeField] private float progressBarHeight = 18f;

    [Header("Style")]
    [SerializeField] private Sprite defaultTickSprite;
    [SerializeField] private Sprite closeButtonSprite;
    [SerializeField] private Color boardColor = new Color(0f, 0f, 0f, 0.62f);
    [SerializeField] private Color rowColor = new Color(0.06f, 0.07f, 0.08f, 0.82f);
    [SerializeField] private Color numberColor = new Color(0.15f, 0.58f, 1f, 0.95f);
    [SerializeField] private Color progressColor = new Color(0.25f, 0.68f, 1f, 0.95f);
    [SerializeField] private Color progressBackgroundColor = new Color(1f, 1f, 1f, 0.16f);
    [SerializeField] private Color progressStateTextColor = new Color(1f, 1f, 1f, 0.86f);
    [SerializeField] private Color completedProgressStateTextColor = new Color(0.35f, 1f, 0.55f, 0.9f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color conditionColor = new Color(1f, 1f, 1f, 0.72f);
    [SerializeField] private Color finishedColor = new Color(0.35f, 1f, 0.55f, 0.9f);
    [SerializeField] private Color bonusButtonColor = new Color(1f, 0.78f, 0.25f, 0.95f);
    [SerializeField] private Color bonusButtonTextColor = Color.black;
    [SerializeField] private Color bonusReadyIconColor = Color.white;
    [SerializeField] private Color disabledBonusReadyIconColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color closeButtonColor = Color.white;
    [SerializeField] [HideInInspector] private bool largeBoardDefaultsApplied;

    private readonly List<ObjectiveRowView> rowViews = new List<ObjectiveRowView>();
    private bool isBoardVisible;
    private bool isForceHidden;
    private const float PerfectOrderQualityThreshold = 0.999f;

    private class ObjectiveRowView
    {
        public RectTransform Root;
        public TextMeshProUGUI NumberText;
        public TextMeshProUGUI ContentText;
        public TextMeshProUGUI ConditionText;
        public TextMeshProUGUI ProgressStateText;
        public Image ProgressFill;
        public Image NumberBackground;
        public Image TickImage;
        public TextMeshProUGUI TickFallbackText;
        public Button BonusButton;
        public TextMeshProUGUI BonusButtonText;
        public Image BonusReadyIcon;
        public LayoutElement LayoutElement;
    }

    private void Awake()
    {
        Build();
        LoadObjectiveStates();
        SyncPerfectOrderObjectiveFromSavedStats();
        isBoardVisible = showBoardOnStart;
        if (objectiveBoardButton != null)
        {
            objectiveBoardButton.onClick.AddListener(ToggleBoard);
            EnsureObjectiveButtonTransition();
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideBoard);
            closeButton.onClick.AddListener(HideBoard);
        }

        RefreshBoard();
    }

    private void OnEnable()
    {
        DeveloperSimulation.OrderSubmittedDetailed += HandleOrderSubmittedDetailed;
    }

    private void OnDisable()
    {
        DeveloperSimulation.OrderSubmittedDetailed -= HandleOrderSubmittedDetailed;
    }

    private void OnDestroy()
    {
        if (objectiveBoardButton != null)
        {
            objectiveBoardButton.onClick.RemoveListener(ToggleBoard);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideBoard);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        RefreshBoard();
    }

    public void RefreshBoard()
    {
        Build();

        if (boardRoot == null || objectiveContainer == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = boardTitle;
        }

        int objectiveCount = objectives != null ? objectives.Length : 0;
        EnsureRowCount(objectiveCount);

        for (int i = 0; i < rowViews.Count; i++)
        {
            ObjectiveBoardItem objective = i < objectiveCount ? objectives[i] : null;
            bool visible = objective != null && objective.isVisible;
            rowViews[i].Root.gameObject.SetActive(visible);

            if (visible)
            {
                ApplyObjective(rowViews[i], objective, i + 1);
            }
        }

        if (canvasGroup != null)
        {
            bool shouldShow = isBoardVisible && !isForceHidden && HasVisibleObjective();
            canvasGroup.alpha = shouldShow ? 1f : 0f;
            canvasGroup.blocksRaycasts = shouldShow;
            canvasGroup.interactable = shouldShow;
        }
    }

    public void SetForceHidden(bool forceHidden)
    {
        if (isForceHidden == forceHidden)
        {
            return;
        }

        isForceHidden = forceHidden;
        RefreshBoard();
    }

    public void ShowBoard()
    {
        isBoardVisible = true;
        RefreshBoard();
    }

    public void HideBoard()
    {
        isBoardVisible = false;
        RefreshBoard();
    }

    public void ToggleBoard()
    {
        isBoardVisible = !isBoardVisible;
        RefreshBoard();
    }

    private void EnsureObjectiveButtonTransition()
    {
        if (objectiveBoardButton == null)
        {
            return;
        }

        if (objectiveBoardButton.GetComponent<HoverScaleEffect>() == null)
        {
            objectiveBoardButton.gameObject.AddComponent<HoverScaleEffect>();
        }

        objectiveBoardButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = objectiveBoardButton.colors;
        colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(colors.normalColor, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.12f;
        objectiveBoardButton.colors = colors;
    }

    private void HandleOrderSubmittedDetailed(
        DeveloperSimulation developer,
        ActiveOrder order,
        float quality,
        bool overDeadline)
    {
        if (!trackPerfectOrderObjective || quality < PerfectOrderQualityThreshold)
        {
            return;
        }

        ObjectiveBoardItem objective = GetObjectiveByNumber(perfectOrderObjectiveNumber);
        if (objective == null)
        {
            return;
        }

        objective.requiredProgress = Mathf.Max(1f, perfectOrdersRequired);

        int savedPerfectOrders = PlayerPrefs.GetInt("PerfectQualityOrdersCompleted", 0);
        int developerPerfectOrders = developer != null
            ? developer.devInfo.perfectQualityOrdersCompleted
            : 0;
        SetObjectiveProgress(
            perfectOrderObjectiveNumber,
            Mathf.Max(savedPerfectOrders, developerPerfectOrders));
    }

    private void SyncPerfectOrderObjectiveFromSavedStats()
    {
        if (!trackPerfectOrderObjective)
        {
            return;
        }

        ObjectiveBoardItem objective = GetObjectiveByNumber(perfectOrderObjectiveNumber);
        if (objective == null)
        {
            return;
        }

        objective.requiredProgress = Mathf.Max(1f, perfectOrdersRequired);
        objective.currentProgress = Mathf.Max(
            objective.currentProgress,
            PlayerPrefs.GetInt("PerfectQualityOrdersCompleted", 0));
        objective.isFinished = objective.isFinished || objective.Progress01 >= 1f;
        SaveObjectiveState(perfectOrderObjectiveNumber);
    }

    public void SetObjectiveProgress(int objectiveNumber, float currentProgress)
    {
        ObjectiveBoardItem objective = GetObjectiveByNumber(objectiveNumber);
        if (objective == null)
        {
            return;
        }

        objective.currentProgress = Mathf.Max(0f, currentProgress);
        objective.isFinished = objective.isFinished || objective.Progress01 >= 1f;
        SaveObjectiveState(objectiveNumber);
        RefreshBoard();
    }

    public void SetObjectiveProgress01(int objectiveNumber, float progress01)
    {
        ObjectiveBoardItem objective = GetObjectiveByNumber(objectiveNumber);
        if (objective == null)
        {
            return;
        }

        objective.currentProgress = Mathf.Clamp01(progress01) * objective.requiredProgress;
        objective.isFinished = objective.isFinished || progress01 >= 1f;
        SaveObjectiveState(objectiveNumber);
        RefreshBoard();
    }

    public void FinishObjective(int objectiveNumber)
    {
        ObjectiveBoardItem objective = GetObjectiveByNumber(objectiveNumber);
        if (objective == null)
        {
            return;
        }

        objective.isFinished = true;
        objective.currentProgress = objective.requiredProgress;
        SaveObjectiveState(objectiveNumber);
        RefreshBoard();
    }

    public void ClaimBonus(int objectiveNumber)
    {
        ObjectiveBoardItem objective = GetObjectiveByNumber(objectiveNumber);
        if (objective == null || !objective.isFinished || !objective.hasBonus || objective.bonusClaimed)
        {
            return;
        }

        DeveloperSimulation developer = FindFirstObjectByType<DeveloperSimulation>();
        if (developer != null)
        {
            if (objective.bonusMoney > 0)
            {
                developer.AddMoney(objective.bonusMoney);
            }

            if (objective.bonusExperience > 0f)
            {
                developer.AddExperience(objective.bonusExperience);
            }
        }

        objective.bonusClaimed = true;
        objective.onBonusClaimed?.Invoke();
        SaveObjectiveState(objectiveNumber);
        RefreshBoard();
    }

    public void SetObjectiveVisible(int objectiveNumber, bool visible)
    {
        ObjectiveBoardItem objective = GetObjectiveByNumber(objectiveNumber);
        if (objective == null)
        {
            return;
        }

        objective.isVisible = visible;
        RefreshBoard();
    }

    public void SetObjectiveText(int objectiveNumber, string content, string finishCondition)
    {
        ObjectiveBoardItem objective = GetObjectiveByNumber(objectiveNumber);
        if (objective == null)
        {
            return;
        }

        objective.content = content;
        objective.finishCondition = finishCondition;
        RefreshBoard();
    }

    private void LoadObjectiveStates()
    {
        if (!persistObjectiveState || objectives == null)
        {
            return;
        }

        for (int i = 0; i < objectives.Length; i++)
        {
            ObjectiveBoardItem objective = objectives[i];
            if (objective == null)
            {
                continue;
            }

            int objectiveNumber = i + 1;
            string prefix = GetObjectivePrefsPrefix(objectiveNumber);

            if (PlayerPrefs.HasKey(prefix + "Progress"))
            {
                objective.currentProgress = Mathf.Max(
                    objective.currentProgress,
                    PlayerPrefs.GetFloat(prefix + "Progress", objective.currentProgress));
            }

            if (PlayerPrefs.GetInt(prefix + "Finished", objective.isFinished ? 1 : 0) == 1)
            {
                objective.isFinished = true;
                objective.currentProgress = Mathf.Max(objective.currentProgress, objective.requiredProgress);
            }

            if (PlayerPrefs.GetInt(prefix + "BonusClaimed", objective.bonusClaimed ? 1 : 0) == 1)
            {
                objective.bonusClaimed = true;
            }
        }
    }

    private void SaveObjectiveState(int objectiveNumber)
    {
        if (!persistObjectiveState)
        {
            return;
        }

        ObjectiveBoardItem objective = GetObjectiveByNumber(objectiveNumber);
        if (objective == null)
        {
            return;
        }

        string prefix = GetObjectivePrefsPrefix(objectiveNumber);
        PlayerPrefs.SetFloat(prefix + "Progress", objective.currentProgress);
        PlayerPrefs.SetInt(prefix + "Finished", objective.isFinished ? 1 : 0);
        PlayerPrefs.SetInt(prefix + "BonusClaimed", objective.bonusClaimed ? 1 : 0);
        PlayerPrefs.Save();
    }

    private string GetObjectivePrefsPrefix(int objectiveNumber)
    {
        string key = string.IsNullOrWhiteSpace(persistenceKey) ? name : persistenceKey;
        return "ObjectiveBoard_" + key + "_Objective_" + objectiveNumber + "_";
    }

    private ObjectiveBoardItem GetObjectiveByNumber(int objectiveNumber)
    {
        int index = objectiveNumber - 1;
        if (objectives == null || index < 0 || index >= objectives.Length)
        {
            return null;
        }

        return objectives[index];
    }

    private void Build()
    {
        NormalizeBoardDefaults();

        if (!createMissingUi && (boardRoot == null || objectiveContainer == null))
        {
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
                "ObjectiveBoardCanvas",
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

        if (boardRoot == null)
        {
            GameObject boardObject = new GameObject(
                "ObjectiveBoard",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            boardObject.transform.SetParent(canvas.transform, false);
            boardRoot = boardObject.GetComponent<RectTransform>();
            boardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            boardRoot.pivot = new Vector2(0.5f, 0.5f);
            boardRoot.anchoredPosition = anchoredPosition;
            boardRoot.sizeDelta = boardSize;

            Image background = boardObject.GetComponent<Image>();
            background.color = boardColor;
            background.raycastTarget = false;
            canvasGroup = boardObject.GetComponent<CanvasGroup>();
        }

        VerticalLayoutGroup boardLayout = boardRoot.GetComponent<VerticalLayoutGroup>();
        if (boardLayout != null)
        {
            boardLayout.enabled = false;
        }

        boardRoot.anchorMin = new Vector2(0.5f, 0.5f);
        boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
        boardRoot.pivot = new Vector2(0.5f, 0.5f);
        boardRoot.anchoredPosition = anchoredPosition;
        boardRoot.sizeDelta = boardSize;

        Image boardBackground = boardRoot.GetComponent<Image>();
        if (boardBackground != null)
        {
            boardBackground.color = boardColor;
        }

        if (canvasGroup == null)
        {
            canvasGroup = boardRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = boardRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }

        EnsureObjectiveButtonTransition();
        EnsureCloseButton();

        if (titleText == null)
        {
            GameObject titleObject = new GameObject(
                "Title",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            titleObject.transform.SetParent(boardRoot, false);
            titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 24f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = textColor;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.raycastTarget = false;

            LayoutElement titleLayout = titleObject.GetComponent<LayoutElement>();
            titleLayout.preferredHeight = 34f;
        }

        ApplyTopStretch(titleText.rectTransform, titlePosition, titleSize);
        titleText.fontSize = titleFontSize;
        ApplyRightTop(closeButton.GetComponent<RectTransform>(), closeButtonPosition, closeButtonSize);
        ApplyCloseButtonStyle();

        if (objectiveContainer == null)
        {
            GameObject containerObject = new GameObject(
                "ObjectiveList",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            containerObject.transform.SetParent(boardRoot, false);
            objectiveContainer = containerObject.GetComponent<RectTransform>();

            VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = rowSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        ApplyTopStretch(objectiveContainer, objectiveListPosition, objectiveListSize);

        VerticalLayoutGroup containerLayout = objectiveContainer.GetComponent<VerticalLayoutGroup>();
        if (containerLayout != null)
        {
            containerLayout.spacing = rowSpacing;
            containerLayout.childControlHeight = true;
            containerLayout.childForceExpandHeight = false;
        }
    }

    private void EnsureCloseButton()
    {
        if (boardRoot == null)
        {
            return;
        }

        if (closeButton == null)
        {
            Transform existing = boardRoot.Find("Close Button");
            if (existing != null)
            {
                closeButton = existing.GetComponent<Button>();
            }
        }

        if (closeButton == null)
        {
            GameObject buttonObject = new GameObject(
                "Close Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(boardRoot, false);
            closeButton = buttonObject.GetComponent<Button>();
            closeButton.onClick.RemoveListener(HideBoard);
            closeButton.onClick.AddListener(HideBoard);
        }

        ApplyRightTop(closeButton.GetComponent<RectTransform>(), closeButtonPosition, closeButtonSize);
        ApplyCloseButtonStyle();
    }

    private void ApplyCloseButtonStyle()
    {
        if (closeButton == null)
        {
            return;
        }

        Image image = closeButton.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = closeButtonSprite;
            image.color = closeButtonColor;
            image.preserveAspect = true;
        }

        if (closeButton.GetComponent<HoverScaleEffect>() == null)
        {
            closeButton.gameObject.AddComponent<HoverScaleEffect>();
        }

        closeButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = closeButton.colors;
        colors.normalColor = closeButtonColor;
        colors.highlightedColor = Color.Lerp(closeButtonColor, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(closeButtonColor, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(closeButtonColor.r, closeButtonColor.g, closeButtonColor.b, 0.35f);
        colors.fadeDuration = 0.12f;
        closeButton.colors = colors;
    }

    private void EnsureRowCount(int count)
    {
        while (rowViews.Count < count)
        {
            rowViews.Add(CreateRow(rowViews.Count + 1));
        }

        for (int i = count; i < rowViews.Count; i++)
        {
            if (rowViews[i].Root != null)
            {
                rowViews[i].Root.gameObject.SetActive(false);
            }
        }
    }

    private ObjectiveRowView CreateRow(int number)
    {
        GameObject rowObject = new GameObject(
            "Objective " + number,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        rowObject.transform.SetParent(objectiveContainer, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, rowHeight);
        LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = rowHeight;
        rowLayout.minHeight = rowHeight;
        rowLayout.flexibleHeight = 0f;

        Image rowBackground = rowObject.GetComponent<Image>();
        rowBackground.color = rowColor;
        rowBackground.raycastTarget = false;

        ObjectiveRowView row = new ObjectiveRowView
        {
            Root = rowRect,
            LayoutElement = rowLayout,
            NumberBackground = CreateNumberBadge(rowRect),
            ContentText = CreateText(rowRect, "Content", new Vector2(56f, -12f), new Vector2(-68f, 36f), 18f, textColor, FontStyles.Bold),
            ConditionText = CreateText(rowRect, "Finish Condition", new Vector2(56f, -52f), new Vector2(-68f, 24f), 14f, conditionColor, FontStyles.Normal),
            ProgressFill = CreateProgressBar(rowRect, out TextMeshProUGUI progressStateText),
            TickImage = CreateTickImage(rowRect),
            TickFallbackText = CreateTickFallbackText(rowRect),
            BonusButton = CreateBonusButton(rowRect, out TextMeshProUGUI bonusButtonText, out Image bonusReadyIcon)
        };
        row.NumberText = row.NumberBackground.GetComponentInChildren<TextMeshProUGUI>();
        row.ProgressStateText = progressStateText;
        row.BonusButtonText = bonusButtonText;
        row.BonusReadyIcon = bonusReadyIcon;
        return row;
    }

    private Image CreateNumberBadge(RectTransform parent)
    {
        GameObject badgeObject = new GameObject(
            "Number",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        badgeObject.transform.SetParent(parent, false);
        RectTransform rect = badgeObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -12f);
        rect.sizeDelta = new Vector2(32f, 32f);

        Image image = badgeObject.GetComponent<Image>();
        image.color = numberColor;
        image.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(rect, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 18f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(
        RectTransform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        Color color,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private Image CreateProgressBar(RectTransform parent, out TextMeshProUGUI progressStateText)
    {
        GameObject barObject = new GameObject(
            "Progress Bar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        barObject.transform.SetParent(parent, false);
        RectTransform rect = barObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 12f);
        rect.sizeDelta = new Vector2(-24f, progressBarHeight);

        Image background = barObject.GetComponent<Image>();
        background.color = progressBackgroundColor;
        background.raycastTarget = false;

        GameObject fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fillObject.transform.SetParent(rect, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fill = fillObject.GetComponent<Image>();
        fill.color = progressColor;
        fill.raycastTarget = false;
        progressStateText = CreateText(
            parent,
            "Progress State",
            new Vector2(-88f, 17f),
            new Vector2(76f, 20f),
            13f,
            conditionColor,
            FontStyles.Bold);
        progressStateText.alignment = TextAlignmentOptions.Right;
        return fill;
    }

    private Image CreateTickImage(RectTransform parent)
    {
        GameObject tickObject = new GameObject(
            "Completion Tick",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        tickObject.transform.SetParent(parent, false);

        Image image = tickObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.gameObject.SetActive(false);
        return image;
    }

    private TextMeshProUGUI CreateTickFallbackText(RectTransform parent)
    {
        TextMeshProUGUI text = CreateText(
            parent,
            "Completion Tick Text",
            new Vector2(-42f, 24f),
            new Vector2(78f, 78f),
            58f,
            finishedColor,
            FontStyles.Bold);
        text.text = "✓";
        text.alignment = TextAlignmentOptions.Center;
        text.gameObject.SetActive(false);
        return text;
    }

    private Button CreateBonusButton(RectTransform parent, out TextMeshProUGUI label, out Image bonusReadyIcon)
    {
        GameObject buttonObject = new GameObject(
            "Bonus Button",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image background = buttonObject.GetComponent<Image>();
        background.color = bonusButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;

        GameObject iconObject = new GameObject(
            "Ready Icon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        bonusReadyIcon = iconObject.GetComponent<Image>();
        bonusReadyIcon.preserveAspect = true;
        bonusReadyIcon.raycastTarget = false;
        bonusReadyIcon.gameObject.SetActive(false);

        GameObject labelObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        label = labelObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = 13f;
        label.fontStyle = FontStyles.Bold;
        label.color = bonusButtonTextColor;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        buttonObject.SetActive(false);
        return button;
    }

    private void ApplyObjective(ObjectiveRowView row, ObjectiveBoardItem objective, int number)
    {
        NormalizeObjectiveDefaults(objective);
        ApplyObjectiveLayout(row, objective);

        if (row.NumberText != null)
        {
            row.NumberText.text = number.ToString();
            row.NumberText.fontSize = GetNumberFontSize(objective);
        }

        if (row.ContentText != null)
        {
            row.ContentText.text = objective.content;
            row.ContentText.fontSize = GetContentFontSize(objective);
        }

        if (row.ConditionText != null)
        {
            row.ConditionText.text = objective.isFinished
                ? "Finished"
                : objective.finishCondition;
            row.ConditionText.fontSize = GetConditionFontSize(objective);
            row.ConditionText.color = objective.isFinished ? finishedColor : conditionColor;
        }

        if (row.ProgressStateText != null)
        {
            row.ProgressStateText.text = BuildProgressStateText(objective);
            row.ProgressStateText.fontSize = GetProgressStateFontSize(objective);
            row.ProgressStateText.color = objective.isFinished
                ? completedProgressStateTextColor
                : progressStateTextColor;
        }

        if (row.ProgressFill != null)
        {
            row.ProgressFill.color = objective.isFinished ? finishedColor : progressColor;
        }

        if (row.NumberBackground != null)
        {
            row.NumberBackground.color = objective.isFinished ? finishedColor : numberColor;
        }

        ApplyFill(row.ProgressFill, objective.Progress01);
        ApplyTick(row, objective);
        ApplyBonusButton(row, objective, number);
    }

    private void ApplyObjectiveLayout(ObjectiveRowView row, ObjectiveBoardItem objective)
    {
        if (row.LayoutElement != null)
        {
            row.LayoutElement.preferredHeight = objective.rowHeight;
            row.LayoutElement.minHeight = objective.rowHeight;
            row.LayoutElement.flexibleHeight = 0f;
        }

        ApplyTopLeft(row.NumberBackground.rectTransform, GetNumberPosition(objective), GetNumberSize(objective));
        ApplyTopStretch(row.ContentText.rectTransform, GetContentPosition(objective), GetContentSize(objective));
        ApplyTopStretch(row.ConditionText.rectTransform, GetConditionPosition(objective), GetConditionSize(objective));

        RectTransform progressRect = row.ProgressFill.transform.parent as RectTransform;
        if (progressRect != null)
        {
            ApplyBottomStretch(progressRect, GetProgressBarPosition(objective), GetProgressBarSize(objective));
        }

        ApplyRightBottom(row.ProgressStateText.rectTransform, GetProgressStatePosition(objective), GetProgressStateSize(objective));
        ApplyRightCenter(row.TickImage.rectTransform, GetTickPosition(objective), GetTickSize(objective));
        ApplyRightCenter(row.TickFallbackText.rectTransform, GetTickPosition(objective), GetTickSize(objective));
        ApplyRightCenter(row.BonusButton.GetComponent<RectTransform>(), GetBonusButtonPosition(objective), GetBonusButtonSize(objective));
        ApplyBonusButtonInnerLayout(row, objective);
    }

    private void ApplyTick(ObjectiveRowView row, ObjectiveBoardItem objective)
    {
        if (row.TickImage == null)
        {
            return;
        }

        Sprite sprite = objective.tickSprite != null ? objective.tickSprite : defaultTickSprite;
        row.TickImage.sprite = sprite;
        row.TickImage.color = Color.white;
        row.TickImage.gameObject.SetActive(objective.isFinished && sprite != null);

        if (row.TickFallbackText != null)
        {
            row.TickFallbackText.gameObject.SetActive(objective.isFinished && sprite == null);
            row.TickFallbackText.fontSize = Mathf.Max(1f, GetTickSize(objective).y * 0.72f);
            row.TickFallbackText.color = finishedColor;
        }
    }

    private void ApplyBonusButton(ObjectiveRowView row, ObjectiveBoardItem objective, int number)
    {
        if (row.BonusButton == null)
        {
            return;
        }

        bool canClaim = objective.isFinished && objective.hasBonus && !objective.bonusClaimed;
        row.BonusButton.gameObject.SetActive(objective.hasBonus);
        row.BonusButton.interactable = canClaim;
        row.BonusButton.onClick.RemoveAllListeners();
        Image buttonImage = row.BonusButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = canClaim
                ? bonusButtonColor
                : new Color(bonusButtonColor.r, bonusButtonColor.g, bonusButtonColor.b, 0.45f);
        }

        if (canClaim)
        {
            row.BonusButton.onClick.AddListener(() => ClaimBonus(number));
        }

        if (row.BonusButtonText != null)
        {
            if (objective.bonusClaimed)
            {
                row.BonusButtonText.text = "Claimed";
            }
            else if (!objective.isFinished)
            {
                row.BonusButtonText.text = "Bonus";
            }
            else
            {
                row.BonusButtonText.text = string.IsNullOrWhiteSpace(objective.bonusButtonText)
                    ? "Claim"
                    : objective.bonusButtonText;
            }

            row.BonusButtonText.color = canClaim
                ? bonusButtonTextColor
                : new Color(bonusButtonTextColor.r, bonusButtonTextColor.g, bonusButtonTextColor.b, 0.65f);
            row.BonusButtonText.fontSize = objective.bonusButtonFontSize;
        }

        if (row.BonusReadyIcon != null)
        {
            row.BonusReadyIcon.sprite = objective.bonusReadySprite;
            row.BonusReadyIcon.gameObject.SetActive(objective.hasBonus && objective.bonusReadySprite != null);
            row.BonusReadyIcon.color = canClaim
                ? bonusReadyIconColor
                : disabledBonusReadyIconColor;
        }
    }

    private void ApplyBonusButtonInnerLayout(ObjectiveRowView row, ObjectiveBoardItem objective)
    {
        if (row.BonusButtonText != null)
        {
            RectTransform textRect = row.BonusButtonText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = objective.bonusTextOffsetMin;
            textRect.offsetMax = objective.bonusTextOffsetMax;
        }

        if (row.BonusReadyIcon != null)
        {
            RectTransform iconRect = row.BonusReadyIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = objective.bonusIconPosition;
            iconRect.sizeDelta = objective.bonusIconSize;
        }
    }

    private string BuildProgressStateText(ObjectiveBoardItem objective)
    {
        if (objective.isFinished)
        {
            return "Done";
        }

        return Mathf.FloorToInt(objective.currentProgress)
            + "/"
            + Mathf.CeilToInt(objective.requiredProgress);
    }

    private void NormalizeBoardDefaults()
    {
        if (ApproximatelyColor(progressColor, new Color(0.35f, 1f, 0.45f, 0.95f))
            || ApproximatelyColor(progressColor, finishedColor))
        {
            progressColor = new Color(0.25f, 0.68f, 1f, 0.95f);
        }

        if (objectiveListSize.y <= 0f)
        {
            objectiveListSize.y = Mathf.Max(1f, boardSize.y - Mathf.Abs(objectiveListPosition.y) - 44f);
        }

        if (largeBoardDefaultsApplied)
        {
            return;
        }

        if (boardSize == Vector2.zero || Approximately(boardSize, new Vector2(420f, 520f)))
        {
            boardSize = new Vector2(1000f, 1500f);
        }

        if (titlePosition == Vector2.zero)
        {
            titlePosition = new Vector2(44f, -34f);
        }

        if (titleSize == Vector2.zero)
        {
            titleSize = new Vector2(-88f, 86f);
        }

        if (titleFontSize <= 0f || Mathf.Approximately(titleFontSize, 24f))
        {
            titleFontSize = 58f;
        }

        if (objectiveListPosition == Vector2.zero)
        {
            objectiveListPosition = new Vector2(44f, -144f);
        }

        if (objectiveListSize == Vector2.zero)
        {
            objectiveListSize = new Vector2(-88f, 1312f);
        }

        if (Mathf.Approximately(rowHeight, 104f))
        {
            rowHeight = 220f;
        }

        if (Mathf.Approximately(rowSpacing, 10f))
        {
            rowSpacing = 22f;
        }

        if (Mathf.Approximately(progressBarHeight, 8f))
        {
            progressBarHeight = 18f;
        }

        largeBoardDefaultsApplied = true;
    }

    private void NormalizeObjectiveDefaults(ObjectiveBoardItem objective)
    {
        objective.requiredProgress = Mathf.Max(0.01f, objective.requiredProgress);

        if (objective.rowHeight <= 0f || Mathf.Approximately(objective.rowHeight, 104f))
        {
            objective.rowHeight = rowHeight;
        }

        if (objective.numberFontSize <= 0f || Mathf.Approximately(objective.numberFontSize, 18f))
        {
            objective.numberFontSize = 42f;
        }

        if (objective.contentFontSize <= 0f || Mathf.Approximately(objective.contentFontSize, 18f))
        {
            objective.contentFontSize = 42f;
        }

        if (objective.conditionFontSize <= 0f || Mathf.Approximately(objective.conditionFontSize, 14f))
        {
            objective.conditionFontSize = 30f;
        }

        if (objective.progressStateFontSize <= 0f || Mathf.Approximately(objective.progressStateFontSize, 13f))
        {
            objective.progressStateFontSize = 28f;
        }

        if (Approximately(objective.numberPosition, new Vector2(12f, -12f)))
        {
            objective.numberPosition = new Vector2(28f, -28f);
        }

        if (objective.numberSize == Vector2.zero || Approximately(objective.numberSize, new Vector2(32f, 32f)))
        {
            objective.numberSize = new Vector2(70f, 70f);
        }

        if (Approximately(objective.contentPosition, new Vector2(56f, -12f)))
        {
            objective.contentPosition = new Vector2(126f, -28f);
        }

        if (objective.contentSize == Vector2.zero || Approximately(objective.contentSize, new Vector2(-68f, 36f)))
        {
            objective.contentSize = new Vector2(-220f, 76f);
        }

        if (Approximately(objective.conditionPosition, new Vector2(56f, -52f)))
        {
            objective.conditionPosition = new Vector2(126f, -108f);
        }

        if (objective.conditionSize == Vector2.zero || Approximately(objective.conditionSize, new Vector2(-68f, 24f)))
        {
            objective.conditionSize = new Vector2(-220f, 52f);
        }

        if (Approximately(objective.progressBarPosition, new Vector2(0f, 12f))
            || Approximately(objective.progressBarPosition, new Vector2(0f, 28f)))
        {
            objective.progressBarPosition = new Vector2(0f, 20f);
        }

        if (objective.progressBarSize == Vector2.zero || Approximately(objective.progressBarSize, new Vector2(-104f, 8f)))
        {
            objective.progressBarSize = new Vector2(-250f, progressBarHeight);
        }

        if (Approximately(objective.progressStatePosition, new Vector2(-88f, 17f))
            || Approximately(objective.progressStatePosition, new Vector2(-212f, 28f)))
        {
            objective.progressStatePosition = new Vector2(-212f, 20f);
        }

        if (objective.progressStateSize == Vector2.zero || Approximately(objective.progressStateSize, new Vector2(76f, 20f)))
        {
            objective.progressStateSize = new Vector2(180f, 42f);
        }

        if (Approximately(objective.tickPosition, new Vector2(-20f, 0f)))
        {
            objective.tickPosition = new Vector2(-42f, 24f);
        }

        if (objective.tickSize == Vector2.zero || Approximately(objective.tickSize, new Vector2(34f, 34f)))
        {
            objective.tickSize = new Vector2(78f, 78f);
        }

        if (Approximately(objective.bonusButtonPosition, new Vector2(-20f, -32f)))
        {
            objective.bonusButtonPosition = new Vector2(-36f, -64f);
        }

        if (objective.bonusButtonFontSize <= 0f || Mathf.Approximately(objective.bonusButtonFontSize, 13f))
        {
            objective.bonusButtonFontSize = 30f;
        }

        if (objective.bonusButtonSize == Vector2.zero || Approximately(objective.bonusButtonSize, new Vector2(86f, 28f)))
        {
            objective.bonusButtonSize = new Vector2(170f, 58f);
        }

        if (objective.bonusIconSize == Vector2.zero)
        {
            objective.bonusIconSize = new Vector2(34f, 34f);
        }

        if (objective.bonusTextOffsetMin == Vector2.zero)
        {
            objective.bonusTextOffsetMin = new Vector2(48f, 0f);
        }

        if (objective.bonusTextOffsetMax == Vector2.zero)
        {
            objective.bonusTextOffsetMax = new Vector2(-8f, 0f);
        }
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x)
            && Mathf.Approximately(a.y, b.y);
    }

    private static bool ApproximatelyColor(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f
            && Mathf.Abs(a.g - b.g) < 0.01f
            && Mathf.Abs(a.b - b.b) < 0.01f
            && Mathf.Abs(a.a - b.a) < 0.01f;
    }

    private float GetNumberFontSize(ObjectiveBoardItem objective)
    {
        return objective.numberFontSize;
    }

    private float GetContentFontSize(ObjectiveBoardItem objective)
    {
        return objective.contentFontSize;
    }

    private float GetConditionFontSize(ObjectiveBoardItem objective)
    {
        return objective.conditionFontSize;
    }

    private float GetProgressStateFontSize(ObjectiveBoardItem objective)
    {
        return objective.progressStateFontSize;
    }

    private Vector2 GetNumberPosition(ObjectiveBoardItem objective)
    {
        return objective.numberPosition;
    }

    private Vector2 GetNumberSize(ObjectiveBoardItem objective)
    {
        return objective.numberSize;
    }

    private Vector2 GetContentPosition(ObjectiveBoardItem objective)
    {
        return objective.contentPosition;
    }

    private Vector2 GetContentSize(ObjectiveBoardItem objective)
    {
        return objective.contentSize;
    }

    private Vector2 GetConditionPosition(ObjectiveBoardItem objective)
    {
        return objective.conditionPosition;
    }

    private Vector2 GetConditionSize(ObjectiveBoardItem objective)
    {
        return objective.conditionSize;
    }

    private Vector2 GetProgressBarPosition(ObjectiveBoardItem objective)
    {
        return objective.progressBarPosition;
    }

    private Vector2 GetProgressBarSize(ObjectiveBoardItem objective)
    {
        return objective.progressBarSize;
    }

    private Vector2 GetProgressStatePosition(ObjectiveBoardItem objective)
    {
        return objective.progressStatePosition;
    }

    private Vector2 GetProgressStateSize(ObjectiveBoardItem objective)
    {
        return objective.progressStateSize;
    }

    private Vector2 GetTickPosition(ObjectiveBoardItem objective)
    {
        return objective.tickPosition;
    }

    private Vector2 GetTickSize(ObjectiveBoardItem objective)
    {
        return objective.tickSize;
    }

    private Vector2 GetBonusButtonPosition(ObjectiveBoardItem objective)
    {
        return objective.bonusButtonPosition;
    }

    private Vector2 GetBonusButtonSize(ObjectiveBoardItem objective)
    {
        return objective.bonusButtonSize;
    }

    private static void ApplyTopLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void ApplyTopStretch(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void ApplyRightTop(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void ApplyBottomStretch(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void ApplyRightBottom(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void ApplyRightCenter(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void ApplyFill(Image fill, float progress)
    {
        if (fill == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(progress);
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(clamped, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private bool HasVisibleObjective()
    {
        if (objectives == null)
        {
            return false;
        }

        foreach (ObjectiveBoardItem objective in objectives)
        {
            if (objective != null && objective.isVisible)
            {
                return true;
            }
        }

        return false;
    }
}

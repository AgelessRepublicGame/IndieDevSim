using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgrammingBugGame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform playArea;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private TMP_InputField _codeInput;

    [Header("Game Settings")]
    [SerializeField] private float gameTime = 30f;
    [SerializeField] private float bugSpawnInterval = 5f;
    [SerializeField] private float fallSpeed = 40f;
    [SerializeField] private int normalLinesPerBug = 5;
    [SerializeField] private int openingBugCount = 5;
    [SerializeField] private float openingBugSpacing = 120f;

    [Header("Ghost - Chaotic Bugs")]
    [SerializeField] private float chaoticMinTurnInterval = 0.12f;
    [SerializeField] private float chaoticMaxTurnInterval = 0.5f;
    [SerializeField] private float chaoticMinHorizontalMultiplier = 0.55f;
    [SerializeField] private float chaoticMaxHorizontalMultiplier = 1.5f;
    [SerializeField] private float chaoticMinFallMultiplier = 0.45f;
    [SerializeField] private float chaoticMaxFallMultiplier = 1.25f;

    [Header("Protector - Creeping Bugs")]
    [SerializeField] private float creepingBugSpeed = 25f;
    [SerializeField] private float creepingContactDistance = 30f;
    [SerializeField] private int creepingContactPenalty = 10;

    [Header("Colorblind - Invert Colors")]
    [SerializeField] private Color invertedPurpleColor = new Color(1f, 0.45f, 0f);

    [Header("Juggler - Simultaneous Bugs")]
    [SerializeField] private int simultaneousBatchSize = 3;
    [SerializeField] private float simultaneousHorizontalJitter = 35f;
    [SerializeField] private float simultaneousVerticalSpread = 100f;

    private float timer;
    private float bugSpawnTimer;
    private bool isRunning;
    private bool _hideoKKeyboardMode;
    private Image _mouseBlocker;

    private int fixedBugs;
    private int totalBugsAppeared;
    private int falseFriendPenalty;
    private int creepingBugPenalty;
    private int creepingBugContacts;
    private bool greenBugsEnabled;
    private bool purpleBugsEnabled;
    private bool chaoticBugsEnabled;
    private bool creepingBugsEnabled;
    private bool invertBugColorsEnabled;
    private bool oldAgeModeEnabled;
    private bool oldAgeFailureTriggered;
    private bool simultaneousBugsEnabled;
    private MiniGameProgressHud _progressHud;
    public bool ShouldFailWhenRealBugIsMissed => oldAgeModeEnabled && isRunning;
    public TextMeshProUGUI score_text;

    private readonly string[] normalTexts =
    {
        "int score = 0;",
        "player.Move();",
        "UpdateHealth();",
        "LoadScene();",
        "Debug.Log(message);"
    };

    private readonly string[] bugTexts =
    {
        "NullReferenceException",
        "IndexOutOfRangeException",
        "MissingReferenceException",
        "Syntax Error",
        "Stack Overflow"
    };

    private readonly string[] greenBugTexts =
    {
        "Optional Warning",
        "Unused Variable",
        "Style Hint",
        "Debug Message",
        "Todo Comment"
    };

    private readonly string[] purpleBugTexts =
    {
        "Race Condition",
        "Memory Leak",
        "Deadlock",
        "Infinite Loop",
        "Data Corruption"
    };

    public void ConfigureConditions(bool greenBugs, bool purpleBugs)
    {
        greenBugsEnabled = greenBugs;
        purpleBugsEnabled = purpleBugs;
    }

    public void ConfigureBossChallenge(ManagerId managerId)
    {
        if (BossMinigameConfig.IsTadG(managerId))
        {
            gameTime *= 0.85f;
            bugSpawnInterval *= 0.7f;
            openingBugCount = Mathf.RoundToInt(openingBugCount * 1.5f);
            normalLinesPerBug += 2;
            fallSpeed *= 1.15f;
        }
        else if (BossMinigameConfig.IsHideoK(managerId))
        {
            gameTime *= 1.1f;
            bugSpawnInterval *= 1.35f;
            purpleBugsEnabled = true;
            greenBugsEnabled = true;
            fallSpeed *= 0.5f;
            _hideoKKeyboardMode = true;
        }
    }

    private float GetEffectiveFallSpeed()
    {
        return fallSpeed * RandomEventSystem.GetProgrammingBugSpeedMultiplier();
    }

    private void Start()
    {
        _progressHud = MiniGameProgressHud.Attach(gameObject);
        chaoticBugsEnabled = MinigameEventSelectionController.IsProgrammingChaoticBugsSelected;
        creepingBugsEnabled = MinigameEventSelectionController.IsProgrammingCreepingBugsSelected;
        invertBugColorsEnabled = MinigameEventSelectionController.IsProgrammingInvertColorsSelected;
        oldAgeModeEnabled = MinigameEventSelectionController.IsProgrammingOldAgeSelected;
        simultaneousBugsEnabled = MinigameEventSelectionController.IsProgrammingSimultaneousBugsSelected;

        if (_hideoKKeyboardMode)
        {
            SetupHideoKTypingMode();
        }

        StartGame();
        UpdateProgressHud();
    }

    private void Update()
    {
        if (!isRunning || StageEventSystem.IsBlocking) return;

        timer -= Time.deltaTime;
        bugSpawnTimer -= Time.deltaTime;
        UpdateProgressHud();

        if (bugSpawnTimer <= 0f)
        {
            if (simultaneousBugsEnabled)
            {
                SpawnSimultaneousBatch(Mathf.Max(2, simultaneousBatchSize), true);
            }
            else
            {
                SpawnBug();
            }
            bugSpawnTimer = bugSpawnInterval;
        }

        if (timer <= -5f)
        {
            EndGame();
        }
    }

    private void SetupHideoKTypingMode()
    {
        EnsureCodeInput();
        EnsureMouseBlocker();

        if (_codeInput != null)
        {
            _codeInput.onValueChanged.RemoveListener(OnCodeInputChanged);
            _codeInput.onValueChanged.AddListener(OnCodeInputChanged);
            _codeInput.gameObject.SetActive(true);
            _codeInput.text = "";
            if (_codeInput.textComponent != null)
            {
                _codeInput.textComponent.color = Color.black;
            }
        }

        if (score_text != null)
        {
            score_text.text = "Hideo K. challenge: type the bug text exactly to fix it.\n";
        }
    }

    private void EnsureCodeInput()
    {
        if (_codeInput != null)
        {
            return;
        }

        _codeInput = GetComponentInChildren<TMP_InputField>(true);
        if (_codeInput != null)
        {
            return;
        }

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null || playArea == null)
        {
            return;
        }

        GameObject inputObject = new GameObject("CodeInput");
        inputObject.transform.SetParent(canvas.transform, false);
        RectTransform inputRect = inputObject.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.1f, 0.04f);
        inputRect.anchorMax = new Vector2(0.9f, 0.12f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        Image background = inputObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.96f);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(inputObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 6f);
        textRect.offsetMax = new Vector2(-12f, -6f);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 22f;
        text.color = Color.black;

        GameObject placeholderObject = new GameObject("Placeholder");
        placeholderObject.transform.SetParent(inputObject.transform, false);
        RectTransform placeholderRect = placeholderObject.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(12f, 6f);
        placeholderRect.offsetMax = new Vector2(-12f, -6f);
        TextMeshProUGUI placeholder = placeholderObject.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Type bug text here...";
        placeholder.fontSize = 22f;
        placeholder.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);

        _codeInput = inputObject.AddComponent<TMP_InputField>();
        _codeInput.textComponent = text;
        _codeInput.placeholder = placeholder;
        _codeInput.targetGraphic = background;
        _codeInput.transform.SetAsLastSibling();
    }

    private void EnsureMouseBlocker()
    {
        if (_mouseBlocker != null || playArea == null)
        {
            return;
        }

        GameObject blockerObject = new GameObject("MouseBlocker");
        blockerObject.transform.SetParent(playArea, false);
        RectTransform blockerRect = blockerObject.AddComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        blockerObject.transform.SetAsFirstSibling();

        _mouseBlocker = blockerObject.AddComponent<Image>();
        _mouseBlocker.color = new Color(0f, 0f, 0f, 0f);
        _mouseBlocker.raycastTarget = true;
    }

    private void OnCodeInputChanged(string value)
    {
        if (!_hideoKKeyboardMode || !isRunning || StageEventSystem.IsBlocking || playArea == null)
        {
            return;
        }

        string typed = value?.Trim() ?? "";
        UpdateTypingProgress(typed);

        if (typed.Length == 0)
        {
            return;
        }

        FallingBug[] bugs = playArea.GetComponentsInChildren<FallingBug>(true);
        if (oldAgeModeEnabled && !HasMatchingBugPrefix(typed, bugs))
        {
            FailCurrentOrder("Old Age: invalid code failed the order.");
            return;
        }

        foreach (FallingBug bug in bugs)
        {
            if (bug == null || !bug.IsFixableBug)
            {
                continue;
            }

            string bugText = bug.DisplayText.Trim();
            if (bugText.Length == 0)
            {
                continue;
            }

            if (typed.Equals(bugText, StringComparison.OrdinalIgnoreCase))
            {
                bug.FixWithKeyboard();
                if (_codeInput != null)
                {
                    _codeInput.text = "";
                }

                UpdateTypingProgress("");
                break;
            }
        }
    }

    private bool HasMatchingBugPrefix(string typed, FallingBug[] bugs)
    {
        foreach (FallingBug bug in bugs)
        {
            if (bug != null
                && bug.IsFixableBug
                && bug.DisplayText.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateTypingProgress(string typed)
    {
        if (playArea == null)
        {
            return;
        }

        FallingBug[] bugs = playArea.GetComponentsInChildren<FallingBug>(true);
        FallingBug bestBug = null;
        int bestMatchLength = 0;

        foreach (FallingBug bug in bugs)
        {
            if (bug == null)
            {
                continue;
            }

            if (!bug.IsFixableBug)
            {
                bug.ResetDisplayText();
                continue;
            }

            int matchLength = GetPrefixMatchLength(typed, bug.DisplayText);
            if (matchLength > bestMatchLength)
            {
                bestMatchLength = matchLength;
                bestBug = bug;
            }
        }

        foreach (FallingBug bug in bugs)
        {
            if (bug == null || !bug.IsFixableBug)
            {
                continue;
            }

            if (bug == bestBug && bestMatchLength > 0)
            {
                bug.SetTypingProgress(bestMatchLength);
            }
            else
            {
                bug.ResetDisplayText();
            }
        }
    }

    private static int GetPrefixMatchLength(string typed, string target)
    {
        if (string.IsNullOrEmpty(typed) || string.IsNullOrEmpty(target))
        {
            return 0;
        }

        int maxLength = Mathf.Min(typed.Length, target.Length);
        for (int i = 0; i < maxLength; i++)
        {
            if (char.ToLowerInvariant(typed[i]) != char.ToLowerInvariant(target[i]))
            {
                return 0;
            }
        }

        return maxLength;
    }

    private void StartGame()
    {
        timer = gameTime;
        bugSpawnTimer = bugSpawnInterval;
        isRunning = true;

        if (simultaneousBugsEnabled)
        {
            SpawnSimultaneousBatch(Mathf.Max(2, openingBugCount), false);
        }
        else
        {
            for (int i = 0; i < openingBugCount; i++)
            {
                SpawnBug(i * openingBugSpacing, false);
            }
        }
    }

    private void SpawnSimultaneousBatch(int count, bool allowSpecialBugs)
    {
        if (playArea == null || count <= 0)
        {
            return;
        }

        float halfWidth = playArea.rect.width * 0.4f;
        for (int i = 0; i < count; i++)
        {
            float normalizedPosition = count == 1 ? 0.5f : i / (float)(count - 1);
            float x = Mathf.Lerp(-halfWidth, halfWidth, normalizedPosition)
                + UnityEngine.Random.Range(-simultaneousHorizontalJitter, simultaneousHorizontalJitter);
            float heightOffset = UnityEngine.Random.Range(0f, simultaneousVerticalSpread);
            SpawnBug(heightOffset, allowSpecialBugs, x);
        }
    }

    private void SpawnBug(
        float heightOffset = 0f,
        bool allowSpecialBugs = true,
        float? primaryBugX = null)
    {
        bool spawnPurpleBug = allowSpecialBugs && purpleBugsEnabled && UnityEngine.Random.value > 0.5f;
        SpawnLine(
            spawnPurpleBug ? BugLineType.PurpleBug : BugLineType.RedBug,
            heightOffset,
            primaryBugX);

        for (int i = 0; i < normalLinesPerBug; i++)
        {
            SpawnLine(BugLineType.Normal, heightOffset + UnityEngine.Random.Range(-400f, 400f));
        }

        if (allowSpecialBugs && greenBugsEnabled)
        {
            SpawnLine(BugLineType.GreenFalseFriend, heightOffset + UnityEngine.Random.Range(-300f, 300f));
        }

        totalBugsAppeared++;
        UpdateProgressHud();
    }

    private void SpawnLine(BugLineType lineType, float heightOffset = 0f, float? forcedX = null)
    {
        GameObject lineObject = Instantiate(textPrefab, playArea);
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        TMP_Text text = lineObject.GetComponent<TMP_Text>();
        bool isRealBug = lineType == BugLineType.RedBug || lineType == BugLineType.PurpleBug;

        float x = forcedX ?? UnityEngine.Random.Range(-playArea.rect.width / 2f, playArea.rect.width / 2f);
        float y = playArea.rect.height / 2f + heightOffset;

        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta= new Vector2(rect.sizeDelta.x, isRealBug || lineType == BugLineType.GreenFalseFriend ? 30f : 0f);

        string lineText = GetText(lineType);
        Color eventColor = invertBugColorsEnabled
            ? GetInvertedColor(lineType)
            : GetColor(lineType);
        Color lineColor = RandomEventSystem.ApplyProgrammingBugColor(eventColor);
        text.text = lineText;
        text.color = lineColor;
        text.raycastTarget = !_hideoKKeyboardMode;

        FallingBug line = lineObject.AddComponent<FallingBug>();
        float effectiveFallSpeed = GetEffectiveFallSpeed();
        float speed = lineType == BugLineType.PurpleBug ? effectiveFallSpeed * 1.6f : effectiveFallSpeed;
        line.Init(this, text, speed, lineType, lineType == BugLineType.PurpleBug, lineText, lineColor, !_hideoKKeyboardMode);

        if (chaoticBugsEnabled
            && (lineType == BugLineType.RedBug || lineType == BugLineType.GreenFalseFriend))
        {
            line.EnableChaoticMovement(
                chaoticMinTurnInterval,
                chaoticMaxTurnInterval,
                chaoticMinHorizontalMultiplier,
                chaoticMaxHorizontalMultiplier,
                chaoticMinFallMultiplier,
                chaoticMaxFallMultiplier);
        }

        if (creepingBugsEnabled
            && (lineType == BugLineType.GreenFalseFriend || lineType == BugLineType.Normal))
        {
            line.EnableCreepingMovement(creepingBugSpeed, creepingContactDistance);
        }

    }

    public void OnCreeperTouchedRed(FallingBug creeper, FallingBug redBug)
    {
        if (!isRunning || creeper == null || redBug == null)
        {
            return;
        }

        creepingBugPenalty += Mathf.Max(0, creepingContactPenalty);
        creepingBugContacts++;
        Destroy(creeper.gameObject);
        Destroy(redBug.gameObject);
    }

    public void OnLineClicked(GameObject lineObject, BugLineType lineType)
    {
        if (!isRunning || StageEventSystem.IsBlocking) return;

        if (oldAgeModeEnabled
            && lineType != BugLineType.RedBug
            && lineType != BugLineType.PurpleBug)
        {
            FailCurrentOrder("Old Age: clicking a false bug failed the order.");
            return;
        }

        if (lineType == BugLineType.RedBug || lineType == BugLineType.PurpleBug)
        {
            fixedBugs++;
            UpdateProgressHud();
            Destroy(lineObject);
        }
        else if (lineType == BugLineType.GreenFalseFriend)
        {
            falseFriendPenalty += 10;
            UpdateProgressHud();
            Destroy(lineObject);
        }
    }

    public void OnRequiredBugMissed(FallingBug bug)
    {
        if (bug == null || !oldAgeModeEnabled || !isRunning)
        {
            return;
        }

        FailCurrentOrder("Old Age: a missed bug failed the order.");
    }

    private void EndGame()
    {
        isRunning = false;

        if (_codeInput != null)
        {
            _codeInput.onValueChanged.RemoveListener(OnCodeInputChanged);
        }

        float quality = totalBugsAppeared > 0
            ? Mathf.Clamp(
                (float)(fixedBugs + creepingBugContacts) / totalBugsAppeared * 100f
                    - falseFriendPenalty
                    - creepingBugPenalty,
                0f,
                100f)
            : 0f;

        if (score_text != null)
        {
            score_text.text += ($"Bugs Fixed: {fixedBugs}\n");
            score_text.text += ($"Total Bugs Appeared: {totalBugsAppeared}\n");
            score_text.text += ($"False Friend Penalty: {falseFriendPenalty}\n");
            score_text.text += ($"Red Bugs Covered: {creepingBugContacts}\n");
            score_text.text += ($"Creeping Bug Penalty: {creepingBugPenalty}\n");
            score_text.text += ($"Quality: {quality:F2}%");
        }

        if (oldAgeModeEnabled && quality < 99.999f)
        {
            FailCurrentOrder("Old Age: less than 100% failed the order.");
            return;
        }

        MissionObjects mission = GetComponentInParent<MissionObjects>();
        if (mission != null && mission.CanAcceptCompletion)
        {
            mission.MissionEnd(quality / 100);
        }
    }

    private void FailCurrentOrder(string message)
    {
        if (oldAgeFailureTriggered)
        {
            return;
        }

        oldAgeFailureTriggered = true;
        isRunning = false;
        if (score_text != null)
        {
            score_text.text = message;
        }
        UpdateProgressHud();

        DeveloperSimulation developer = FindFirstObjectByType<DeveloperSimulation>();
        if (developer != null && developer.HasActiveOrder())
        {
            developer.Cheat(-1);
            return;
        }

        MissionObjects mission = GetComponentInParent<MissionObjects>();
        if (mission != null && mission.CanAcceptCompletion)
        {
            mission.MissionEnd(0f);
        }
    }

    private string GetText(BugLineType lineType)
    {
        switch (lineType)
        {
            case BugLineType.RedBug:
                return bugTexts[UnityEngine.Random.Range(0, bugTexts.Length)];
            case BugLineType.GreenFalseFriend:
                return greenBugTexts[UnityEngine.Random.Range(0, greenBugTexts.Length)];
            case BugLineType.PurpleBug:
                return purpleBugTexts[UnityEngine.Random.Range(0, purpleBugTexts.Length)];
            default:
                return normalTexts[UnityEngine.Random.Range(0, normalTexts.Length)];
        }
    }

    private void UpdateProgressHud()
    {
        if (_progressHud == null)
        {
            return;
        }

        float timePercent = gameTime > 0f
            ? Mathf.Clamp01(timer / gameTime)
            : 0f;
        float taskPercent = totalBugsAppeared > 0
            ? Mathf.Clamp01(fixedBugs / (float)totalBugsAppeared)
            : 0f;

        _progressHud.SetProgress(
            timePercent,
            "Time: " + Mathf.CeilToInt(Mathf.Max(0f, timer)) + "s",
            taskPercent,
            "Bugs fixed: " + fixedBugs + " / " + totalBugsAppeared);
    }

    private Color GetColor(BugLineType lineType)
    {
        switch (lineType)
        {
            case BugLineType.RedBug:
                return Color.red;
            case BugLineType.GreenFalseFriend:
                return Color.green;
            case BugLineType.PurpleBug:
                return new Color(0.6f, 0f, 1f);
            default:
                return Color.white;
        }
    }

    private Color GetInvertedColor(BugLineType lineType)
    {
        switch (lineType)
        {
            case BugLineType.RedBug:
                return Color.green;
            case BugLineType.GreenFalseFriend:
                return Color.red;
            case BugLineType.PurpleBug:
                return invertedPurpleColor;
            default:
                return Color.black;
        }
    }
}

public enum BugLineType
{
    Normal,
    RedBug,
    GreenFalseFriend,
    PurpleBug
}

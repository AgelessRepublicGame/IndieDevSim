using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DesignTaskType
{
    UseAllKeywords,
    FillMissingWord,
    MultipleChoice,
    Anagram,
    CorrectTheError
}

public class DesignPuzzle : MonoBehaviour
{
    private const int HintUnlockBoardLevel = 1;

    public TMP_InputField answerInput;
    public TMP_Text keywordText;
    public TMP_Text resultText;

    [Header("Task Settings")]
    public string[] keywords = { "shooting", "cover", "ammo", "reloading", "enemy" };
    [SerializeField] private float _keywordVisibleSeconds = 10f;

    [Header("Context Hints")]
    [Tooltip("Optional dedicated text for hints. Result Text is used when this is empty.")]
    [SerializeField] private TMP_Text _hintText;
    [Tooltip("Optional Hint button. Its click listener is connected automatically.")]
    [SerializeField] private Button _hintButton;
    [SerializeField] private string _hintButtonLabel = "Hint";

    [Header("Hideo K Bug Hunt")]
    [SerializeField] private RectTransform _bugHuntArea;
    [Tooltip("Parent panel for BugHuntArea. If empty, the Canvas child named Screen Panel is used.")]
    [SerializeField] private RectTransform _bugHuntParentPanel;
    [SerializeField] private string _bugHuntParentPanelName = "Screen Panel";
    [SerializeField] private float _bugHuntDuration = 24f;
    [SerializeField] private float _bugHuntSpawnInterval = 1.4f;
    [SerializeField] private float _bugHuntFallSpeed = 55f;

    private readonly List<string> _visibleKeywords = new List<string>();
    private readonly HashSet<string> _collectedKeywords = new HashSet<string>();
    private readonly List<GameObject> _fallingKeywordObjects = new List<GameObject>();
    private float _keywordTimer;
    private bool _keywordFadeActive;
    private DesignTaskType _taskType;
    private string _expectedAnswer = "";
    private string[] _multipleChoiceOptions = System.Array.Empty<string>();
    private bool _hideoKBugHuntMode;
    private bool _tadGChallengeMode;
    private bool _isMetelOrder;
    private float _bugHuntTimer;
    private float _bugHuntSpawnTimer;
    private bool _bugHuntRunning;
    private int _hintLevel;
    private MiniGameProgressHud _progressHud;

    private static readonly string[] MissingWordTemplates =
    {
        "The player needs {0} to survive the encounter.",
        "Add {0} to make the combat feel responsive.",
        "The level design should emphasize {0}."
    };

    private static readonly string[] ErrorTemplates =
    {
        "Players expect smooth {0} during combat.",
        "The HUD should clearly show remaining {0}.",
        "Every shooter needs reliable {0}."
    };

    private void Start()
    {
        _progressHud = MiniGameProgressHud.Attach(gameObject);
        ConfigureHintButton();
        ConfigureFromMission();
        if (_hideoKBugHuntMode)
        {
            StartHideoKBugHunt();
            UpdateProgressHud();
            return;
        }

        if (_isMetelOrder)
        {
            _taskType = DesignTaskType.UseAllKeywords;
        }
        else
        {
            PickTaskType();
        }

        BuildTaskPrompt();
        InitializeKeywordFade();
        UpdateProgressHud();
    }

    private void ConfigureHintButton()
    {
        if (_hintButton == null)
        {
            return;
        }

        bool hintsUnlocked = AreHintsUnlocked();
        _hintButton.gameObject.SetActive(hintsUnlocked);
        if (!hintsUnlocked)
        {
            return;
        }

        _hintButton.onClick.RemoveListener(ShowHint);
        _hintButton.onClick.AddListener(ShowHint);

        TMP_Text label = _hintButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null && !string.IsNullOrWhiteSpace(_hintButtonLabel))
        {
            label.text = _hintButtonLabel;
        }
    }

    public void ShowHint()
    {
        TMP_Text target = _hintText != null ? _hintText : resultText;
        if (target == null)
        {
            return;
        }

        if (!AreHintsUnlocked())
        {
            target.text = "Buy the Whiteboard to unlock hints.";
            return;
        }

        target.text = "<b>Hint:</b> " + GetContextHint(_hintLevel);
        _hintLevel++;
    }

    private static bool AreHintsUnlocked()
    {
        return PlayerPrefs.GetInt("WhiteBoardLevel", 0) >= HintUnlockBoardLevel;
    }

    private string GetContextHint(int hintLevel)
    {
        if (_hideoKBugHuntMode)
        {
            return hintLevel == 0
                ? "Click each falling keyword once. The counter increases only when you collect a new keyword."
                : "Prioritize keywords you have not collected yet; repeated words do not add progress.";
        }

        switch (_taskType)
        {
            case DesignTaskType.FillMissingWord:
                return GetWordHint(
                    hintLevel,
                    "Read the sentence and type only the design word that best completes the blank.");

            case DesignTaskType.MultipleChoice:
                if (hintLevel == 0)
                {
                    return "Enter either the complete correct word or its option letter: A, B, or C.";
                }

                int correctIndex = System.Array.FindIndex(
                    _multipleChoiceOptions,
                    option => NormalizeAnswer(option) == _expectedAnswer);
                return correctIndex >= 0
                    ? "The answer is option " + (char)('A' + correctIndex) + "."
                    : GetWordHint(hintLevel, "Compare each option with the requested design keyword.");

            case DesignTaskType.Anagram:
                return GetWordHint(
                    hintLevel,
                    "Rearrange every displayed letter; do not add or remove letters.");

            case DesignTaskType.CorrectTheError:
                return GetWordHint(
                    hintLevel,
                    "Only one keyword is misspelled. Type its corrected form, not the full sentence.");

            default:
                return hintLevel == 0
                    ? "Write one clear design sentence that naturally includes every listed keyword."
                    : "Before submitting, count the listed keywords and confirm that each exact word appears in your answer.";
        }
    }

    private string GetWordHint(int hintLevel, string firstHint)
    {
        if (hintLevel == 0 || string.IsNullOrEmpty(_expectedAnswer))
        {
            return firstHint;
        }

        if (hintLevel == 1)
        {
            return "The answer has " + _expectedAnswer.Length +
                " letters and starts with '" +
                char.ToUpperInvariant(_expectedAnswer[0]) + "'.";
        }

        return "The answer is \"" + _expectedAnswer + "\".";
    }

    private void ConfigureFromMission()
    {
        MissionObjects mission = GetComponentInParent<MissionObjects>();
        if (mission == null || mission.activeOrder == null)
        {
            return;
        }

        ManagerId managerId = mission.activeOrder.manager;
        _isMetelOrder = managerId == ManagerId.Metel;
        _tadGChallengeMode = BossMinigameConfig.IsTadG(managerId);
        _hideoKBugHuntMode = BossMinigameConfig.IsHideoK(managerId);

        if (_tadGChallengeMode)
        {
            _keywordVisibleSeconds *= 0.65f;
        }
    }

    private void PickTaskType()
    {
        if (keywords == null || keywords.Length == 0)
        {
            _taskType = DesignTaskType.UseAllKeywords;
            return;
        }

        int roll = Random.Range(0, 4);
        _taskType = roll switch
        {
            0 => DesignTaskType.FillMissingWord,
            1 => DesignTaskType.MultipleChoice,
            2 => DesignTaskType.Anagram,
            _ => DesignTaskType.CorrectTheError
        };
    }

    private void BuildTaskPrompt()
    {
        if (keywordText == null)
        {
            return;
        }

        string keyword = keywords[Random.Range(0, keywords.Length)];
        _expectedAnswer = keyword.ToLowerInvariant();

        switch (_taskType)
        {
            case DesignTaskType.FillMissingWord:
                keywordText.text =
                    "Fill in the missing word:\n\n" +
                    string.Format(MissingWordTemplates[Random.Range(0, MissingWordTemplates.Length)], "_____");
                break;

            case DesignTaskType.MultipleChoice:
                _multipleChoiceOptions = BuildMultipleChoiceOptions(keyword);
                keywordText.text = "Choose the correct design keyword:\n\n";
                for (int i = 0; i < _multipleChoiceOptions.Length; i++)
                {
                    keywordText.text += (char)('A' + i) + ") " + _multipleChoiceOptions[i] + "\n";
                }

                keywordText.text += "\nType the word or letter (A/B/C).";
                break;

            case DesignTaskType.Anagram:
                keywordText.text =
                    "Unscramble the letters:\n\n" +
                    ScrambleWord(keyword) +
                    "\n\nType the correct word.";
                break;

            case DesignTaskType.CorrectTheError:
                string misspelled = MisspellWord(keyword);
                keywordText.text =
                    "Find and correct the error:\n\n" +
                    string.Format(ErrorTemplates[Random.Range(0, ErrorTemplates.Length)], misspelled) +
                    "\n\nType the corrected keyword only.";
                break;

            default:
                keywordText.text = "Use all keywords:\n";
                foreach (string item in keywords)
                {
                    keywordText.text += "- " + item + "\n";
                }

                break;
        }
    }

    private void InitializeKeywordFade()
    {
        if (_taskType == DesignTaskType.UseAllKeywords)
        {
            _visibleKeywords.Clear();
            _visibleKeywords.AddRange(keywords);
            ShowKeywords();
        }

        _keywordFadeActive = _taskType == DesignTaskType.UseAllKeywords
            && (RandomEventSystem.IsCoffeeMissingActive() || RandomEventSystem.IsNeighborHammerDrillActive());
        _keywordTimer = GetEffectiveKeywordVisibleSeconds();

        if (resultText != null)
        {
            resultText.text = _tadGChallengeMode
                ? "Tad G. expects a tighter design document."
                : "";
        }
    }

    private void Update()
    {
        UpdateDesignInputBlock();
        UpdateProgressHud();

        if (StageEventSystem.IsBlocking)
        {
            return;
        }

        if (_hideoKBugHuntMode)
        {
            UpdateHideoKBugHunt();
            return;
        }

        if (!_keywordFadeActive || _visibleKeywords.Count == 0)
        {
            return;
        }

        _keywordTimer -= Time.deltaTime / RandomEventSystem.GetReactionSpeedMultiplier();
        if (_keywordTimer <= 0f)
        {
            _visibleKeywords.RemoveAt(0);
            ShowKeywords();
            _keywordTimer = GetEffectiveKeywordVisibleSeconds();
        }
    }

    private float GetEffectiveKeywordVisibleSeconds()
    {
        return _keywordVisibleSeconds
            * RandomEventSystem.GetDesignKeywordVisibleMultiplier()
            / RandomEventSystem.GetDesignKeywordFadeMultiplier();
    }

    private void ShowKeywords()
    {
        if (keywordText == null)
        {
            return;
        }

        keywordText.text = "Use all keywords:\n";
        foreach (string keyword in keywords)
        {
            if (_visibleKeywords.Contains(keyword))
            {
                keywordText.text += "- " + keyword + "\n";
            }
            else
            {
                keywordText.text += "- ???\n";
            }
        }
    }

    public void SubmitAnswer()
    {
        if (StageEventSystem.IsBlocking || _hideoKBugHuntMode)
        {
            return;
        }

        if (answerInput == null || resultText == null)
        {
            return;
        }

        string playerAnswer = NormalizeAnswer(answerInput.text);
        int qualityScore;

        if (_taskType == DesignTaskType.UseAllKeywords)
        {
            int usedKeywordCount = 0;
            foreach (string keyword in keywords)
            {
                if (playerAnswer.Contains(keyword.ToLowerInvariant()))
                {
                    usedKeywordCount++;
                }
            }

            qualityScore = Mathf.RoundToInt((usedKeywordCount / (float)keywords.Length) * 100f);
            resultText.text =
                "Keywords used: " + usedKeywordCount + " / " + keywords.Length +
                "\nQuality Score: " + qualityScore + " points";
        }
        else
        {
            bool correct = IsTaskAnswerCorrect(playerAnswer);
            qualityScore = correct ? 100 : IsCloseAnswer(playerAnswer) ? 60 : 0;
            resultText.text = correct
                ? "Correct!\nQuality Score: 100 points"
                : "Not quite right.\nQuality Score: " + qualityScore + " points";
        }

        if (MinigameEventSelectionController.IsDesignOldAgeSelected && qualityScore < 100)
        {
            resultText.text = "Old Age: one mistake failed the order.";
            FailCurrentOrder();
            return;
        }

        if (_tadGChallengeMode && qualityScore > 0)
        {
            qualityScore = Mathf.RoundToInt(qualityScore * 0.92f);
            resultText.text += "\nTad G. challenge modifier applied.";
        }

        TryEndMission((float)qualityScore / 100f);
    }

    private bool IsTaskAnswerCorrect(string playerAnswer)
    {
        if (string.IsNullOrWhiteSpace(playerAnswer))
        {
            return false;
        }

        if (_taskType == DesignTaskType.MultipleChoice)
        {
            if (playerAnswer.Length == 1 && playerAnswer[0] >= 'a' && playerAnswer[0] <= 'c')
            {
                int index = playerAnswer[0] - 'a';
                if (index >= 0 && index < _multipleChoiceOptions.Length)
                {
                    return NormalizeAnswer(_multipleChoiceOptions[index]) == _expectedAnswer;
                }
            }
        }

        return playerAnswer == _expectedAnswer;
    }

    private bool IsCloseAnswer(string playerAnswer)
    {
        if (string.IsNullOrWhiteSpace(playerAnswer) || string.IsNullOrWhiteSpace(_expectedAnswer))
        {
            return false;
        }

        return Mathf.Abs(playerAnswer.Length - _expectedAnswer.Length) <= 1
            && LevenshteinDistance(playerAnswer, _expectedAnswer) <= 2;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        int[,] distances = new int[left.Length + 1, right.Length + 1];
        for (int i = 0; i <= left.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (int j = 0; j <= right.Length; j++)
        {
            distances[0, j] = j;
        }

        for (int i = 1; i <= left.Length; i++)
        {
            for (int j = 1; j <= right.Length; j++)
            {
                int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                distances[i, j] = Mathf.Min(
                    Mathf.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[left.Length, right.Length];
    }

    private static string NormalizeAnswer(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().ToLowerInvariant();
    }

    private string[] BuildMultipleChoiceOptions(string correctKeyword)
    {
        List<string> options = new List<string> { correctKeyword };
        List<string> pool = keywords.Where(keyword => keyword != correctKeyword).ToList();

        while (options.Count < 3 && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            options.Add(pool[index]);
            pool.RemoveAt(index);
        }

        while (options.Count < 3)
        {
            options.Add("prototype");
        }

        for (int i = options.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (options[i], options[swapIndex]) = (options[swapIndex], options[i]);
        }

        return options.ToArray();
    }

    private static string ScrambleWord(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return "";
        }

        char[] letters = word.ToUpperInvariant().ToCharArray();
        for (int i = letters.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (letters[i], letters[swapIndex]) = (letters[swapIndex], letters[i]);
        }

        string scrambled = new string(letters);
        return scrambled == word.ToUpperInvariant() && word.Length > 1
            ? ScrambleWord(word)
            : scrambled;
    }

    private static string MisspellWord(string word)
    {
        if (word.Length < 3)
        {
            return word + "x";
        }

        int index = Random.Range(1, word.Length - 1);
        char[] letters = word.ToCharArray();
        char temp = letters[index];
        letters[index] = letters[index + 1];
        letters[index + 1] = temp;
        return new string(letters);
    }

    private void StartHideoKBugHunt()
    {
        if (answerInput != null)
        {
            answerInput.gameObject.SetActive(false);
        }

        if (keywordText != null)
        {
            keywordText.text =
                "Hideo K. mixed up the workflow.\n" +
                "Collect every falling keyword before time runs out.";
        }

        if (resultText != null)
        {
            resultText.text = "Collected: 0 / " + keywords.Length;
        }

        EnsureBugHuntArea();
        _bugHuntTimer = _bugHuntDuration;
        _bugHuntSpawnTimer = 0.2f;
        _bugHuntRunning = true;
        _collectedKeywords.Clear();
    }

    private void EnsureBugHuntArea()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        if (_bugHuntParentPanel == null)
        {
            RectTransform[] rectTransforms = canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rectTransform in rectTransforms)
            {
                if (rectTransform != null
                    && rectTransform.gameObject.name.Equals(
                        _bugHuntParentPanelName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    _bugHuntParentPanel = rectTransform;
                    break;
                }
            }
        }

        RectTransform parent = _bugHuntParentPanel != null
            ? _bugHuntParentPanel
            : canvas.transform as RectTransform;
        if (parent == null)
        {
            return;
        }

        if (_bugHuntArea != null)
        {
            if (_bugHuntArea.parent != parent)
            {
                _bugHuntArea.SetParent(parent, false);
            }
            return;
        }

        GameObject areaObject = new GameObject("BugHuntArea");
        areaObject.transform.SetParent(parent, false);
        _bugHuntArea = areaObject.AddComponent<RectTransform>();
        _bugHuntArea.anchorMin = new Vector2(0.1f, 0.15f);
        _bugHuntArea.anchorMax = new Vector2(0.9f, 0.75f);
        _bugHuntArea.offsetMin = Vector2.zero;
        _bugHuntArea.offsetMax = Vector2.zero;

        Image blocker = areaObject.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.08f);
        blocker.raycastTarget = false;
    }

    private void UpdateHideoKBugHunt()
    {
        if (!_bugHuntRunning)
        {
            return;
        }

        _bugHuntTimer -= Time.deltaTime;
        _bugHuntSpawnTimer -= Time.deltaTime;

        if (_bugHuntSpawnTimer <= 0f)
        {
            SpawnFallingKeyword();
            _bugHuntSpawnTimer = _bugHuntSpawnInterval;
        }

        UpdateFallingKeywords();

        if (_collectedKeywords.Count >= keywords.Length)
        {
            UpdateProgressHud();
            FinishBugHunt(1f);
            return;
        }

        if (_bugHuntTimer <= 0f)
        {
            float quality = keywords.Length > 0
                ? (float)_collectedKeywords.Count / keywords.Length
                : 0f;
            FinishBugHunt(quality);
        }
    }

    private void SpawnFallingKeyword()
    {
        if (_bugHuntArea == null || keywords == null || keywords.Length == 0)
        {
            return;
        }

        List<string> remaining = keywords
            .Where(keyword => !_collectedKeywords.Contains(keyword.ToLowerInvariant()))
            .ToList();
        if (remaining.Count == 0)
        {
            return;
        }

        string keyword = remaining[Random.Range(0, remaining.Count)];
        GameObject keywordObject = new GameObject("FallingKeyword");
        keywordObject.transform.SetParent(_bugHuntArea, false);

        RectTransform rect = keywordObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180f, 36f);
        rect.anchoredPosition = new Vector2(
            Random.Range(-_bugHuntArea.rect.width * 0.4f, _bugHuntArea.rect.width * 0.4f),
            _bugHuntArea.rect.height * 0.5f);

        Image background = keywordObject.AddComponent<Image>();
        background.color = new Color(0.15f, 0.45f, 0.85f, 0.92f);

        BoxCollider2D clickCollider = keywordObject.AddComponent<BoxCollider2D>();
        clickCollider.size = rect.sizeDelta;
        clickCollider.offset = Vector2.zero;
        clickCollider.isTrigger = true;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(keywordObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = keyword;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        FallingDesignKeyword fallingKeyword = keywordObject.AddComponent<FallingDesignKeyword>();
        fallingKeyword.Initialize(this, keyword, _bugHuntFallSpeed);

        _fallingKeywordObjects.Add(keywordObject);
    }

    private void UpdateFallingKeywords()
    {
        for (int i = _fallingKeywordObjects.Count - 1; i >= 0; i--)
        {
            GameObject keywordObject = _fallingKeywordObjects[i];
            if (keywordObject == null)
            {
                _fallingKeywordObjects.RemoveAt(i);
                continue;
            }

            RectTransform rect = keywordObject.GetComponent<RectTransform>();
            if (rect != null && rect.anchoredPosition.y < -_bugHuntArea.rect.height * 0.55f)
            {
                Destroy(keywordObject);
                _fallingKeywordObjects.RemoveAt(i);
                if (MinigameEventSelectionController.IsDesignOldAgeSelected)
                {
                    _bugHuntRunning = false;
                    if (resultText != null)
                    {
                        resultText.text = "Old Age: a missed keyword failed the order.";
                    }
                    FailCurrentOrder();
                    return;
                }
            }
        }
    }

    public void CollectKeyword(string keyword)
    {
        if (!_bugHuntRunning || string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        _collectedKeywords.Add(keyword.ToLowerInvariant());
        UpdateProgressHud();
        if (resultText != null)
        {
            resultText.text = "Collected: " + _collectedKeywords.Count + " / " + keywords.Length;
        }
    }

    private void FinishBugHunt(float quality)
    {
        _bugHuntRunning = false;
        foreach (GameObject keywordObject in _fallingKeywordObjects)
        {
            if (keywordObject != null)
            {
                Destroy(keywordObject);
            }
        }

        _fallingKeywordObjects.Clear();

        if (resultText != null)
        {
            resultText.text +=
                "\nQuality Score: " + Mathf.RoundToInt(quality * 100f) + " points";
        }

        if (MinigameEventSelectionController.IsDesignOldAgeSelected && quality < 0.999f)
        {
            if (resultText != null)
            {
                resultText.text = "Old Age: incomplete bug hunt failed the order.";
            }
            FailCurrentOrder();
            return;
        }

        TryEndMission(quality);
    }

    private void UpdateProgressHud()
    {
        if (_progressHud == null)
        {
            return;
        }

        if (_hideoKBugHuntMode)
        {
            float timePercent = _bugHuntDuration > 0f
                ? Mathf.Clamp01(_bugHuntTimer / _bugHuntDuration)
                : 0f;
            float bugHuntTaskPercent = keywords != null && keywords.Length > 0
                ? Mathf.Clamp01(_collectedKeywords.Count / (float)keywords.Length)
                : 0f;
            _progressHud.SetProgress(
                timePercent,
                "Time: " + Mathf.CeilToInt(Mathf.Max(0f, _bugHuntTimer)) + "s",
                bugHuntTaskPercent,
                "Collected: " + _collectedKeywords.Count + " / " + (keywords != null ? keywords.Length : 0));
            return;
        }

        float timerPercent = _keywordFadeActive && GetEffectiveKeywordVisibleSeconds() > 0f
            ? Mathf.Clamp01(_keywordTimer / GetEffectiveKeywordVisibleSeconds())
            : 1f;
        string timerText = _keywordFadeActive
            ? "Keyword visible: " + Mathf.CeilToInt(Mathf.Max(0f, _keywordTimer)) + "s"
            : "Time: no timer";

        float taskPercent = GetAnswerProgress();
        _progressHud.SetProgress(
            timerPercent,
            timerText,
            taskPercent,
            "Task: " + Mathf.RoundToInt(taskPercent * 100f) + "%");
    }

    private float GetAnswerProgress()
    {
        if (_taskType == DesignTaskType.UseAllKeywords)
        {
            if (keywords == null || keywords.Length == 0 || answerInput == null)
            {
                return 0f;
            }

            string playerAnswer = NormalizeAnswer(answerInput.text);
            int used = 0;
            foreach (string keyword in keywords)
            {
                if (playerAnswer.Contains(keyword.ToLowerInvariant()))
                {
                    used++;
                }
            }

            return used / (float)keywords.Length;
        }

        if (answerInput == null || string.IsNullOrEmpty(_expectedAnswer))
        {
            return 0f;
        }

        string typed = NormalizeAnswer(answerInput.text);
        if (typed == _expectedAnswer)
        {
            return 1f;
        }

        int matchingCharacters = 0;
        int max = Mathf.Min(typed.Length, _expectedAnswer.Length);
        for (int i = 0; i < max; i++)
        {
            if (typed[i] != _expectedAnswer[i])
            {
                break;
            }

            matchingCharacters++;
        }

        return matchingCharacters / (float)_expectedAnswer.Length;
    }

    private void FailCurrentOrder()
    {
        DeveloperSimulation developer = FindFirstObjectByType<DeveloperSimulation>();
        if (developer != null && developer.HasActiveOrder())
        {
            developer.Cheat(-1);
            return;
        }

        TryEndMission(0f);
    }

    private void TryEndMission(float score)
    {
        MissionObjects mission = GetComponentInParent<MissionObjects>();
        if (mission == null || !mission.CanAcceptCompletion)
        {
            return;
        }

        mission.MissionEnd(score);
    }

    private void UpdateDesignInputBlock()
    {
        if (answerInput == null)
        {
            return;
        }

        bool blocked = StageEventSystem.IsBlocking;
        answerInput.interactable = !blocked;
        answerInput.readOnly = blocked;

        if (blocked && answerInput.isFocused)
        {
            answerInput.DeactivateInputField();
            answerInput.ReleaseSelection();
        }
    }
}

public class FallingDesignKeyword : MonoBehaviour, IPointerClickHandler
{
    private DesignPuzzle _puzzle;
    private string _keyword;
    private float _fallSpeed;
    private RectTransform _rectTransform;
    private BoxCollider2D _clickCollider;
    private bool _collected;

    public void Initialize(DesignPuzzle puzzle, string keyword, float fallSpeed)
    {
        _puzzle = puzzle;
        _keyword = keyword;
        _fallSpeed = fallSpeed;
        _rectTransform = GetComponent<RectTransform>();
        _clickCollider = GetComponent<BoxCollider2D>();

        if (_clickCollider != null && _rectTransform != null)
        {
            _clickCollider.size = _rectTransform.rect.size;
            _clickCollider.offset = Vector2.zero;
        }
    }

    private void Update()
    {
        HandleColliderClick();

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition += Vector2.down * _fallSpeed * Time.deltaTime;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Collect();
    }

    private void OnMouseDown()
    {
        Collect();
    }

    private void HandleColliderClick()
    {
        if (_collected
            || _clickCollider == null
            || _rectTransform == null
            || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        Camera eventCamera = GetEventCamera();
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _rectTransform,
                Input.mousePosition,
                eventCamera,
                out Vector3 worldPoint))
        {
            return;
        }

        if (_clickCollider.OverlapPoint(worldPoint))
        {
            Collect();
        }
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    private void Collect()
    {
        if (_collected)
        {
            return;
        }

        _collected = true;
        if (_puzzle != null)
        {
            _puzzle.CollectKeyword(_keyword);
        }

        Destroy(gameObject);
    }
}

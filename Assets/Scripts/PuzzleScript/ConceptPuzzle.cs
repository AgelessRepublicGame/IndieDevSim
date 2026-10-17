using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConceptPuzzle : MonoBehaviour
{
    private const int HintUnlockBoardLevel = 1;

    public Transform sample;
    public Transform result;
    public TextMeshProUGUI score;

    [Header("Hideo K Word Challenge")]
    [SerializeField] private string[] _wordBank = { "mechanic", "prototype", "feedback", "iteration", "vision" };
    [SerializeField] private TMP_InputField _bossWordInput;
    [SerializeField] private TextMeshProUGUI _bossPromptText;

    [Header("Hints")]
    [SerializeField] private Button _hintButton;
    [SerializeField] private TextMeshProUGUI _hintText;

    private bool _hideoKWordMode;
    private bool _tadGChallengeMode;
    private string _expectedWord = "";
    private float _positionScoreMultiplier = 1f;
    private int _hintLevel;
    private MiniGameProgressHud _progressHud;

    private void Start()
    {
        _progressHud = MiniGameProgressHud.Attach(gameObject);
        ConfigureHintButton();
        ConfigureFromMission();
        if (_hideoKWordMode)
        {
            StartHideoKWordChallenge();
        }
        UpdateProgressHud();
    }

    private void Update()
    {
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
    }

    public void ShowHint()
    {
        TextMeshProUGUI target = _hintText != null ? _hintText : score;
        if (target == null)
        {
            return;
        }

        if (!AreHintsUnlocked())
        {
            target.text = "Buy the Whiteboard to unlock hints.";
            return;
        }

        if (!_hideoKWordMode || string.IsNullOrEmpty(_expectedWord))
        {
            target.text = "Hint: Compare every result object with the matching sample position.";
            return;
        }

        if (_hintLevel == 0)
        {
            target.text = "Hint: Rearrange every displayed letter without adding or removing letters.";
        }
        else if (_hintLevel == 1)
        {
            target.text = "Hint: The word starts with '" +
                char.ToUpperInvariant(_expectedWord[0]) +
                "' and has " + _expectedWord.Length + " letters.";
        }
        else
        {
            target.text = "Hint: The word is \"" + _expectedWord + "\".";
        }

        _hintLevel++;
    }

    private static bool AreHintsUnlocked()
    {
        return PlayerPrefs.GetInt("WhiteBoardLevel", 0) >= HintUnlockBoardLevel;
    }

    private void ConfigureFromMission()
    {
        MissionObjects mission = FindMission();
        if (mission == null || mission.activeOrder == null)
        {
            return;
        }

        ManagerId managerId = mission.activeOrder.manager;
        _hideoKWordMode = BossMinigameConfig.IsHideoK(managerId);
        _tadGChallengeMode = BossMinigameConfig.IsTadG(managerId);

        if (_tadGChallengeMode)
        {
            _positionScoreMultiplier = 0.9f;
        }
    }

    public void CheckPositions()
    {
        if (_hintButton != null
            && EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == _hintButton.gameObject)
        {
            return;
        }

        if (_hideoKWordMode)
        {
            CheckWordChallenge();
            return;
        }

        float totalScore = 0;
        for (int i = 1; i < sample.childCount; i++)
        {
            float offset = Vector3.Distance(sample.GetChild(i).position - sample.GetChild(0).position,
                result.GetChild(i).position - result.GetChild(0).position);
            if (offset < 0.3f)
            {
                totalScore += 1;
            }
            else if (offset < 2)
            {
                totalScore += 1 - offset / 2;
            }
        }

        totalScore /= (sample.childCount - 1);
        totalScore *= _positionScoreMultiplier;
        if (MinigameEventSelectionController.IsConceptOldAgeSelected && totalScore < 0.999f)
        {
            if (score != null)
            {
                score.text = "Old Age: one mistake failed the order.";
            }
            FailCurrentOrder();
            return;
        }

        Debug.Log("Correct! Total Score: " + totalScore);
        score.text = "Score: " + (totalScore * 100).ToString("F0");
        TryEndMission(totalScore);
    }

    private void UpdateProgressHud()
    {
        if (_progressHud == null)
        {
            return;
        }

        float taskPercent = _hideoKWordMode
            ? GetWordProgress()
            : GetPositionProgress();
        _progressHud.SetProgress(
            1f,
            "Time: no timer",
            taskPercent,
            "Task: " + Mathf.RoundToInt(taskPercent * 100f) + "%");
    }

    private float GetPositionProgress()
    {
        if (sample == null || result == null || sample.childCount <= 1)
        {
            return 0f;
        }

        int count = Mathf.Min(sample.childCount, result.childCount);
        if (count <= 1)
        {
            return 0f;
        }

        int matched = 0;
        for (int i = 1; i < count; i++)
        {
            float offset = Vector3.Distance(
                sample.GetChild(i).position - sample.GetChild(0).position,
                result.GetChild(i).position - result.GetChild(0).position);
            if (offset < 0.3f)
            {
                matched++;
            }
        }

        return matched / (float)(count - 1);
    }

    private float GetWordProgress()
    {
        if (_bossWordInput == null || string.IsNullOrEmpty(_expectedWord))
        {
            return 0f;
        }

        string typed = _bossWordInput.text.Trim().ToLowerInvariant();
        int matchingCharacters = 0;
        int max = Mathf.Min(typed.Length, _expectedWord.Length);
        for (int i = 0; i < max; i++)
        {
            if (typed[i] != _expectedWord[i])
            {
                break;
            }

            matchingCharacters++;
        }

        return matchingCharacters / (float)_expectedWord.Length;
    }

    private void StartHideoKWordChallenge()
    {
        if (sample != null)
        {
            sample.gameObject.SetActive(false);
        }

        if (result != null)
        {
            result.gameObject.SetActive(false);
        }

        EnsureBossWordUi();
        _expectedWord = _wordBank[Random.Range(0, _wordBank.Length)].ToLowerInvariant();

        if (_bossPromptText != null)
        {
            _bossPromptText.text =
                "Hideo K. mixed up the workflow.\n" +
                "This concept stage now needs a written keyword.\n\n" +
                "Unscramble and type this word:\n" +
                ScrambleWord(_expectedWord);
            _bossPromptText.gameObject.SetActive(true);
        }

        if (_bossWordInput != null)
        {
            _bossWordInput.gameObject.SetActive(true);
            _bossWordInput.text = "";
        }

        if (score != null)
        {
            score.text = "Type the unscrambled keyword, then press Check.";
        }
    }

    private void EnsureBossWordUi()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        TMP_FontAsset font = score != null ? score.font : null;
        RectTransform checkButtonRect = FindCheckButtonRect(canvas.transform);
        RectTransform referenceInputRect = FindReferenceInputRect(canvas.transform);

        if (_bossPromptText == null)
        {
            GameObject promptObject = new GameObject("BossPrompt");
            promptObject.transform.SetParent(canvas.transform, false);
            RectTransform promptRect = promptObject.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.1f, 0.12f);
            promptRect.anchorMax = new Vector2(0.9f, 0.28f);
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;

            _bossPromptText = promptObject.AddComponent<TextMeshProUGUI>();
            _bossPromptText.fontSize = 35f;
            _bossPromptText.alignment = TextAlignmentOptions.TopLeft;
            _bossPromptText.color = Color.white;
            if (font != null)
            {
                _bossPromptText.font = font;
            }
        }

        if (_bossWordInput == null)
        {
            GameObject inputObject = new GameObject("BossWordInput");
            inputObject.transform.SetParent(canvas.transform, false);
            RectTransform inputRect = inputObject.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.15f, 0.3f);
            inputRect.anchorMax = new Vector2(0.85f, 0.38f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;

            Image background = inputObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.98f);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(inputObject.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            
            textRect.offsetMin = new Vector2(12f, 6f);
            textRect.offsetMax = new Vector2(-12f, -6f);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 40f;
            text.color = Color.black;
            if (font != null)
            {
                text.font = font;
            }

            GameObject placeholderObject = new GameObject("Placeholder");
            placeholderObject.transform.SetParent(inputObject.transform, false);
            RectTransform placeholderRect = placeholderObject.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(12f, 6f);
            placeholderRect.offsetMax = new Vector2(-12f, -6f);
            TextMeshProUGUI placeholder = placeholderObject.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Type the keyword...";
            placeholder.fontSize = 40f;
            placeholder.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);
            if (font != null)
            {
                placeholder.font = font;
            }

            _bossWordInput = inputObject.AddComponent<TMP_InputField>();
            _bossWordInput.textComponent = text;
            _bossWordInput.placeholder = placeholder;
            _bossWordInput.targetGraphic = background;
        }
        else if (_bossWordInput.textComponent != null)
        {
            _bossWordInput.textComponent.color = Color.black;
        }

        if (_bossWordInput != null && _bossWordInput.textComponent != null)
        {
            _bossWordInput.textComponent.textWrappingMode = TextWrappingModes.Normal;
        }

        LayoutBossWordUi(checkButtonRect, referenceInputRect);
        _bossPromptText.transform.SetAsLastSibling();
        _bossWordInput.transform.SetAsLastSibling();
    }

    private void LayoutBossWordUi(RectTransform checkButtonRect, RectTransform referenceInputRect)
    {
        if (_bossPromptText == null || _bossWordInput == null)
        {
            return;
        }

        RectTransform promptRect = _bossPromptText.rectTransform;
        RectTransform inputRect = _bossWordInput.GetComponent<RectTransform>();

        if (referenceInputRect != null)
        {
            CopyRectLayout(referenceInputRect, inputRect);
        }

        if (checkButtonRect != null)
        {
            if (referenceInputRect == null)
            {
                inputRect.anchorMin = new Vector2(0.5f, 0.5f);
                inputRect.anchorMax = new Vector2(0.5f, 0.5f);
                inputRect.pivot = new Vector2(1.0f, 0.0f);
                inputRect.sizeDelta = new Vector2(700f, 200f);
                // inputRect.sizeDelta = new Vector2(Mathf.Max(checkButtonRect.sizeDelta.x + 120f, 420f), 52f);
                inputRect.anchoredPosition = checkButtonRect.anchoredPosition + new Vector2(0f, 150f);
            }

            promptRect.anchorMin = checkButtonRect.anchorMin;
            promptRect.anchorMax = checkButtonRect.anchorMax;
            promptRect.pivot = new Vector2(0.5f, 1f);
            promptRect.sizeDelta = new Vector2(Mathf.Max(checkButtonRect.sizeDelta.x + 200f, 520f), 120f);
            float checkBottomY = checkButtonRect.anchoredPosition.y - (checkButtonRect.sizeDelta.y * checkButtonRect.pivot.y);
            promptRect.anchoredPosition = new Vector2(checkButtonRect.anchoredPosition.x, checkBottomY - 8f);
        }
    }

    private RectTransform FindReferenceInputRect(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        TMP_InputField[] inputs = root.GetComponentsInChildren<TMP_InputField>(true);
        foreach (TMP_InputField input in inputs)
        {
            if (input == null || input == _bossWordInput)
            {
                continue;
            }

            return input.GetComponent<RectTransform>();
        }

        return null;
    }

    private static void CopyRectLayout(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.offsetMin = source.offsetMin;
        target.offsetMax = source.offsetMax;
    }

    private static RectTransform FindCheckButtonRect(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            string name = button.name.ToLowerInvariant();
            if (name.Contains("check") || name.Contains("submit"))
            {
                return button.GetComponent<RectTransform>();
            }
        }

        return null;
    }

    private MissionObjects FindMission()
    {
        MissionObjects mission = GetComponentInParent<MissionObjects>();
        if (mission != null)
        {
            return mission;
        }

        Transform current = transform;
        while (current != null)
        {
            mission = current.GetComponent<MissionObjects>();
            if (mission != null)
            {
                return mission;
            }

            current = current.parent;
        }

        return FindFirstObjectByType<MissionObjects>();
    }

    private void CheckWordChallenge()
    {
        string answer = _bossWordInput != null ? _bossWordInput.text.Trim().ToLowerInvariant() : "";
        float quality = answer == _expectedWord ? 1f : answer.Length > 0 ? 0.45f : 0f;

        if (MinigameEventSelectionController.IsConceptOldAgeSelected && quality < 0.999f)
        {
            if (score != null)
            {
                score.text = "Old Age: one mistake failed the order.";
            }
            FailCurrentOrder();
            return;
        }

        if (score != null)
        {
            score.text = quality >= 1f
                ? "Correct keyword!\nScore: 100"
                : "Close, but not exact.\nScore: " + (quality * 100f).ToString("F0");
        }

        TryEndMission(quality);
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
        MissionObjects mission = FindMission();
        if (mission == null || !mission.CanAcceptCompletion)
        {
            return;
        }

        mission.MissionEnd(score);
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
}

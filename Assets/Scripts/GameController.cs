using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
public enum Items
{
    None,
    Coffee,
    WhiteBoard,
    Desk,
    Colleague

}
public enum GameState

{
    Running,
    Stopped,
    NewGame,
}

[System.Serializable]
public class HideoCompletionNpcVariant
{
    [Tooltip("Complete NPC object. Configure its sprite, hierarchy, scale, name UI, and collider in the prefab.")]
    public GameObject prefab;
    public string npcName = "Manager";
    [TextArea] public string description = "Let's talk.";
    [Tooltip("Scene loaded when the player presses E near this NPC.")]
    public string destinationSceneName;
}

public class GameController : MonoBehaviour
{
    public DeveloperSimulation developerSimulation;
    public bool DeskUpdated;
    public bool CoffeeUpdated;
    public bool WhiteBoardUpdated;
    public bool ColleagueUpdated;
    [FormerlySerializedAs("managers")]
    public GameObject[] managerPrefabs;
    public GameObject[] Desks;
    public GameObject Coffee;
    public GameObject WhiteBoard;
    public GameObject ColleagueDesk;
    public GameObject DeskBtn;
    public GameObject BoardBtn;
    public GameObject CoffeeBtn;
    public GameObject ColleagueBtn;
    [Header("Office Upgrade Timers")]
    [SerializeField] private float _deskNextLevelSeconds = 180f;
    [SerializeField] private float _coffeeNextLevelSeconds = 180f;
    [SerializeField] private float _whiteBoardNextLevelSeconds = 180f;
    [SerializeField] private float _colleagueNextLevelSeconds = 180f;
    [SerializeField] private TMP_Text _deskUpgradeTimerText;
    [SerializeField] private TMP_Text _coffeeUpgradeTimerText;
    [SerializeField] private TMP_Text _whiteBoardUpgradeTimerText;
    [SerializeField] private TMP_Text _colleagueUpgradeTimerText;
    [SerializeField] private Image _deskUpgradeProgressFill;
    [SerializeField] private Image _coffeeUpgradeProgressFill;
    [SerializeField] private Image _whiteBoardUpgradeProgressFill;

    [Header("Objective Board")]
    [SerializeField] private ObjectiveBoardController _objectiveBoard;
    [SerializeField] [Min(1)] private int _colleagueDeskObjectiveNumber = 2;
    [SerializeField] [Min(1)] private int _allUpgradesObjectiveNumber = 3;

    [SerializeField] private Transform _confirmPurchasePopup;
    [SerializeField] private Transform _pausePopup;
    [SerializeField] private Transform _rentPopup;
    [SerializeField] private TMP_Text _rentValueText;
    [SerializeField] private float _dayDurationSeconds = 300f;
    [SerializeField] private int _lowLevelMax = 3;
    [SerializeField] private int _midLevelMax = 6;
    [SerializeField] private string _storyIntroSceneName = "StoryIntro";
    [Tooltip("Normal walking speed used with or without the coffee machine.")]
    [SerializeField] private float _defaultMovementSpeed = 3f;
    [Tooltip("Shift speed unlocked after buying the coffee machine.")]
    [SerializeField] private float _coffeeMovementSpeed = 8f;

    [Header("Minigame Typing Sound")]
    [SerializeField] private AudioClip _minigameTypingSound;
    [SerializeField] [Range(0f, 1f)] private float _minigameTypingVolume = 0.7f;
    [SerializeField] private Vector2 _minigameTypingPitchRange = new Vector2(0.95f, 1.05f);
    public int RockStarrRequiredOrders = 35;
    public int BetesdaRequiredOrders = 60;
    public int TadGRequiredPerfectQualityOrders = 25;
    public int TadGRequiredHighQualityOrders = 25;
    public int HideoKRequiredPerfectQualityOrders = 50;

    [Header("Hideo K Completion NPC")]
    [Tooltip("One complete NPC prefab is selected randomly. Configure the full NPC appearance inside each prefab.")]
    [SerializeField] private HideoCompletionNpcVariant[] _hideoCompletionNpcVariants;
    [SerializeField] private Vector3 _hideoCompletionNpcOffset = new Vector3(1.5f, 0f, 0f);
    [SerializeField] private Vector3 _hideoCompletionNpcFallbackPosition = new Vector3(0f, 0f, 0f);
    [SerializeField] private Transform _hideoCompletionNpcParent;
    [SerializeField] private string _hideoCompletionNpcName = "Manager";
    [SerializeField] [TextArea] private string _hideoCompletionNpcDescription = "Let's talk.";
    [Tooltip("Font size of the description displayed above the spawned NPC.")]
    [SerializeField] [Min(0.1f)] private float _hideoCompletionNpcDescriptionFontSize = 3f;
    [Tooltip("Local position of the description relative to the spawned NPC.")]
    [SerializeField] private Vector3 _hideoCompletionNpcDescriptionPosition = new Vector3(0f, 1.2f, 0f);
    [Tooltip("Scene loaded when the player interacts with this NPC.")]
    [SerializeField] private string _hideoCompletionNpcSceneName;

    [Header("Hideo K Completion Firework")]
    [Tooltip("ParticleSystem prefab played after a perfect Hideo K order.")]
    [SerializeField] private ParticleSystem _hideoCompletionFireworkPrefab;
    [Tooltip("Viewport position: (0.5, 0.5) is the middle of the screen.")]
    [SerializeField] private Vector2 _hideoCompletionFireworkScreenPosition = new Vector2(0.5f, 0.5f);
    [Tooltip("Distance from the camera used to place the particle in world space.")]
    [SerializeField] [Min(0.01f)] private float _hideoCompletionFireworkCameraDistance = 5f;
    [Tooltip("Additional world-space position adjustment after screen placement.")]
    [SerializeField] private Vector3 _hideoCompletionFireworkPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 _hideoCompletionFireworkScale = Vector3.one;
    [SerializeField] [Min(0.1f)] private float _hideoCompletionFireworkLifetime = 6f;

    private const string HideoCompletionNpcUnlockedKey = "HideoCompletionNpcUnlocked";
    public const string HideoCompletionNpcAppearanceKey = "HideoCompletionNpcAppearance";
    private const string HideoCompletionNpcPositionXKey = "HideoCompletionNpcPositionX";
    private const string HideoCompletionNpcPositionYKey = "HideoCompletionNpcPositionY";
    private const string HideoCompletionNpcPositionZKey = "HideoCompletionNpcPositionZ";
    private const float PerfectTaskQuality = 0.999f;
    private GameObject _hideoCompletionNpcInstance;
    private static readonly ManagerId[] ManagerProgression =
    {
        ManagerId.Metel,
        ManagerId.CDProjec,
        ManagerId.ElArt,
        ManagerId.Ubisoff,
        ManagerId.RockStarr,
        ManagerId.Betesda,
        ManagerId.TadG,
        ManagerId.HideoK
    };
    //    public GameObject DeptMsg;
    [SerializeField] private Animation _notEnoughMoney;
    public Sprite newDesk;
    int gameSpeed = 1;
    private Items _confirmPurcahseId = Items.None;
    private float _rentDayElapsed;
    private int _currentRent;
    private bool _rentPaymentDue;
    private int _deskLevel;
    private int _coffeeLevel;
    private int _whiteBoardLevel;
    private int _colleagueLevel;
    private float _deskNextLevelRemaining;
    private float _coffeeNextLevelRemaining;
    private float _whiteBoardNextLevelRemaining;
    private float _colleagueNextLevelRemaining;
    public static GameState GameState;
    public static bool IsLoadingStoryIntro { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        IsLoadingStoryIntro = ShouldLoadStoryIntro();
    }

    private void OnEnable()
    {
        DeveloperSimulation.OrderSubmittedDetailed += HandleOrderSubmittedDetailed;
    }

    private void OnDisable()
    {
        DeveloperSimulation.OrderSubmittedDetailed -= HandleOrderSubmittedDetailed;
    }

    void Start()
    {
        if (IsLoadingStoryIntro)
        {
            SceneManager.LoadScene(_storyIntroSceneName);
            return;
        }

        IsLoadingStoryIntro = false;
        AddUpgradeHoverScales();

        developerSimulation = FindFirstObjectByType<DeveloperSimulation>();
        EnsureRandomEventController();
        LoadOfficeUpgradeProgress();
        EnsureUpgradeTimerLabels();
        DeskUpdated = _deskLevel >= 1;
        CoffeeUpdated = _coffeeLevel >= 1;
        WhiteBoardUpdated = _whiteBoardLevel >= 1;
        ColleagueUpdated = _colleagueLevel >= 1;
        developerSimulation.devInfo.money = PlayerPrefs.GetInt("Money", 100);
        developerSimulation.devInfo.level = PlayerPrefs.GetInt("Level", 1);
        developerSimulation.devInfo.experience = PlayerPrefs.GetInt("Experience", 0);
        developerSimulation.devInfo.taskCompleted = PlayerPrefs.GetInt("TaskCompleted", 0);
        developerSimulation.devInfo.perfectQualityOrdersCompleted = PlayerPrefs.GetInt("PerfectQualityOrdersCompleted", 0);
        developerSimulation.devInfo.highQualityOrdersCompleted = PlayerPrefs.GetInt("HighQualityOrdersCompleted", 0);
        _rentDayElapsed = PlayerPrefs.GetFloat("RentDayElapsed", 0f);
        _rentPaymentDue = PlayerPrefs.GetInt("RentPaymentDue", 0) == 1;
        _currentRent = PlayerPrefs.GetInt("CurrentRent", 0);
        if (_rentPopup != null)
        {
            _rentPopup.gameObject.SetActive(false);
        }
        if (DeskUpdated)
        {
            updateDesk(false);
        }
        if (CoffeeUpdated)
        {
            updateCoffee(false);
        }
        if (WhiteBoardUpdated)
        {
            updateWhiteBoard(false);
        }
        if (ColleagueUpdated)
        {
            updateColleague(false);
            CompleteColleagueDeskObjective();
        }
        CompleteAllUpgradesObjectiveIfReady();
        if (MinigameEventSelectionController.IsHideoEventTaskActive)
        {
            SetOfficeUpgradesSuppressed(
                MinigameEventSelectionController.IsOldAgeSelected);
        }
        else
        {
            ApplyPlayerMovementSettings(false);
        }

        TrySpawnHideoCompletionNpc();

        if (_rentPaymentDue)
        {
            if (_currentRent <= 0)
            {
                _currentRent = GetDailyRentForLevel(developerSimulation.devInfo.level);
            }
            DisplayRentPopup();
        }
        else
        {
            GameState = GameState.Running;
        }
    }

    private void AddUpgradeHoverScales()
    {
        AddHoverScale(DeskBtn);
        AddHoverScale(BoardBtn);
        AddHoverScale(CoffeeBtn);
        AddHoverScale(ColleagueBtn);
    }

    private void AddHoverScale(GameObject target)
    {
        if (target != null && target.GetComponent<HoverScaleEffect>() == null)
        {
            target.AddComponent<HoverScaleEffect>();
        }
    }

    public void ResetValues()
    {
        NewGame();
    }

    private bool ShouldLoadStoryIntro()
    {
        return PlayerPrefs.GetInt(StoryIntroController.StoryIntroCompleteKey, 0) == 0
            && !string.IsNullOrEmpty(_storyIntroSceneName)
            && SceneManager.GetActiveScene().name != _storyIntroSceneName
            && Application.CanStreamedLevelBeLoaded(_storyIntroSceneName);
    }
    private void OnNotEnoghMoney()
    {
        _notEnoughMoney.Play();
    }
    private void EnableConfirmPopup(Items toPucahse)
    {
        _confirmPurcahseId = toPucahse;
        _confirmPurchasePopup.gameObject.SetActive(true);
        GameState = GameState.Stopped;
    }
    public void DisableConfirmPurchasePopup()
    {
        _confirmPurcahseId = Items.None;
        _confirmPurchasePopup.gameObject.SetActive(false);
        GameState = GameState.Running;
    }
    public void ConfirmPurchase()
    {
        switch (_confirmPurcahseId)
        {
            case Items.Coffee:
                updateCoffee(true);
                break;
            case Items.Desk:
                updateDesk(true);
                break;
            case Items.WhiteBoard:
                updateWhiteBoard(true);
                break;
            case Items.Colleague:
                updateColleague(true);
                break;
        }
        DisableConfirmPurchasePopup();
    }
    public void updateDesk(bool payment)
    {

        if (payment)
        {
            if (developerSimulation.devInfo.money < 1000)
            {
                OnNotEnoghMoney();
                return;
            }
            if (!_confirmPurchasePopup.gameObject.activeSelf)
            {
                EnableConfirmPopup(Items.Desk);
                return;
            }

            developerSimulation.RemoveMoney(1000);
            _deskLevel = Mathf.Max(1, _deskLevel);
            StartOfficeUpgradeTimer(Items.Desk);
        }
        foreach (var desk in Desks)
        {
            desk.GetComponent<SpriteRenderer>().sprite = newDesk;
        }
        developerSimulation.moneyboost = true;
        DeskUpdated = true;
        CompleteAllUpgradesObjectiveIfReady();
        RefreshOfficeUpgradeTimerLabels();
        UpdateUpgradeButtonAvailability();
    }
    public void updateCoffee(bool payment)
    {

        if (payment)
        {
            if (developerSimulation.devInfo.money < 2000)
            {
                OnNotEnoghMoney();
                return;
            }
            if (!_confirmPurchasePopup.gameObject.activeSelf)
            {
                EnableConfirmPopup(Items.Coffee);
                return;
            }
            developerSimulation.RemoveMoney(2000);
            _coffeeLevel = Mathf.Max(1, _coffeeLevel);
            StartOfficeUpgradeTimer(Items.Coffee);
        }
        Coffee.SetActive(true);
        CoffeeUpdated = true;
        ApplyPlayerMovementSettings(false);
        CompleteAllUpgradesObjectiveIfReady();
        RefreshOfficeUpgradeTimerLabels();
        UpdateUpgradeButtonAvailability();
    }
    public void updateWhiteBoard(bool payment)
    {
        if (payment)
        {
            if (developerSimulation.devInfo.money < 3000)
            {
                OnNotEnoghMoney();
                return;
            }
            if (!_confirmPurchasePopup.gameObject.activeSelf)
            {
                EnableConfirmPopup(Items.WhiteBoard);
                return;
            }
            developerSimulation.RemoveMoney(3000);
            _whiteBoardLevel = Mathf.Max(1, _whiteBoardLevel);
            StartOfficeUpgradeTimer(Items.WhiteBoard);
        }

        developerSimulation.scoreBoost = true;
        WhiteBoard.SetActive(true);
        WhiteBoardUpdated = true;
        CompleteAllUpgradesObjectiveIfReady();
        RefreshOfficeUpgradeTimerLabels();
        UpdateUpgradeButtonAvailability();
    }
    public void updateColleague(bool payment)
    {
        if (payment)
        {
            if (developerSimulation.devInfo.money < 5000)
            {
                OnNotEnoghMoney();
                return;
            }
            if (!_confirmPurchasePopup.gameObject.activeSelf)
            {
                EnableConfirmPopup(Items.Colleague);
                return;
            }
            developerSimulation.RemoveMoney(5000);
            _colleagueLevel = Mathf.Max(1, _colleagueLevel);
            StartOfficeUpgradeTimer(Items.Colleague);
        }

        ColleagueDesk.SetActive(true);
        ColleagueDesk.GetComponent<ColleagueDesk>().SetPurchased(true);
        ColleagueUpdated = true;
        if (payment)
        {
            CompleteColleagueDeskObjective();
        }
        CompleteAllUpgradesObjectiveIfReady();
        RefreshOfficeUpgradeTimerLabels();
        UpdateUpgradeButtonAvailability();
    }

    private void CompleteColleagueDeskObjective()
    {
        ObjectiveBoardController objectiveBoard = GetObjectiveBoard();
        if (objectiveBoard != null)
        {
            objectiveBoard.FinishObjective(_colleagueDeskObjectiveNumber);
        }
    }

    private void CompleteAllUpgradesObjectiveIfReady()
    {
        if (!DeskUpdated || !CoffeeUpdated || !WhiteBoardUpdated || !ColleagueUpdated)
        {
            return;
        }

        ObjectiveBoardController objectiveBoard = GetObjectiveBoard();
        if (objectiveBoard != null)
        {
            objectiveBoard.FinishObjective(_allUpgradesObjectiveNumber);
        }
    }

    private ObjectiveBoardController GetObjectiveBoard()
    {
        if (_objectiveBoard == null)
        {
            _objectiveBoard = FindFirstObjectByType<ObjectiveBoardController>();
        }

        return _objectiveBoard;
    }

    public void SetOfficeUpgradesSuppressed(bool suppressed)
    {
        if (developerSimulation == null)
        {
            developerSimulation = FindFirstObjectByType<DeveloperSimulation>();
        }

        if (developerSimulation != null)
        {
            developerSimulation.scoreBoost = !suppressed && WhiteBoardUpdated;
        }

        SimpleTopDownCharacterController player = FindFirstObjectByType<SimpleTopDownCharacterController>();
        if (player != null)
        {
            player.ConfigureMovement(
                _defaultMovementSpeed,
                !suppressed && CoffeeUpdated,
                _coffeeMovementSpeed);
        }

        ColleagueDesk colleague = FindFirstObjectByType<ColleagueDesk>();
        if (colleague != null)
        {
            colleague.SetUpgradeSuppressed(suppressed);
        }
    }

    private void ApplyPlayerMovementSettings(bool upgradesSuppressed)
    {
        SimpleTopDownCharacterController player = FindFirstObjectByType<SimpleTopDownCharacterController>();
        if (player == null)
        {
            return;
        }

        player.ConfigureMovement(
            _defaultMovementSpeed,
            !upgradesSuppressed && CoffeeUpdated,
            _coffeeMovementSpeed);
    }

    private void LoadOfficeUpgradeProgress()
    {
        _deskLevel = PlayerPrefs.GetInt("DeskLevel", 0);
        _coffeeLevel = PlayerPrefs.GetInt("CoffeeLevel", 0);
        _whiteBoardLevel = PlayerPrefs.GetInt("WhiteBoardLevel", 0);
        _colleagueLevel = PlayerPrefs.GetInt("ColleagueLevel", 0);
        _deskNextLevelRemaining = PlayerPrefs.GetFloat("DeskNextLevelRemaining", 0f);
        _coffeeNextLevelRemaining = PlayerPrefs.GetFloat("CoffeeNextLevelRemaining", 0f);
        _whiteBoardNextLevelRemaining = PlayerPrefs.GetFloat("WhiteBoardNextLevelRemaining", 0f);
        _colleagueNextLevelRemaining = PlayerPrefs.GetFloat("ColleagueNextLevelRemaining", 0f);

        RestoreMissingUpgradeTimer(ref _deskLevel, ref _deskNextLevelRemaining, _deskNextLevelSeconds);
        RestoreMissingUpgradeTimer(ref _coffeeLevel, ref _coffeeNextLevelRemaining, _coffeeNextLevelSeconds);
        RestoreMissingUpgradeTimer(ref _whiteBoardLevel, ref _whiteBoardNextLevelRemaining, _whiteBoardNextLevelSeconds);
        NormalizeOfficeUpgradeChain();
    }

    private void NormalizeOfficeUpgradeChain()
    {
        if (_coffeeLevel >= 1)
        {
            _deskLevel = Mathf.Max(_deskLevel, 2);
            _deskNextLevelRemaining = 0f;
        }

        if (_whiteBoardLevel >= 1)
        {
            _coffeeLevel = Mathf.Max(_coffeeLevel, 2);
            _coffeeNextLevelRemaining = 0f;
        }

        if (_colleagueLevel >= 1)
        {
            _whiteBoardLevel = Mathf.Max(_whiteBoardLevel, 2);
            _whiteBoardNextLevelRemaining = 0f;
        }

        _colleagueNextLevelRemaining = 0f;
    }

    private void RestoreMissingUpgradeTimer(ref int level, ref float remaining, float duration)
    {
        if (level == 1 && remaining <= 0f)
        {
            remaining = Mathf.Max(1f, duration);
        }
    }

    private void StartOfficeUpgradeTimer(Items item)
    {
        switch (item)
        {
            case Items.Desk:
                if (_deskLevel == 1 && _deskNextLevelRemaining <= 0f)
                {
                    _deskNextLevelRemaining = Mathf.Max(1f, _deskNextLevelSeconds);
                }
                break;
            case Items.Coffee:
                if (_coffeeLevel == 1 && _coffeeNextLevelRemaining <= 0f)
                {
                    _coffeeNextLevelRemaining = Mathf.Max(1f, _coffeeNextLevelSeconds);
                }
                break;
            case Items.WhiteBoard:
                if (_whiteBoardLevel == 1 && _whiteBoardNextLevelRemaining <= 0f)
                {
                    _whiteBoardNextLevelRemaining = Mathf.Max(1f, _whiteBoardNextLevelSeconds);
                }
                break;
            case Items.Colleague:
                break;
        }

        RefreshOfficeUpgradeTimerLabels();
        UpdateUpgradeButtonAvailability();
    }

    private void UpdateOfficeUpgradeTimers()
    {
        bool changed = false;
        changed |= TickOfficeUpgradeTimer(ref _deskNextLevelRemaining, ref _deskLevel);
        changed |= TickOfficeUpgradeTimer(ref _coffeeNextLevelRemaining, ref _coffeeLevel);
        changed |= TickOfficeUpgradeTimer(ref _whiteBoardNextLevelRemaining, ref _whiteBoardLevel);

        if (changed)
        {
            RefreshOfficeUpgradeTimerLabels();
            UpdateUpgradeButtonAvailability();
        }
    }

    private bool TickOfficeUpgradeTimer(ref float remaining, ref int level)
    {
        if (level != 1 || remaining <= 0f)
        {
            return false;
        }

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        if (remaining <= 0f)
        {
            level = 2;
            return true;
        }

        return true;
    }

    private void RefreshOfficeUpgradeTimerLabels()
    {
        SetUpgradeTimerText(
            _deskUpgradeTimerText,
            _deskUpgradeProgressFill,
            "Coffee",
            DeskUpdated,
            CoffeeUpdated,
            _deskLevel,
            _deskNextLevelRemaining,
            _deskNextLevelSeconds);
        SetUpgradeTimerText(
            _coffeeUpgradeTimerText,
            _coffeeUpgradeProgressFill,
            "Whiteboard",
            CoffeeUpdated,
            WhiteBoardUpdated,
            _coffeeLevel,
            _coffeeNextLevelRemaining,
            _coffeeNextLevelSeconds);
        SetUpgradeTimerText(
            _whiteBoardUpgradeTimerText,
            _whiteBoardUpgradeProgressFill,
            "Colleague",
            WhiteBoardUpdated,
            ColleagueUpdated,
            _whiteBoardLevel,
            _whiteBoardNextLevelRemaining,
            _whiteBoardNextLevelSeconds);
        if (_colleagueUpgradeTimerText != null)
        {
            _colleagueUpgradeTimerText.gameObject.SetActive(ColleagueUpdated);
            _colleagueUpgradeTimerText.text = ColleagueUpdated
                ? "All office upgrades unlocked"
                : "";
        }
    }

    private void SetUpgradeTimerText(
        TMP_Text label,
        Image fill,
        string nextUpgradeName,
        bool currentPurchased,
        bool nextPurchased,
        int currentLevel,
        float remaining,
        float duration)
    {
        if (label == null)
        {
            return;
        }

        if (!currentPurchased || nextPurchased)
        {
            label.gameObject.SetActive(false);
            SetProgressFillActive(fill, false, 0f);
            return;
        }

        label.gameObject.SetActive(true);
        if (currentLevel >= 2 || remaining <= 0f)
        {
            label.text = nextUpgradeName + " unlocked";
            SetProgressFillActive(fill, true, 1f);
            return;
        }

        label.text = nextUpgradeName + " unlocks in " + FormatUpgradeTime(remaining);
        float progress = duration > 0f
            ? 1f - Mathf.Clamp01(remaining / duration)
            : 1f;
        SetProgressFillActive(fill, true, progress);
    }

    private void SetProgressFillActive(Image fill, bool active, float amount)
    {
        if (fill == null)
        {
            return;
        }

        if (fill.transform.parent != null)
        {
            fill.transform.parent.gameObject.SetActive(active);
        }
        else
        {
            fill.gameObject.SetActive(active);
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

    private string FormatUpgradeTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = totalSeconds / 60;
        int remainder = totalSeconds % 60;
        return minutes.ToString("00") + ":" + remainder.ToString("00");
    }

    private void EnsureUpgradeTimerLabels()
    {
        _deskUpgradeTimerText = EnsureUpgradeTimerLabel(_deskUpgradeTimerText, DeskBtn, "DeskUpgradeTimer");
        _coffeeUpgradeTimerText = EnsureUpgradeTimerLabel(_coffeeUpgradeTimerText, CoffeeBtn, "CoffeeUpgradeTimer");
        _whiteBoardUpgradeTimerText = EnsureUpgradeTimerLabel(_whiteBoardUpgradeTimerText, BoardBtn, "WhiteBoardUpgradeTimer");
        _colleagueUpgradeTimerText = EnsureUpgradeTimerLabel(_colleagueUpgradeTimerText, ColleagueBtn, "ColleagueUpgradeTimer");
        _deskUpgradeProgressFill = EnsureUpgradeProgressFill(_deskUpgradeProgressFill, _deskUpgradeTimerText, "DeskToCoffeeProgress");
        _coffeeUpgradeProgressFill = EnsureUpgradeProgressFill(_coffeeUpgradeProgressFill, _coffeeUpgradeTimerText, "CoffeeToWhiteBoardProgress");
        _whiteBoardUpgradeProgressFill = EnsureUpgradeProgressFill(_whiteBoardUpgradeProgressFill, _whiteBoardUpgradeTimerText, "WhiteBoardToColleagueProgress");
        RefreshOfficeUpgradeTimerLabels();
        UpdateUpgradeButtonAvailability();
    }

    private TMP_Text EnsureUpgradeTimerLabel(TMP_Text label, GameObject button, string objectName)
    {
        if (label != null || button == null)
        {
            return label;
        }

        Transform parent = button.transform.parent;
        if (parent == null)
        {
            return label;
        }

        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            label = existing.GetComponent<TMP_Text>();
            if (label != null)
            {
                return label;
            }
        }

        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        RectTransform sourceRect = button.GetComponent<RectTransform>();
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        if (sourceRect != null)
        {
            labelRect.anchorMin = sourceRect.anchorMin;
            labelRect.anchorMax = sourceRect.anchorMax;
            labelRect.pivot = sourceRect.pivot;
            labelRect.sizeDelta = new Vector2(Mathf.Max(sourceRect.sizeDelta.x, 180f), 42f);
            labelRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -48f);
        }

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Image EnsureUpgradeProgressFill(Image fill, TMP_Text label, string objectName)
    {
        if (fill != null || label == null || label.transform.parent == null)
        {
            return fill;
        }

        Transform existing = label.transform.parent.Find(objectName);
        if (existing != null)
        {
            fill = existing.GetComponentInChildren<Image>();
            if (fill != null)
            {
                return fill;
            }
        }

        GameObject barObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barObject.transform.SetParent(label.transform.parent, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        if (labelRect != null)
        {
            barRect.anchorMin = labelRect.anchorMin;
            barRect.anchorMax = labelRect.anchorMax;
            barRect.pivot = labelRect.pivot;
            barRect.sizeDelta = new Vector2(Mathf.Max(labelRect.sizeDelta.x, 180f), 8f);
            barRect.anchoredPosition = labelRect.anchoredPosition + new Vector2(0f, -28f);
        }

        Image background = barObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.45f);
        background.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(barObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.zero;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fill = fillObject.GetComponent<Image>();
        fill.color = new Color(0.35f, 0.9f, 1f, 0.95f);
        fill.type = Image.Type.Simple;
        fill.raycastTarget = false;
        return fill;
    }

    private void UpdateUpgradeButtonAvailability()
    {
        SetUpgradeButtonAvailable(DeskBtn, !DeskUpdated);
        SetUpgradeButtonAvailable(CoffeeBtn, !CoffeeUpdated && DeskUpdated && _deskLevel >= 2);
        SetUpgradeButtonAvailable(BoardBtn, !WhiteBoardUpdated && CoffeeUpdated && _coffeeLevel >= 2);
        SetUpgradeButtonAvailable(ColleagueBtn, !ColleagueUpdated && WhiteBoardUpdated && _whiteBoardLevel >= 2);
    }

    private void SetUpgradeButtonAvailable(GameObject button, bool available)
    {
        if (button != null)
        {
            button.SetActive(available);
        }
    }

    public void AddTypingSoundToMinigame(GameObject miniGameObject)
    {
        if (miniGameObject == null || _minigameTypingSound == null)
        {
            return;
        }

        MinigameTypingSound typingSound = miniGameObject.GetComponent<MinigameTypingSound>();
        if (typingSound == null)
        {
            typingSound = miniGameObject.AddComponent<MinigameTypingSound>();
        }

        typingSound.Configure(
            _minigameTypingSound,
            _minigameTypingVolume,
            _minigameTypingPitchRange);
    }
    // Update is called once per frame
    void Update()
    {
        bool missionActive = IsMissionGameActive();
        SetObjectiveBoardForceHidden(missionActive);

        if (Input.GetKeyDown(KeyCode.Escape) && !missionActive && !_rentPaymentDue)
        {
            if (!_pausePopup.gameObject.activeSelf)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }

        if (GameState == GameState.Stopped) return;
        UpdateRentTimer();
        UpdateOfficeUpgradeTimers();
        SpeedControl();
        // DeptMsg.SetActive(developerSimulation.devInfo.money < 0);
        bool waitingForCompletionNpc =
            PlayerPrefs.GetInt(HideoCompletionNpcUnlockedKey, 0) == 1;
        if (!waitingForCompletionNpc && !FindFirstObjectByType<ManagerSimulation>())
        {
            GameObject managerPrefab = GetUnlockedManagerPrefab();
            if (managerPrefab != null)
            {
                Instantiate(managerPrefab);
            }
        }
        TrySpawnHideoCompletionNpc();
        GetComponent<Canvas>().enabled = !missionActive;
        if (_rentPaymentDue && !missionActive)
        {
            DisplayRentPopup();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            ResetValues();
        }

    }

    private void SetObjectiveBoardForceHidden(bool forceHidden)
    {
        ObjectiveBoardController objectiveBoard = GetObjectiveBoard();
        if (objectiveBoard != null)
        {
            objectiveBoard.SetForceHidden(forceHidden);
        }
    }
    private void UpdateRentTimer()
    {
        if (_rentPaymentDue)
        {
            return;
        }

        _rentDayElapsed += Time.deltaTime;
        if (_rentDayElapsed < Mathf.Max(1f, _dayDurationSeconds))
        {
            return;
        }

        _rentDayElapsed = 0f;
        ShowRentPopup();
    }

    private void ShowRentPopup()
    {
        _currentRent = GetDailyRentForLevel(developerSimulation.devInfo.level);
        _rentPaymentDue = true;
        if (IsMissionGameActive())
        {
            SaveGame();
            return;
        }

        DisplayRentPopup();
    }

    private bool IsMissionGameActive()
    {
        return MissionObjects.IsInMission
            || FindFirstObjectByType<ConceptPuzzle>() != null
            || FindFirstObjectByType<DesignPuzzle>() != null
            || FindFirstObjectByType<ProgrammingBugGame>() != null;
    }

    private void DisplayRentPopup()
    {
        if (_rentValueText != null)
        {
            _rentValueText.text = "Rent (" + _currentRent.ToString() + ")";
        }
        if (_rentPopup != null)
        {
            _rentPopup.gameObject.SetActive(true);
        }
        GameState = GameState.Stopped;
        SaveGame();
    }

    private int GetDailyRentForLevel(int level)
    {
        if (level <= _lowLevelMax)
        {
            return 100;
        }
        if (level <= _midLevelMax)
        {
            return 500;
        }

        return 1000;
    }

    private GameObject GetUnlockedManagerPrefab()
    {
        GameObject latestUnlockedPrefab = null;
        for (int i = 0; i < ManagerProgression.Length; i++)
        {
            ManagerId managerId = ManagerProgression[i];
            if (!IsManagerUnlocked(managerId))
            {
                break;
            }

            GameObject prefab = GetManagerPrefab(managerId);
            if (prefab != null)
            {
                latestUnlockedPrefab = prefab;
            }
        }

        return latestUnlockedPrefab;
    }

    private bool IsManagerUnlocked(ManagerId managerId)
    {
        switch (managerId)
        {
            case ManagerId.Metel:
                return true;
            case ManagerId.CDProjec:
                return developerSimulation.devInfo.level >= 2;
            case ManagerId.ElArt:
                return developerSimulation.devInfo.level >= 3;
            case ManagerId.Ubisoff:
                return developerSimulation.devInfo.level >= 4;
            case ManagerId.RockStarr:
                return IsManagerUnlocked(ManagerId.Ubisoff)
                    && developerSimulation.devInfo.taskCompleted >= RockStarrRequiredOrders;
            case ManagerId.Betesda:
                return IsManagerUnlocked(ManagerId.RockStarr)
                    && developerSimulation.devInfo.taskCompleted >= BetesdaRequiredOrders;
            case ManagerId.TadG:
                return IsManagerUnlocked(ManagerId.Betesda)
                    && developerSimulation.devInfo.perfectQualityOrdersCompleted >= TadGRequiredPerfectQualityOrders
                    && developerSimulation.devInfo.highQualityOrdersCompleted >= TadGRequiredHighQualityOrders;
            case ManagerId.HideoK:
                return IsManagerUnlocked(ManagerId.TadG)
                    && developerSimulation.devInfo.perfectQualityOrdersCompleted >= HideoKRequiredPerfectQualityOrders;
            default:
                return false;
        }
    }

    private GameObject GetManagerPrefab(ManagerId managerId)
    {
        if (managerPrefabs == null)
        {
            return null;
        }

        foreach (GameObject prefab in managerPrefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            ManagerSimulation manager = prefab.GetComponent<ManagerSimulation>();
            if (manager != null && manager.managerId == managerId)
            {
                return prefab;
            }
        }

        Debug.LogWarning("Missing manager prefab for " + managerId);
        return null;
    }

    private void HandleOrderSubmittedDetailed(
        DeveloperSimulation developer,
        ActiveOrder order,
        float quality,
        bool overDeadline)
    {
        if (order == null || order.manager != ManagerId.HideoK || quality < PerfectTaskQuality)
        {
            return;
        }

        PlayerPrefs.SetInt(HideoCompletionNpcUnlockedKey, 1);
        SelectRandomHideoCompletionNpcAppearance();
        PlayerPrefs.Save();
        PlayHideoCompletionFirework();
        TrySpawnHideoCompletionNpc();

        if (_hideoCompletionNpcInstance != null)
        {
            ManagerSimulation hideo = FindHideoKManager();
            if (hideo != null)
            {
                hideo.gameObject.SetActive(false);
            }
        }
    }

    private void PlayHideoCompletionFirework()
    {
        if (_hideoCompletionFireworkPrefab == null)
        {
            return;
        }

        Camera camera = Camera.main;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        if (camera != null)
        {
            spawnPosition = camera.ViewportToWorldPoint(new Vector3(
                _hideoCompletionFireworkScreenPosition.x,
                _hideoCompletionFireworkScreenPosition.y,
                Mathf.Max(0.01f, _hideoCompletionFireworkCameraDistance)));
            spawnPosition += _hideoCompletionFireworkPositionOffset;
            spawnRotation = camera.transform.rotation;
        }
        else
        {
            spawnPosition = _hideoCompletionFireworkPositionOffset;
            spawnRotation = Quaternion.identity;
        }

        ParticleSystem firework = Instantiate(
            _hideoCompletionFireworkPrefab,
            spawnPosition,
            spawnRotation);
        firework.transform.localScale = _hideoCompletionFireworkScale;
        firework.gameObject.SetActive(true);
        firework.Play(true);
        Destroy(
            firework.gameObject,
            Mathf.Max(0.1f, _hideoCompletionFireworkLifetime));
    }

    private void TrySpawnHideoCompletionNpc()
    {
        if (_hideoCompletionNpcInstance != null
            || PlayerPrefs.GetInt(HideoCompletionNpcUnlockedKey, 0) == 0)
        {
            return;
        }

        HideoCompletionNpcVariant selectedVariant = GetSelectedHideoCompletionNpcVariant();
        GameObject selectedPrefab = selectedVariant != null ? selectedVariant.prefab : null;
        if (selectedPrefab == null)
        {
            Debug.LogWarning(
                "Hideo completion NPC was not created. Assign a complete prefab to every NPC Variant.");
            return;
        }

        ManagerSimulation hideo = FindHideoKManager();
        Vector3 spawnPosition;
        if (hideo != null)
        {
            spawnPosition = hideo.transform.position + _hideoCompletionNpcOffset;
            SaveHideoCompletionNpcPosition(spawnPosition);
        }
        else
        {
            spawnPosition = GetSavedHideoCompletionNpcPosition();
        }

        _hideoCompletionNpcInstance = Instantiate(
            selectedPrefab,
            spawnPosition,
            selectedPrefab.transform.rotation,
            _hideoCompletionNpcParent);

        if (hideo != null)
        {
            hideo.gameObject.SetActive(false);
        }

        SceneChatNpc chatNpc = _hideoCompletionNpcInstance.GetComponent<SceneChatNpc>();
        if (chatNpc == null)
        {
            chatNpc = _hideoCompletionNpcInstance.AddComponent<SceneChatNpc>();
        }

        chatNpc.Configure(
            GetVariantName(selectedVariant),
            GetVariantDescription(selectedVariant),
            GetVariantSceneName(selectedVariant));
    }

    private void SaveHideoCompletionNpcPosition(Vector3 position)
    {
        PlayerPrefs.SetFloat(HideoCompletionNpcPositionXKey, position.x);
        PlayerPrefs.SetFloat(HideoCompletionNpcPositionYKey, position.y);
        PlayerPrefs.SetFloat(HideoCompletionNpcPositionZKey, position.z);
        PlayerPrefs.Save();
    }

    private Vector3 GetSavedHideoCompletionNpcPosition()
    {
        if (!PlayerPrefs.HasKey(HideoCompletionNpcPositionXKey))
        {
            return _hideoCompletionNpcFallbackPosition;
        }

        return new Vector3(
            PlayerPrefs.GetFloat(HideoCompletionNpcPositionXKey, _hideoCompletionNpcFallbackPosition.x),
            PlayerPrefs.GetFloat(HideoCompletionNpcPositionYKey, _hideoCompletionNpcFallbackPosition.y),
            PlayerPrefs.GetFloat(HideoCompletionNpcPositionZKey, _hideoCompletionNpcFallbackPosition.z));
    }

    public void SetHideoCompletionNpcPosition(Vector3 offsetFromHideo)
    {
        _hideoCompletionNpcOffset = offsetFromHideo;
        if (_hideoCompletionNpcInstance == null)
        {
            return;
        }

        ManagerSimulation hideo = FindHideoKManager();
        if (hideo != null)
        {
            _hideoCompletionNpcInstance.transform.position =
                hideo.transform.position + _hideoCompletionNpcOffset;
        }
    }

    public void SetHideoCompletionNpcDescriptionFontSize(float fontSize)
    {
        _hideoCompletionNpcDescriptionFontSize = Mathf.Max(0.1f, fontSize);
        if (_hideoCompletionNpcInstance == null)
        {
            return;
        }

        SceneChatNpc chatNpc = _hideoCompletionNpcInstance.GetComponent<SceneChatNpc>();
        if (chatNpc != null)
        {
            chatNpc.SetDescriptionFontSize(_hideoCompletionNpcDescriptionFontSize);
        }
    }

    public void SetHideoCompletionNpcDescriptionPosition(Vector3 localPosition)
    {
        _hideoCompletionNpcDescriptionPosition = localPosition;
        if (_hideoCompletionNpcInstance == null)
        {
            return;
        }

        SceneChatNpc chatNpc = _hideoCompletionNpcInstance.GetComponent<SceneChatNpc>();
        if (chatNpc != null)
        {
            chatNpc.SetDescriptionPosition(localPosition);
        }
    }

    public void SetHideoCompletionNpcScene(string sceneName)
    {
        _hideoCompletionNpcSceneName = sceneName;

        if (_hideoCompletionNpcInstance == null)
        {
            return;
        }

        SceneChatNpc chatNpc = _hideoCompletionNpcInstance.GetComponent<SceneChatNpc>();
        if (chatNpc != null)
        {
            chatNpc.SetDestinationScene(sceneName);
        }
    }

    private void SelectRandomHideoCompletionNpcAppearance()
    {
        int variantCount = _hideoCompletionNpcVariants != null
            ? _hideoCompletionNpcVariants.Length
            : 0;

        if (PlayerPrefs.HasKey(HideoCompletionNpcAppearanceKey)
            || variantCount == 0)
        {
            return;
        }

        int selectedIndex = UnityEngine.Random.Range(0, variantCount);
        PlayerPrefs.SetInt(HideoCompletionNpcAppearanceKey, selectedIndex);
    }

    private HideoCompletionNpcVariant GetSelectedHideoCompletionNpcVariant()
    {
        if (_hideoCompletionNpcVariants == null || _hideoCompletionNpcVariants.Length == 0)
        {
            return null;
        }

        SelectRandomHideoCompletionNpcAppearance();
        int selectedIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(HideoCompletionNpcAppearanceKey, 0),
            0,
            _hideoCompletionNpcVariants.Length - 1);
        return _hideoCompletionNpcVariants[selectedIndex];
    }

    private string GetVariantName(HideoCompletionNpcVariant variant)
    {
        return variant != null && !string.IsNullOrWhiteSpace(variant.npcName)
            ? variant.npcName
            : _hideoCompletionNpcName;
    }

    private string GetVariantDescription(HideoCompletionNpcVariant variant)
    {
        return variant != null && !string.IsNullOrWhiteSpace(variant.description)
            ? variant.description
            : _hideoCompletionNpcDescription;
    }

    private string GetVariantSceneName(HideoCompletionNpcVariant variant)
    {
        return variant != null && !string.IsNullOrWhiteSpace(variant.destinationSceneName)
            ? variant.destinationSceneName
            : _hideoCompletionNpcSceneName;
    }

    private ManagerSimulation FindHideoKManager()
    {
        ManagerSimulation[] managers = FindObjectsByType<ManagerSimulation>(FindObjectsSortMode.None);
        foreach (ManagerSimulation manager in managers)
        {
            if (manager != null && manager.managerId == ManagerId.HideoK)
            {
                return manager;
            }
        }

        return null;
    }

    public void PayRent()
    {
        if (!_rentPaymentDue)
        {
            return;
        }

        if (developerSimulation.devInfo.money < _currentRent)
        {
            OnNotEnoghMoney();
            return;
        }

        developerSimulation.RemoveMoney(_currentRent);
        CloseRentPopup();
    }

    public void RestartFromRent()
    {
        NewGame();
    }

    private void CloseRentPopup()
    {
        _rentPaymentDue = false;
        _currentRent = 0;
        if (_rentPopup != null)
        {
            _rentPopup.gameObject.SetActive(false);
        }
        GameState = GameState.Running;
        SaveGame();
    }

    void SpeedControl()
    {
        if (Input.GetKeyDown(KeyCode.F10))
        {
            gameSpeed++;
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            gameSpeed--;
        }
        gameSpeed = Mathf.Clamp(gameSpeed, 1, 10);
        Time.timeScale = gameSpeed;
    }
    private void OnDestroy()
    {
        if (GameState == GameState.NewGame) return;

        SaveGame();
    }

    private void EnsureRandomEventController()
    {
        if (GetComponent<RandomEventController>() != null)
        {
            return;
        }

        if (FindFirstObjectByType<RandomEventController>() == null)
        {
            gameObject.AddComponent<RandomEventController>();
        }
    }

    private void SaveGame()
    {
        _deskLevel = Mathf.Max(_deskLevel, DeskUpdated ? 1 : 0);
        _coffeeLevel = Mathf.Max(_coffeeLevel, CoffeeUpdated ? 1 : 0);
        _whiteBoardLevel = Mathf.Max(_whiteBoardLevel, WhiteBoardUpdated ? 1 : 0);
        _colleagueLevel = Mathf.Max(_colleagueLevel, ColleagueUpdated ? 1 : 0);
        PlayerPrefs.SetInt("DeskLevel", _deskLevel);
        PlayerPrefs.SetInt("CoffeeLevel", _coffeeLevel);
        PlayerPrefs.SetInt("WhiteBoardLevel", _whiteBoardLevel);
        PlayerPrefs.SetInt("ColleagueLevel", _colleagueLevel);
        PlayerPrefs.SetFloat("DeskNextLevelRemaining", _deskNextLevelRemaining);
        PlayerPrefs.SetFloat("CoffeeNextLevelRemaining", _coffeeNextLevelRemaining);
        PlayerPrefs.SetFloat("WhiteBoardNextLevelRemaining", _whiteBoardNextLevelRemaining);
        PlayerPrefs.SetFloat("ColleagueNextLevelRemaining", _colleagueNextLevelRemaining);
        PlayerPrefs.SetInt("Money", (int)developerSimulation.devInfo.money);
        PlayerPrefs.SetInt("Level", developerSimulation.devInfo.level);
        PlayerPrefs.SetInt("Experience", (int)developerSimulation.devInfo.experience);
        PlayerPrefs.SetInt("TaskCompleted", developerSimulation.devInfo.taskCompleted);
        PlayerPrefs.SetInt("PerfectQualityOrdersCompleted", developerSimulation.devInfo.perfectQualityOrdersCompleted);
        PlayerPrefs.SetInt("HighQualityOrdersCompleted", developerSimulation.devInfo.highQualityOrdersCompleted);
        PlayerPrefs.SetFloat("RentDayElapsed", _rentDayElapsed);
        PlayerPrefs.SetInt("RentPaymentDue", _rentPaymentDue ? 1 : 0);
        PlayerPrefs.SetInt("CurrentRent", _currentRent);
        PlayerPrefs.Save();
    }
    public void PauseGame()
    {
        _pausePopup.gameObject.SetActive(true);
        GameState = GameState.Stopped;
    }
    public void ResumeGame()
    {

        _pausePopup.gameObject.SetActive(false);
        GameState = GameState.Running;
    }
    public void NewGame()
    {
        GameState = GameState.NewGame;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void Quit()
    {
        SaveGame();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

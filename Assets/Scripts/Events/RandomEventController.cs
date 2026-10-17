using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RandomEventController : MonoBehaviour
{
    [Header("Triggers")]
    [SerializeField] [Range(0f, 1f)] private float _triggerChanceAfterOrder = 0.12f;
    [SerializeField] private RandomEventDuration _computerOverheatingDuration = RandomEventDuration.NextProgrammingStage;
    [SerializeField] private RandomEventDuration _monitorBrokenDuration = RandomEventDuration.NextProgrammingStage;
    [SerializeField] [Range(0f, 1f)] private float _stageEventChanceDuringStage = 0.15f;
    [SerializeField] private float _stageEventMinDelay = 2f;
    [SerializeField] private float _stageEventMaxDelay = 8f;

    [Header("UI")]
    [SerializeField] private GameObject _eventPopup;
    [SerializeField] private TMP_Text _eventTitleText;
    [SerializeField] private TMP_Text _eventBodyText;
    [Tooltip("Optional: assign an existing Continue button from the scene/prefab (lets you control shape/position).")]
    [SerializeField] private Button _eventPopupContinueButton;
    [Tooltip("Optional: if assigned, the popup will be parented under this transform instead of searching for a Canvas.")]
    [SerializeField] private Transform _eventPopupParentOverride;
    [Header("Popup Button Layout (runtime-created or optionally overridden)")]
    [SerializeField] private bool _applyContinueButtonLayout = true;
    [SerializeField] private Vector2 _continueAnchorMin = new Vector2(1f, 0f);
    [SerializeField] private Vector2 _continueAnchorMax = new Vector2(1f, 0f);
    [SerializeField] private Vector2 _continuePivot = new Vector2(1f, 0f);
    [SerializeField] private Vector2 _continueAnchoredPosition = new Vector2(-16f, 12f);
    [SerializeField] private Vector2 _continueSizeDelta = new Vector2(140f, 36f);

    [Header("Colleague Joke")]
    [Tooltip("If enabled, triggers the colleague joke popup when you receive a project/order from the client.")]
    [SerializeField] private bool _triggerColleagueJokeOnOrderReceived = true;
    [SerializeField] private float _popupDisplaySeconds = 4f;
    [SerializeField] private Image _redFlashOverlay;
    [SerializeField] private Image _coffeeMissingIcon;
    [SerializeField] private Image _trafficJamIcon;
    [SerializeField] private float _coffeeFlashSeconds = 0.5f;
    [SerializeField] private Image _phoneIcon;
    [SerializeField] private Sprite _phoneSprite;
    [SerializeField] private string _phoneImageObjectName = "phoneImage";
    [SerializeField] private float _phoneIconDisplaySeconds = 1f;
    [SerializeField] private Sprite _chairSprite;
    [SerializeField] private string _chairSourceObjectName = "manager_desk";
    [SerializeField] private Sprite _cupSprite;
    [SerializeField] private Sprite _cupShardSprite;
    [SerializeField] private Vector2 _cupBodySize = new Vector2(84f, 96f);
    [SerializeField] private AudioClip _cupSmashSound;
    [SerializeField] [Range(0f, 1f)] private float _cupSmashSoundVolume = 0.7f;

    [Header("Event Sounds")]
    [SerializeField] private AudioClip _momCallingEventSound;
    [SerializeField] [Range(0f, 1f)] private float _momCallingEventSoundVolume = 0.8f;
    [SerializeField] private AudioClip _coffeeMissingEventSound;
    [SerializeField] [Range(0f, 1f)] private float _coffeeMissingEventSoundVolume = 0.8f;
    [SerializeField] private AudioClip _trafficJamEventSound;
    [SerializeField] [Range(0f, 1f)] private float _trafficJamEventSoundVolume = 0.8f;

    [Header("Background Sound")]
    [SerializeField] private AudioClip _backgroundSound;
    [SerializeField] private AudioSource _backgroundAudioSource;
    [SerializeField] [Range(0f, 1f)] private float _backgroundSoundVolume = 0.5f;

    [Header("Boss Looking")]
    [SerializeField] private BossLookingSettings _bossLookingSettings = new BossLookingSettings();

    [Header("Traffic Jam")]
    [SerializeField] [Range(0f, 1f)] private float _trafficJamChanceOnOrder = 0.1f;
    [SerializeField] private Sprite _trafficJamIconSprite;
    [SerializeField] private Vector2 _trafficJamIconSize = new Vector2(88f, 88f);
    [SerializeField] private Vector2 _trafficJamIconAnchoredPosition = new Vector2(-48f, -48f);
    [SerializeField] private float _trafficJamTimePenaltySeconds = 10f;

    [Header("Technical Update")]
    [SerializeField] private Sprite _technicalUpdateIconSprite;
    [SerializeField] private Image _technicalUpdateIcon;
    [SerializeField] private Vector2 _technicalUpdateIconSize = new Vector2(88f, 88f);
    [SerializeField] private Vector2 _technicalUpdateIconAnchoredPosition = new Vector2(-48f, -140f);

    [Header("Neighbor Hammer Drill")]
    [SerializeField] [Range(0f, 1f)] private float _hammerDrillChanceOnOrder = 0.12f;
    [SerializeField] private AudioClip _hammerDrillSound;
    [SerializeField] [Range(0f, 1f)] private float _hammerDrillSoundVolume = 0.6f;

    [Header("Flies Invasion")]
    [SerializeField] [Range(0f, 1f)] private float _fliesInvasionChanceOnOrder = 0.1f;
    [SerializeField] private FliesInvasionSettings _fliesInvasionSettings = new FliesInvasionSettings();
    [SerializeField] private AudioClip _fliesBuzzSound;
    [SerializeField] [Range(0f, 1f)] private float _fliesBuzzVolume = 0.35f;

    [Header("Visuals")]
    [SerializeField] private Transform _smokeAnchor;
    [SerializeField] private string _programmingPcObjectName = "Programming_PC";
    [Tooltip("Optional: particle already placed in the scene (e.g. under Programming_PC).")]
    [SerializeField] private ParticleSystem _smokeParticles;
    [Tooltip("Optional: drag a particle prefab here. Spawned at Smoke Anchor when the event starts.")]
    [SerializeField] private ParticleSystem _smokeParticlePrefab;
    [SerializeField] private Vector3 _smokeLocalOffset = new Vector3(0f, 0.8f, 0f);

    private ParticleSystem _runtimeSmokeInstance;
    private bool _createdRuntimeSmoke;
    private Coroutine _coffeeFlashRoutine;
    private Coroutine _stageEventRoutine;
    private readonly Queue<QueuedEventPopup> _eventPopupQueue = new Queue<QueuedEventPopup>();
    private bool _eventPopupVisible;
    private Button _popupContinueButton;
    private Image _runtimeRedFlashOverlay;
    private Image _runtimeCoffeeMissingIcon;
    private Image _runtimeTrafficJamIcon;
    private Image _runtimeTechnicalUpdateIcon;
    private Image _runtimePhoneIcon;
    private NeighborHammerDrillVignette _hammerDrillVignette;
    private AudioSource _audioSource;
    private AudioSource _momCallingEventSource;
    private AudioSource _coffeeMissingEventSource;
    private AudioSource _trafficJamEventSource;
    private AudioSource _hammerDrillEventSource;
    private bool _backgroundPausedByEvent;
    private bool _hammerDrillPendingForNextOrder;
    private bool _fliesInvasionPendingForNextOrder;
    private bool _bossLookingPendingForNextOrder;
    private bool _trafficJamPendingForNextOrder;
    private bool _technicalUpdatePendingForNextStage;
    private MonitorBrokenFlicker _monitorBrokenFlicker;
    private FliesInvasionVisual _officeFliesVisual;
    private FliesInvasionVisual _minigameFliesVisual;
    private AudioSource _fliesBuzzSource;
    private BossLookingVisual _bossLookingVisual;
    private BossLookingResultOverlay _bossLookingResultOverlay;
    private Coroutine _bossLookingFinishRoutine;
    private Coroutine _officeColleagueJokeRoutine;

    private struct QueuedEventPopup
    {
        public string Title;
        public string Body;
        public string Effect;
    }

    private void OnEnable()
    {
        DeveloperSimulation.OrderSubmitted += HandleOrderSubmitted;
        DeveloperSimulation.OrderReceived += HandleOrderReceived;
        DeveloperSimulation.OrderReadyToSubmit += HandleOrderReadyToSubmit;
        MissionObjects.MissionStarted += HandleMissionStarted;
        MissionObjects.MissionEnded += HandleMissionEnded;
    }

    private void OnDisable()
    {
        DeveloperSimulation.OrderSubmitted -= HandleOrderSubmitted;
        DeveloperSimulation.OrderReceived -= HandleOrderReceived;
        DeveloperSimulation.OrderReadyToSubmit -= HandleOrderReadyToSubmit;
        MissionObjects.MissionStarted -= HandleMissionStarted;
        MissionObjects.MissionEnded -= HandleMissionEnded;
        StopLoopingEventSound(_momCallingEventSource);
        StopLoopingEventSound(_coffeeMissingEventSource);
        StopLoopingEventSound(_trafficJamEventSource);
        StopLoopingEventSound(_hammerDrillEventSource);
        StopFliesBuzz();
        if (_backgroundAudioSource != null)
        {
            _backgroundAudioSource.Stop();
        }
    }

    private void Start()
    {
        if (_eventPopup != null)
        {
            _eventPopup.SetActive(false);
        }

        if (_smokeAnchor == null && !string.IsNullOrEmpty(_programmingPcObjectName))
        {
            GameObject programmingPc = GameObject.Find(_programmingPcObjectName);
            if (programmingPc != null)
            {
                _smokeAnchor = programmingPc.transform;
            }
        }

        StopSmokeVisual();
        EnsureAudioSource();
        StartBackgroundSound();
    }

    private void EnsureAudioSource()
    {
        if (_audioSource != null)
        {
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    private void HandleOrderReceived(DeveloperSimulation developer, ActiveOrder order)
    {
        if (order == null || order.isTutorialOrder || GameController.GameState == GameState.Stopped)
        {
            return;
        }

        if (IsAnyEventRunning())
        {
            return;
        }

        if (_hammerDrillPendingForNextOrder)
        {
            _hammerDrillPendingForNextOrder = false;
            TriggerNeighborHammerDrill();
            return;
        }
        else if (_fliesInvasionPendingForNextOrder)
        {
            _fliesInvasionPendingForNextOrder = false;
            TriggerFliesInvasion();
            return;
        }
        else if (_bossLookingPendingForNextOrder)
        {
            _bossLookingPendingForNextOrder = false;
            TriggerBossLookingForOrder(developer, order);
            return;
        }
        else if (_trafficJamPendingForNextOrder)
        {
            _trafficJamPendingForNextOrder = false;
            TriggerTrafficJam();
            return;
        }

        if (_triggerColleagueJokeOnOrderReceived)
        {
            TriggerColleagueJokeOnOrderReceived();
            if (IsAnyEventRunning())
            {
                return;
            }
        }

        if (Random.value <= _hammerDrillChanceOnOrder)
        {
            TriggerNeighborHammerDrill();
            return;
        }

        if (Random.value <= _fliesInvasionChanceOnOrder)
        {
            TriggerFliesInvasion();
            return;
        }

        if (Random.value <= _trafficJamChanceOnOrder)
        {
            TriggerTrafficJam();
        }
    }

    private void HandleOrderSubmitted(DeveloperSimulation developer, ActiveOrder order)
    {
        RandomEventSystem.OnOrderSubmitted();
        RandomEventSystem.OnCurrentOrderSubmitted();
        RefreshVisuals();

        if (BossLookingSystem.IsActive && _bossLookingFinishRoutine == null)
        {
            HideBossLookingVisual();
            BossLookingSystem.Clear();
        }

        if (GameController.GameState == GameState.Stopped)
        {
            return;
        }

        if (Random.value > _triggerChanceAfterOrder)
        {
            return;
        }

        TriggerRandomEvent();
    }

    private void HandleOrderReadyToSubmit(DeveloperSimulation developer, ActiveOrder order)
    {
        if (!BossLookingSystem.IsActive || order == null || order.isTutorialOrder)
        {
            return;
        }

        if (_bossLookingFinishRoutine != null)
        {
            StopCoroutine(_bossLookingFinishRoutine);
        }

        float averageQuality = GetOrderAverageQuality(order);
        _bossLookingFinishRoutine = StartCoroutine(FinishBossLookingVisit(developer, order, averageQuality));
    }

    private void TriggerRandomEvent()
    {
        if (IsAnyEventRunning() || HasPendingEvent())
        {
            return;
        }

        float roll = Random.value;
        if (roll < 0.143f)
        {
            TriggerComputerOverheating();
            return;
        }

        if (roll < 0.286f)
        {
            TriggerCoffeeMissing();
            return;
        }

        if (roll < 0.429f)
        {
            TriggerNeighborHammerDrillForNextOrder();
            return;
        }

        if (roll < 0.572f)
        {
            TriggerMonitorBroken();
            return;
        }

        if (roll < 0.715f)
        {
            TriggerFliesInvasionForNextOrder();
            return;
        }

        if (roll < 0.8f)
        {
            TriggerBossLookingForNextOrder();
            return;
        }

        if (roll < 0.9f)
        {
            TriggerTrafficJamForNextOrder();
            return;
        }

        TriggerTechnicalUpdateForNextStage();
    }

    public void TriggerNeighborHammerDrill()
    {
        if (IsAnyEventRunning())
        {
            return;
        }

        NeighborHammerDrillSystem.Activate();
        RandomEventSystem.Activate(
            RandomEventType.NeighborHammerDrill,
            RandomEventDuration.CurrentOrder);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.NeighborHammerDrill),
            RandomEventSystem.GetEventDescription(RandomEventType.NeighborHammerDrill),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.NeighborHammerDrill, RandomEventDuration.CurrentOrder));
        PlayHammerDrillSound();
        RefreshVisuals();
    }

    public void TriggerNeighborHammerDrillForNextOrder()
    {
        if (!IsAnyEventRunning() && !HasPendingEvent())
        {
            _hammerDrillPendingForNextOrder = true;
        }
    }

    public void TriggerFliesInvasion()
    {
        if (IsAnyEventRunning())
        {
            return;
        }

        FliesInvasionSystem.Activate();
        RandomEventSystem.Activate(
            RandomEventType.FliesInvasion,
            RandomEventDuration.CurrentOrder);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.FliesInvasion),
            RandomEventSystem.GetEventDescription(RandomEventType.FliesInvasion),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.FliesInvasion, RandomEventDuration.CurrentOrder));
        StartFliesBuzz();
        RefreshVisuals();
    }

    public void TriggerFliesInvasionForNextOrder()
    {
        if (!IsAnyEventRunning() && !HasPendingEvent())
        {
            _fliesInvasionPendingForNextOrder = true;
        }
    }

    public void TriggerTrafficJam()
    {
        if (IsAnyEventRunning())
        {
            return;
        }

        TrafficJamSystem.Configure(_trafficJamTimePenaltySeconds);
        TrafficJamSystem.Activate();
        RandomEventSystem.Activate(RandomEventType.TrafficJam, RandomEventDuration.CurrentOrder);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.TrafficJam),
            RandomEventSystem.GetEventDescription(RandomEventType.TrafficJam),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.TrafficJam, RandomEventDuration.CurrentOrder));
        StartLoopingEventSound(
            ref _trafficJamEventSource,
            _trafficJamEventSound,
            _trafficJamEventSoundVolume,
            "TrafficJamEventAudio");
        RefreshVisuals();
    }

    public void TriggerTrafficJamForNextOrder()
    {
        if (!IsAnyEventRunning() && !HasPendingEvent())
        {
            _trafficJamPendingForNextOrder = true;
        }
    }

    [ContextMenu("Test Traffic Jam Event")]
    public void TestTrafficJamEvent()
    {
        _trafficJamPendingForNextOrder = false;
        TriggerTrafficJam();
    }

    public void TriggerTechnicalUpdate()
    {
        if (IsAnyEventRunning())
        {
            return;
        }

        TechnicalUpdateSystem.Activate();
        RandomEventSystem.Activate(
            RandomEventType.TechnicalUpdate,
            RandomEventDuration.NextProgrammingStage);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.TechnicalUpdate),
            RandomEventSystem.GetEventDescription(RandomEventType.TechnicalUpdate),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.TechnicalUpdate, RandomEventDuration.NextProgrammingStage));
        RefreshVisuals();
    }

    public void TriggerTechnicalUpdateForNextStage()
    {
        if (!IsAnyEventRunning() && !HasPendingEvent())
        {
            _technicalUpdatePendingForNextStage = true;
        }
    }

    [ContextMenu("Test Technical Update Event")]
    public void TestTechnicalUpdateEvent()
    {
        _technicalUpdatePendingForNextStage = false;
        TriggerTechnicalUpdate();
    }

    [ContextMenu("Test Colleague Joke Event")]
    public void TestColleagueJokeEvent()
    {
        MissionObjects mission = FindFirstObjectByType<MissionObjects>();
        if (mission == null || !mission.isStarted || mission.ActiveMiniGame == null)
        {
            Debug.LogWarning("Start a minigame stage before testing Colleague's Joke.");
            return;
        }

        StageEventSystem.ResetForStage();
        StartCoroutine(PlayColleagueJoke(mission, mission.ActiveMiniGame));
    }

    [ContextMenu("Test Flies Invasion Event")]
    public void TestFliesInvasionEvent()
    {
        FliesInvasionSystem.Clear();
        _fliesInvasionPendingForNextOrder = false;
        TriggerFliesInvasion();

        MissionObjects mission = FindFirstObjectByType<MissionObjects>();
        if (mission != null && mission.isStarted && mission.ActiveMiniGame != null)
        {
            ShowMinigameFlies(mission.ActiveMiniGame.transform);
        }
    }

    [ContextMenu("Test Neighbor Hammer Drill Event")]
    public void TestNeighborHammerDrillEvent()
    {
        NeighborHammerDrillSystem.Clear();
        _hammerDrillPendingForNextOrder = false;
        TriggerNeighborHammerDrill();
    }

    private void PlayHammerDrillSound()
    {
        StartLoopingEventSound(
            ref _hammerDrillEventSource,
            _hammerDrillSound,
            _hammerDrillSoundVolume,
            "HammerDrillEventAudio");
    }

    private void StartLoopingEventSound(
        ref AudioSource source,
        AudioClip eventSound,
        float volume,
        string objectName)
    {
        if (eventSound == null)
        {
            return;
        }

        if (source == null)
        {
            GameObject audioObject = new GameObject(objectName);
            audioObject.transform.SetParent(transform, false);
            source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
        }

        source.clip = eventSound;
        source.volume = Mathf.Clamp01(volume);
        if (!source.isPlaying)
        {
            source.Play();
        }
        RefreshBackgroundSoundState();
    }

    private void StopLoopingEventSound(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
        RefreshBackgroundSoundState();
    }

    private void StartBackgroundSound()
    {
        if (_backgroundSound == null)
        {
            return;
        }

        if (_backgroundAudioSource == null)
        {
            GameObject audioObject = new GameObject("BackgroundAudio");
            audioObject.transform.SetParent(transform, false);
            _backgroundAudioSource = audioObject.AddComponent<AudioSource>();
        }

        _backgroundAudioSource.playOnAwake = false;
        _backgroundAudioSource.loop = true;
        _backgroundAudioSource.spatialBlend = 0f;
        _backgroundAudioSource.clip = _backgroundSound;
        _backgroundAudioSource.volume = _backgroundSoundVolume;
        _backgroundAudioSource.Play();
        _backgroundPausedByEvent = false;
        RefreshBackgroundSoundState();
    }

    private void RefreshBackgroundSoundState()
    {
        if (_backgroundAudioSource == null || _backgroundSound == null)
        {
            return;
        }

        bool eventSoundPlaying = IsPlaying(_momCallingEventSource)
            || IsPlaying(_coffeeMissingEventSource)
            || IsPlaying(_trafficJamEventSource)
            || IsPlaying(_hammerDrillEventSource)
            || IsPlaying(_fliesBuzzSource);

        if (eventSoundPlaying)
        {
            if (!_backgroundPausedByEvent)
            {
                _backgroundAudioSource.Pause();
                _backgroundPausedByEvent = true;
            }
            return;
        }

        if (_backgroundPausedByEvent)
        {
            _backgroundAudioSource.UnPause();
            _backgroundPausedByEvent = false;
        }
    }

    private static bool IsPlaying(AudioSource source)
    {
        return source != null && source.isPlaying;
    }

    private void HandleMissionEnded(MissionObjects mission, float score)
    {
        StopLoopingEventSound(_momCallingEventSource);
        if (_stageEventRoutine != null)
        {
            StopCoroutine(_stageEventRoutine);
            _stageEventRoutine = null;
        }

        StageEventSystem.SetBlocking(false);
        HidePhoneIcon();
        HideMinigameFlies();
        TechnicalUpdateSystem.Clear();
        RefreshVisuals();

        if (mission == null || mission.milestone == null)
        {
            return;
        }

        if (mission.milestone.devicetype != "Programming")
        {
            return;
        }

        RandomEventSystem.OnProgrammingStageCompleted();
    }

    private void HandleMissionStarted(MissionObjects mission, GameObject player)
    {
        StageEventSystem.ResetForStage();

        if (_technicalUpdatePendingForNextStage)
        {
            _technicalUpdatePendingForNextStage = false;
            TriggerTechnicalUpdate();
        }

        if (_stageEventRoutine != null)
        {
            StopCoroutine(_stageEventRoutine);
        }

        _stageEventRoutine = StartCoroutine(TryStageEventDuringMission(mission));

        if (FliesInvasionSystem.IsActive)
        {
            StartCoroutine(SpawnMinigameFliesWhenReady(mission));
        }
    }

    private IEnumerator SpawnMinigameFliesWhenReady(MissionObjects mission)
    {
        yield return null;

        if (mission == null || !mission.isStarted || !FliesInvasionSystem.IsActive)
        {
            yield break;
        }

        GameObject miniGame = mission.ActiveMiniGame;
        if (miniGame == null)
        {
            yield break;
        }

        ShowMinigameFlies(miniGame.transform);
    }

    public void TriggerComputerOverheating()
    {
        ActivateEvent(
            RandomEventType.ComputerOverheating,
            _computerOverheatingDuration);
    }

    public void TriggerMonitorBroken()
    {
        ActivateEvent(
            RandomEventType.MonitorBroken,
            _monitorBrokenDuration);
    }

    [ContextMenu("Test Monitor Broken Event")]
    public void TestMonitorBrokenEvent()
    {
        TriggerMonitorBroken();
    }

    public void TriggerCoffeeMissing()
    {
        ActivateEvent(
            RandomEventType.CoffeeMissing,
            RandomEventDuration.NextOrder);
        StartLoopingEventSound(
            ref _coffeeMissingEventSource,
            _coffeeMissingEventSound,
            _coffeeMissingEventSoundVolume,
            "CoffeeMissingEventAudio");
        PlayCoffeeMissingFlash();
    }

    [ContextMenu("Test Coffee Missing Event")]
    public void TestCoffeeMissingEvent()
    {
        TriggerCoffeeMissing();
    }

    [ContextMenu("Test Sudden Call Event")]
    public void TestSuddenCallEvent()
    {
        MissionObjects mission = FindFirstObjectByType<MissionObjects>();
        if (mission == null || !mission.isStarted || mission.ActiveMiniGame == null)
        {
            Debug.LogWarning("Start a minigame stage before testing Sudden Call.");
            return;
        }

        StageEventSystem.ResetForStage();
        StartCoroutine(PlaySuddenCall(mission, mission.ActiveMiniGame));
    }

    [ContextMenu("Test Chair Fell Event")]
    public void TestChairFellEvent()
    {
        MissionObjects mission = FindFirstObjectByType<MissionObjects>();
        if (mission == null || !mission.isStarted || mission.ActiveMiniGame == null)
        {
            Debug.LogWarning("Start a minigame stage before testing Chair Fell.");
            return;
        }

        StageEventSystem.ResetForStage();
        StartCoroutine(PlayChairFell(mission, mission.ActiveMiniGame));
    }

    [ContextMenu("Test Cat Dropped Cup Event")]
    public void TestCatDroppedCupEvent()
    {
        MissionObjects mission = FindFirstObjectByType<MissionObjects>();
        if (mission == null || !mission.isStarted || mission.ActiveMiniGame == null)
        {
            Debug.LogWarning("Start a minigame stage before testing Cat Dropped Cup.");
            return;
        }

        StageEventSystem.ResetForStage();
        StartCoroutine(PlayCatDroppedCup(mission, mission.ActiveMiniGame));
    }

    [ContextMenu("Test Boss Looking Event")]
    public void TestBossLookingEvent()
    {
        _bossLookingPendingForNextOrder = false;
        BossLookingSystem.Clear();
        HideBossLookingVisual();

        DeveloperSimulation developer = FindFirstObjectByType<DeveloperSimulation>();
        if (developer == null || !developer.HasActiveOrder())
        {
            Debug.LogWarning("Accept an order before testing Boss Looking.");
            return;
        }

        TriggerBossLookingForOrder(developer, developer.currentTask.order);
    }

    public void TriggerSuddenCall(MissionObjects mission, GameObject miniGame)
    {
        if (mission == null || miniGame == null || !StageEventSystem.CanTriggerThisStage)
        {
            return;
        }

        StartCoroutine(PlaySuddenCall(mission, miniGame));
    }

    public void TriggerChairFell(MissionObjects mission, GameObject miniGame)
    {
        if (mission == null || miniGame == null || !StageEventSystem.CanTriggerThisStage)
        {
            return;
        }

        StartCoroutine(PlayChairFell(mission, miniGame));
    }

    private void ActivateEvent(RandomEventType eventType, RandomEventDuration duration)
    {
        if (IsAnyEventRunning())
        {
            return;
        }

        RandomEventSystem.Activate(eventType, duration);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(eventType),
            RandomEventSystem.GetEventDescription(eventType),
            RandomEventSystem.GetEventEffectSummary(eventType, duration));
        RefreshVisuals();
    }

    private bool IsAnyEventRunning()
    {
        return RandomEventSystem.HasActiveEvent
            || NeighborHammerDrillSystem.IsActive
            || FliesInvasionSystem.IsActive
            || BossLookingSystem.IsActive
            || TrafficJamSystem.IsActive
            || TechnicalUpdateSystem.IsActive
            || StageEventSystem.IsBlocking
            || _officeColleagueJokeRoutine != null;
    }

    private bool HasPendingEvent()
    {
        return _hammerDrillPendingForNextOrder
            || _fliesInvasionPendingForNextOrder
            || _bossLookingPendingForNextOrder
            || _trafficJamPendingForNextOrder
            || _technicalUpdatePendingForNextStage;
    }

    private void RefreshVisuals()
    {
        StopSmokeVisual();
        HideCoffeeMissingIcon();
        HideTrafficJamIcon();
        HideTechnicalUpdateIcon();
        HideHammerDrillVignette();
        HideMonitorBrokenFlicker();

        switch (RandomEventSystem.ActiveEvent)
        {
            case RandomEventType.ComputerOverheating:
                PlaySmokeVisual();
                break;
            case RandomEventType.CoffeeMissing:
                ShowCoffeeMissingIcon();
                break;
            case RandomEventType.MonitorBroken:
                ShowMonitorBrokenFlicker();
                break;
        }

        if (RandomEventSystem.ActiveEvent != RandomEventType.CoffeeMissing)
        {
            StopLoopingEventSound(_coffeeMissingEventSource);
        }

        if (TrafficJamSystem.IsActive)
        {
            ShowTrafficJamIcon();
        }
        else
        {
            HideTrafficJamIcon();
            StopLoopingEventSound(_trafficJamEventSource);
        }

        if (TechnicalUpdateSystem.IsActive)
        {
            ShowTechnicalUpdateIcon();
        }
        else
        {
            HideTechnicalUpdateIcon();
        }

        if (NeighborHammerDrillSystem.IsActive)
        {
            ShowHammerDrillVignette();
        }
        else
        {
            StopLoopingEventSound(_hammerDrillEventSource);
        }

        if (FliesInvasionSystem.IsActive)
        {
            ShowOfficeFlies();
        }
        else
        {
            HideOfficeFlies();
            HideMinigameFlies();
            StopFliesBuzz();
        }
    }

    private void ShowHammerDrillVignette()
    {
        if (_hammerDrillVignette != null)
        {
            _hammerDrillVignette.Show();
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        _hammerDrillVignette = NeighborHammerDrillVignette.Create(canvas.transform);
        if (_hammerDrillVignette != null)
        {
            _hammerDrillVignette.Show();
        }
    }

    private void HideHammerDrillVignette()
    {
        if (_hammerDrillVignette == null)
        {
            return;
        }

        _hammerDrillVignette.Hide();
        _hammerDrillVignette = null;
    }

    private void ShowMonitorBrokenFlicker()
    {
        if (_monitorBrokenFlicker != null)
        {
            _monitorBrokenFlicker.Show();
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        _monitorBrokenFlicker = MonitorBrokenFlicker.Create(canvas.transform);
        if (_monitorBrokenFlicker != null)
        {
            _monitorBrokenFlicker.Show();
        }
    }

    private void HideMonitorBrokenFlicker()
    {
        if (_monitorBrokenFlicker == null)
        {
            return;
        }

        _monitorBrokenFlicker.Hide();
        _monitorBrokenFlicker = null;
    }

    private Transform GetFliesParent(Transform minigameRoot)
    {
        if (minigameRoot == null)
        {
            return null;
        }

        Canvas canvas = minigameRoot.GetComponentInChildren<Canvas>(true);
        return canvas != null ? canvas.transform : minigameRoot;
    }

    private void ShowOfficeFlies()
    {
        if (_officeFliesVisual != null)
        {
            _officeFliesVisual.Show();
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        _officeFliesVisual = FliesInvasionVisual.Create(canvas.transform, _fliesInvasionSettings);
        if (_officeFliesVisual != null)
        {
            _officeFliesVisual.Show();
        }
    }

    private void HideOfficeFlies()
    {
        if (_officeFliesVisual == null)
        {
            return;
        }

        _officeFliesVisual.Hide();
        _officeFliesVisual = null;
    }

    private void ShowMinigameFlies(Transform minigameRoot)
    {
        HideMinigameFlies();

        Transform parent = GetFliesParent(minigameRoot);
        if (parent == null)
        {
            return;
        }

        _minigameFliesVisual = FliesInvasionVisual.Create(parent, _fliesInvasionSettings);
        if (_minigameFliesVisual != null)
        {
            _minigameFliesVisual.Show();
        }
    }

    private void HideMinigameFlies()
    {
        if (_minigameFliesVisual == null)
        {
            return;
        }

        _minigameFliesVisual.Hide();
        _minigameFliesVisual = null;
    }

    private void StartFliesBuzz()
    {
        if (_fliesBuzzSound == null)
        {
            return;
        }

        if (_fliesBuzzSource == null)
        {
            GameObject buzzObject = new GameObject("FliesBuzzAudio");
            buzzObject.transform.SetParent(transform, false);
            _fliesBuzzSource = buzzObject.AddComponent<AudioSource>();
            _fliesBuzzSource.playOnAwake = false;
            _fliesBuzzSource.loop = true;
            _fliesBuzzSource.spatialBlend = 0f;
        }

        _fliesBuzzSource.clip = _fliesBuzzSound;
        _fliesBuzzSource.volume = _fliesBuzzVolume;
        if (!_fliesBuzzSource.isPlaying)
        {
            _fliesBuzzSource.Play();
        }
        RefreshBackgroundSoundState();
    }

    private void StopFliesBuzz()
    {
        if (_fliesBuzzSource != null && _fliesBuzzSource.isPlaying)
        {
            _fliesBuzzSource.Stop();
        }
        RefreshBackgroundSoundState();
    }

    private void ShowEventPopup(string title, string body, string effect)
    {
        _eventPopupQueue.Enqueue(new QueuedEventPopup
        {
            Title = title,
            Body = body,
            Effect = effect
        });

        TryShowNextEventPopup();
    }

    private void ShowEventPopup(string title, string body, string effect, Transform parentOverride)
    {
        if (parentOverride != null)
        {
            _eventPopupParentOverride = parentOverride;
        }

        ShowEventPopup(title, body, effect);
    }

    private void TryShowNextEventPopup()
    {
        if (_eventPopupVisible)
        {
            return;
        }

        if (_eventPopupQueue.Count == 0)
        {
            if (_eventPopup != null)
            {
                _eventPopup.SetActive(false);
            }

            return;
        }

        EnsurePopupExists();
        if (_eventPopup == null)
        {
            while (_eventPopupQueue.Count > 0)
            {
                QueuedEventPopup message = _eventPopupQueue.Dequeue();
                Debug.Log(message.Title + ": " + message.Body + " (" + message.Effect + ")");
            }

            return;
        }

        QueuedEventPopup nextMessage = _eventPopupQueue.Dequeue();
        if (_eventTitleText != null)
        {
            _eventTitleText.text = nextMessage.Title;
        }

        if (_eventBodyText != null)
        {
            _eventBodyText.text = nextMessage.Body + "\n\n" + nextMessage.Effect;
        }

        _eventPopupVisible = true;
        _eventPopup.SetActive(true);

        if (_popupContinueButton != null)
        {
            _popupContinueButton.gameObject.SetActive(true);
        }
    }

    private void OnEventPopupContinue()
    {
        _eventPopupVisible = false;

        if (_popupContinueButton != null)
        {
            _popupContinueButton.gameObject.SetActive(false);
        }

        TryShowNextEventPopup();
    }

    private void EnsurePopupExists()
    {
        if (_eventPopupContinueButton != null && _popupContinueButton == null)
        {
            _popupContinueButton = _eventPopupContinueButton;
            _popupContinueButton.onClick.RemoveListener(OnEventPopupContinue);
            _popupContinueButton.onClick.AddListener(OnEventPopupContinue);
        }

        if (_eventPopup != null && _eventBodyText != null && _popupContinueButton != null)
        {
            return;
        }

        Transform popupParent = _eventPopupParentOverride;
        if (popupParent == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            popupParent = canvas.transform;
        }

        if (_eventPopup == null)
        {
            _eventPopup = new GameObject("RandomEventPopup");
            _eventPopup.transform.SetParent(popupParent, false);

            RectTransform panelRect = _eventPopup.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -40f);
            panelRect.sizeDelta = new Vector2(720f, 180f);

            Image background = _eventPopup.AddComponent<Image>();
            background.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

            GameObject titleObject = new GameObject("Title");
            titleObject.transform.SetParent(_eventPopup.transform, false);
            RectTransform titleRect = titleObject.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.45f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(16f, 0f);
            titleRect.offsetMax = new Vector2(-16f, -8f);
            _eventTitleText = titleObject.AddComponent<TextMeshProUGUI>();
            _eventTitleText.fontSize = 28f;
            _eventTitleText.fontStyle = FontStyles.Bold;
            _eventTitleText.alignment = TextAlignmentOptions.Left;

            GameObject bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(_eventPopup.transform, false);
            RectTransform bodyRect = bodyObject.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0.22f);
            bodyRect.anchorMax = new Vector2(1f, 0.45f);
            bodyRect.offsetMin = new Vector2(16f, 0f);
            bodyRect.offsetMax = new Vector2(-16f, 0f);
            _eventBodyText = bodyObject.AddComponent<TextMeshProUGUI>();
            _eventBodyText.fontSize = 20f;
            _eventBodyText.alignment = TextAlignmentOptions.TopLeft;
        }

        if (_popupContinueButton == null)
        {
            // Prefer a scene/prefab button if one was assigned or exists in children.
            if (_eventPopupContinueButton != null)
            {
                _popupContinueButton = _eventPopupContinueButton;
            }
            else
            {
                _popupContinueButton = _eventPopup.GetComponentInChildren<Button>(true);
            }

            if (_popupContinueButton == null)
            {
                GameObject buttonObject = new GameObject("ContinueButton");
                buttonObject.transform.SetParent(_eventPopup.transform, false);
                RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
                buttonRect.anchorMin = _continueAnchorMin;
                buttonRect.anchorMax = _continueAnchorMax;
                buttonRect.pivot = _continuePivot;
                buttonRect.anchoredPosition = _continueAnchoredPosition;
                buttonRect.sizeDelta = _continueSizeDelta;

                Image buttonImage = buttonObject.AddComponent<Image>();
                buttonImage.color = new Color(0.2f, 0.55f, 0.25f, 1f);

                _popupContinueButton = buttonObject.AddComponent<Button>();
                _popupContinueButton.targetGraphic = buttonImage;

                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect = labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
                label.text = "Continue";
                label.fontSize = 20f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.raycastTarget = false;
            }

            if (_popupContinueButton != null)
            {
                _popupContinueButton.onClick.RemoveListener(OnEventPopupContinue);
                _popupContinueButton.onClick.AddListener(OnEventPopupContinue);
                _popupContinueButton.gameObject.SetActive(false);
            }
        }

        ApplyContinueButtonLayoutIfNeeded();
    }

    private void ApplyContinueButtonLayoutIfNeeded()
    {
        if (!_applyContinueButtonLayout || _popupContinueButton == null)
        {
            return;
        }

        RectTransform rect = _popupContinueButton.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = _continueAnchorMin;
        rect.anchorMax = _continueAnchorMax;
        rect.pivot = _continuePivot;
        rect.anchoredPosition = _continueAnchoredPosition;
        rect.sizeDelta = _continueSizeDelta;
    }

    private void TriggerColleagueJokeOnOrderReceived()
    {
        // Avoid stacking multiple "freeze" routines.
        if (IsAnyEventRunning())
        {
            return;
        }

        RandomEventSystem.Activate(
            RandomEventType.ColleagueJoke,
            RandomEventDuration.CurrentOrder);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.ColleagueJoke),
            RandomEventSystem.GetEventDescription(RandomEventType.ColleagueJoke),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.ColleagueJoke, RandomEventDuration.NextProgrammingStage));

        // Small "freeze" effect in office mode (no minigame input to block here).
        _officeColleagueJokeRoutine = StartCoroutine(OfficeFreezeSeconds(ColleagueJokeSystem.BlockSeconds));
    }

    private IEnumerator OfficeFreezeSeconds(float seconds)
    {
        if (seconds <= 0f)
        {
            RandomEventSystem.Clear();
            _officeColleagueJokeRoutine = null;
            yield break;
        }

        // Only freeze if the game is currently running (don’t fight pause/rent popups).
        bool canRestore = GameController.GameState == GameState.Running;
        if (canRestore)
        {
            GameController.GameState = GameState.Stopped;
        }

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (canRestore && GameController.GameState == GameState.Stopped)
        {
            GameController.GameState = GameState.Running;
        }

        RandomEventSystem.Clear();
        _officeColleagueJokeRoutine = null;
    }
    private void PlayCoffeeMissingFlash()
    {
        EnsureCoffeeFlashUi();

        if (_coffeeFlashRoutine != null)
        {
            StopCoroutine(_coffeeFlashRoutine);
        }

        _coffeeFlashRoutine = StartCoroutine(ShowCoffeeMissingFlash());
    }

    private void ShowCoffeeMissingIcon()
    {
        EnsureCoffeeFlashUi();

        Image icon = GetCoffeeMissingIcon();
        if (icon == null)
        {
            return;
        }

        icon.transform.SetAsLastSibling();
        icon.gameObject.SetActive(true);
    }

    private void HideCoffeeMissingIcon()
    {
        Image icon = GetCoffeeMissingIcon();
        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(false);
    }

    private void ShowTrafficJamIcon()
    {
        EnsureTrafficJamUi();

        Image icon = GetTrafficJamIcon();
        if (icon == null)
        {
            return;
        }

        ApplyTopRightIconLayout(
            icon,
            _trafficJamIconSize,
            _trafficJamIconAnchoredPosition);
        icon.transform.SetAsLastSibling();
        icon.gameObject.SetActive(true);
    }

    private void HideTrafficJamIcon()
    {
        Image icon = GetTrafficJamIcon();
        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(false);
    }

    private void EnsureTrafficJamUi()
    {
        if (_trafficJamIcon != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (_runtimeTrafficJamIcon != null)
        {
            return;
        }

        GameObject iconObject = new GameObject("TrafficJamIcon");
        iconObject.transform.SetParent(canvas.transform, false);
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 1f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.pivot = new Vector2(1f, 1f);
        iconRect.sizeDelta = _trafficJamIconSize;
        iconRect.anchoredPosition = _trafficJamIconAnchoredPosition;

        _runtimeTrafficJamIcon = iconObject.AddComponent<Image>();
        _runtimeTrafficJamIcon.sprite = _trafficJamIconSprite;
        _runtimeTrafficJamIcon.preserveAspect = true;
        _runtimeTrafficJamIcon.color = Color.white;
        _runtimeTrafficJamIcon.raycastTarget = false;
        iconObject.SetActive(false);
    }

    private Image GetTrafficJamIcon()
    {
        if (_trafficJamIcon != null)
        {
            if (_trafficJamIconSprite != null && _trafficJamIcon.sprite == null)
            {
                _trafficJamIcon.sprite = _trafficJamIconSprite;
            }

            return _trafficJamIcon;
        }

        if (_runtimeTrafficJamIcon != null && _trafficJamIconSprite != null && _runtimeTrafficJamIcon.sprite == null)
        {
            _runtimeTrafficJamIcon.sprite = _trafficJamIconSprite;
        }

        return _runtimeTrafficJamIcon;
    }

    private void ShowTechnicalUpdateIcon()
    {
        EnsureTechnicalUpdateUi();

        Image icon = GetTechnicalUpdateIcon();
        if (icon == null)
        {
            return;
        }

        ApplyTopRightIconLayout(
            icon,
            _technicalUpdateIconSize,
            _technicalUpdateIconAnchoredPosition);
        icon.transform.SetAsLastSibling();
        icon.gameObject.SetActive(true);
    }

    private void HideTechnicalUpdateIcon()
    {
        Image icon = GetTechnicalUpdateIcon();
        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(false);
    }

    private void EnsureTechnicalUpdateUi()
    {
        if (_technicalUpdateIcon != null)
        {
            return;
        }

        if (_runtimeTechnicalUpdateIcon != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject iconObject = new GameObject("TechnicalUpdateIcon");
        iconObject.transform.SetParent(canvas.transform, false);
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 1f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.pivot = new Vector2(1f, 1f);
        iconRect.sizeDelta = _technicalUpdateIconSize;
        iconRect.anchoredPosition = _technicalUpdateIconAnchoredPosition;

        _runtimeTechnicalUpdateIcon = iconObject.AddComponent<Image>();
        _runtimeTechnicalUpdateIcon.sprite = _technicalUpdateIconSprite;
        _runtimeTechnicalUpdateIcon.preserveAspect = true;
        _runtimeTechnicalUpdateIcon.color = Color.white;
        _runtimeTechnicalUpdateIcon.raycastTarget = false;
        iconObject.SetActive(false);
    }

    private Image GetTechnicalUpdateIcon()
    {
        if (_technicalUpdateIcon != null)
        {
            if (_technicalUpdateIconSprite != null && _technicalUpdateIcon.sprite == null)
            {
                _technicalUpdateIcon.sprite = _technicalUpdateIconSprite;
            }

            return _technicalUpdateIcon;
        }

        if (_runtimeTechnicalUpdateIcon != null && _technicalUpdateIconSprite != null && _runtimeTechnicalUpdateIcon.sprite == null)
        {
            _runtimeTechnicalUpdateIcon.sprite = _technicalUpdateIconSprite;
        }

        return _runtimeTechnicalUpdateIcon;
    }

    private static void ApplyTopRightIconLayout(
        Image icon,
        Vector2 size,
        Vector2 anchoredPosition)
    {
        if (icon == null)
        {
            return;
        }

        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(1f, 1f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.pivot = new Vector2(1f, 1f);
        iconRect.sizeDelta = new Vector2(
            Mathf.Max(1f, size.x),
            Mathf.Max(1f, size.y));
        iconRect.anchoredPosition = anchoredPosition;
    }

    private IEnumerator ShowCoffeeMissingFlash()
    {
        Image flash = GetRedFlashOverlay();
        float halfDuration = _coffeeFlashSeconds * 0.5f;

        if (flash != null)
        {
            flash.gameObject.SetActive(true);
            flash.color = new Color(0.8f, 0.1f, 0.1f, 0f);
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 0.35f, elapsed / halfDuration);
                flash.color = new Color(0.8f, 0.1f, 0.1f, alpha);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0.35f, 0f, elapsed / halfDuration);
                flash.color = new Color(0.8f, 0.1f, 0.1f, alpha);
                yield return null;
            }

            flash.gameObject.SetActive(false);
        }

        RefreshVisuals();
        _coffeeFlashRoutine = null;
    }

    private void EnsureCoffeeFlashUi()
    {
        if (_redFlashOverlay != null && _coffeeMissingIcon != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (_redFlashOverlay == null)
        {
            GameObject flashObject = new GameObject("CoffeeMissingFlash");
            flashObject.transform.SetParent(canvas.transform, false);
            RectTransform flashRect = flashObject.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            _runtimeRedFlashOverlay = flashObject.AddComponent<Image>();
            _runtimeRedFlashOverlay.color = new Color(0.8f, 0.1f, 0.1f, 0f);
            _runtimeRedFlashOverlay.raycastTarget = false;
            flashObject.SetActive(false);
        }

        if (_coffeeMissingIcon == null)
        {
            GameObject iconObject = new GameObject("CoffeeMissingIcon");
            iconObject.transform.SetParent(canvas.transform, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(96f, 96f);
            _runtimeCoffeeMissingIcon = iconObject.AddComponent<Image>();
            _runtimeCoffeeMissingIcon.color = new Color(1f, 1f, 1f, 0.9f);
            _runtimeCoffeeMissingIcon.raycastTarget = false;

            GameObject ringObject = new GameObject("EmptyCupRing");
            ringObject.transform.SetParent(iconObject.transform, false);
            RectTransform ringRect = ringObject.AddComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = Vector2.zero;
            ringRect.offsetMax = Vector2.zero;
            Image ringImage = ringObject.AddComponent<Image>();
            ringImage.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
            ringImage.raycastTarget = false;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(iconObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "Ø";
            label.fontSize = 72f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.85f, 0.15f, 0.15f, 1f);
            label.raycastTarget = false;

            iconObject.SetActive(false);
        }
    }

    private Image GetRedFlashOverlay()
    {
        return _redFlashOverlay != null ? _redFlashOverlay : _runtimeRedFlashOverlay;
    }

    private Image GetCoffeeMissingIcon()
    {
        return _coffeeMissingIcon != null ? _coffeeMissingIcon : _runtimeCoffeeMissingIcon;
    }

    private IEnumerator TryStageEventDuringMission(MissionObjects mission)
    {
        yield return null;

        if (mission == null || !mission.isStarted)
        {
            yield break;
        }

        float delay = Random.Range(_stageEventMinDelay, _stageEventMaxDelay);
        yield return new WaitForSeconds(delay);

        if (mission == null
            || !mission.isStarted
            || !StageEventSystem.CanTriggerThisStage
            || IsAnyEventRunning())
        {
            yield break;
        }

        if (Random.value > _stageEventChanceDuringStage)
        {
            yield break;
        }

        GameObject miniGame = mission.ActiveMiniGame;
        if (miniGame == null)
        {
            yield break;
        }

        float roll = Random.value;
        if (roll < 0.25f)
        {
            yield return PlaySuddenCall(mission, miniGame);
        }
        else if (roll < 0.5f)
        {
            yield return PlayChairFell(mission, miniGame);
        }
        else if (roll < 0.75f)
        {
            yield return PlayCatDroppedCup(mission, miniGame);
        }
        else
        {
            yield return PlayColleagueJoke(mission, miniGame);
        }

        _stageEventRoutine = null;
    }

    public void TriggerBossLookingForNextOrder()
    {
        if (!IsAnyEventRunning() && !HasPendingEvent())
        {
            _bossLookingPendingForNextOrder = true;
        }
    }

    private void TriggerBossLookingForOrder(DeveloperSimulation developer, ActiveOrder order)
    {
        if (order == null || order.isTutorialOrder || IsAnyEventRunning())
        {
            return;
        }

        BossLookingSystem.Activate();
        RandomEventSystem.Activate(RandomEventType.BossLooking, RandomEventDuration.NextOrder);

        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.BossLooking),
            RandomEventSystem.GetEventDescription(RandomEventType.BossLooking),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.BossLooking, RandomEventDuration.NextOrder));

        EnsureBossLookingVisual();
        if (_bossLookingVisual != null)
        {
            _bossLookingVisual.BeginVisit();
        }
    }

    private void EnsureBossLookingVisual()
    {
        if (_bossLookingVisual != null)
        {
            return;
        }

        _bossLookingVisual = BossLookingVisual.Create(null, _bossLookingSettings);
    }

    private void HideBossLookingVisual()
    {
        if (_bossLookingFinishRoutine != null)
        {
            StopCoroutine(_bossLookingFinishRoutine);
            _bossLookingFinishRoutine = null;
        }

        HideBossLookingResultOverlay();

        if (_bossLookingVisual == null)
        {
            return;
        }

        _bossLookingVisual.Hide();
        _bossLookingVisual = null;
    }

    private void EnsureBossLookingResultOverlay()
    {
        if (_bossLookingResultOverlay != null)
        {
            return;
        }

        _bossLookingResultOverlay = BossLookingResultOverlay.Create(_bossLookingSettings);
    }

    private void HideBossLookingResultOverlay()
    {
        if (_bossLookingResultOverlay == null)
        {
            return;
        }

        _bossLookingResultOverlay.Hide();
        _bossLookingResultOverlay = null;
    }

    private IEnumerator FinishBossLookingVisit(DeveloperSimulation developer, ActiveOrder order, float averageQuality)
    {
        EnsureBossLookingVisual();
        if (_bossLookingVisual == null || developer == null)
        {
            BossLookingSystem.Clear();
            _bossLookingFinishRoutine = null;
            yield break;
        }

        if (_bossLookingVisual.IsPerfectQuality(averageQuality))
        {
            GrantBossLookingExperienceBonus(developer, order, averageQuality);
        }

        Transform playerTransform = developer.transform;
        Vector3 meetPosition = playerTransform.position + _bossLookingSettings.playerMeetOffset;
        yield return _bossLookingVisual.WalkTo(meetPosition);

        BossLookingResult result = _bossLookingVisual.EvaluateResult(averageQuality);
        EnsureBossLookingResultOverlay();
        if (_bossLookingResultOverlay != null)
        {
            _bossLookingResultOverlay.ShowAt(
                playerTransform,
                result,
                result == BossLookingResult.Satisfied);
        }

        if (_bossLookingSettings.resultDisplaySeconds > 0f)
        {
            yield return new WaitForSeconds(_bossLookingSettings.resultDisplaySeconds);
        }

        HideBossLookingResultOverlay();
        yield return _bossLookingVisual.WalkToExit();
        _bossLookingVisual = null;
        _bossLookingFinishRoutine = null;
        BossLookingSystem.Clear();
    }

    private float GetOrderAverageQuality(ActiveOrder order)
    {
        if (order == null || order.milestones == null || order.milestones.Length == 0)
        {
            return 0f;
        }

        float totalScore = 0f;
        foreach (Milestone milestone in order.milestones)
        {
            totalScore += milestone != null ? milestone.progress : 0f;
        }

        return totalScore / order.milestones.Length;
    }

    private void GrantBossLookingExperienceBonus(DeveloperSimulation developer, ActiveOrder order, float averageQuality)
    {
        if (developer == null || order == null || order.isTutorialOrder)
        {
            return;
        }

        if (averageQuality < _bossLookingSettings.perfectScoreThreshold)
        {
            return;
        }

        float bonusExperience = order.budget * averageQuality * _bossLookingSettings.experienceBonusPercent;
        developer.AddExperience(bonusExperience);
    }

    private IEnumerator PlayColleagueJoke(MissionObjects mission, GameObject miniGame)
    {
        StageEventSystem.MarkTriggered();

        Transform minigameCanvas = GetFliesParent(miniGame != null ? miniGame.transform : null);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.ColleagueJoke),
            RandomEventSystem.GetEventDescription(RandomEventType.ColleagueJoke),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.ColleagueJoke, RandomEventDuration.NextProgrammingStage),
            minigameCanvas);

        SetMinigameInputBlocked(miniGame, true);
        StageEventSystem.SetBlocking(true);

        float elapsed = 0f;
        while (elapsed < ColleagueJokeSystem.BlockSeconds)
        {
            if (mission == null || !mission.isStarted)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        StageEventSystem.SetBlocking(false);
        SetMinigameInputBlocked(miniGame, false);
    }

    private IEnumerator PlaySuddenCall(MissionObjects mission, GameObject miniGame)
    {
        StageEventSystem.MarkTriggered();
        StartLoopingEventSound(
            ref _momCallingEventSource,
            _momCallingEventSound,
            _momCallingEventSoundVolume,
            "MomCallingEventAudio");

        Transform minigameCanvas = GetFliesParent(miniGame != null ? miniGame.transform : null);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.SuddenCall),
            RandomEventSystem.GetEventDescription(RandomEventType.SuddenCall),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.SuddenCall, RandomEventDuration.NextProgrammingStage),
            minigameCanvas);

        SuddenCallOverlay overlay = SuddenCallOverlay.Create(miniGame.transform, GetPhoneSprite());
        if (overlay != null)
        {
            overlay.Show();
        }

        SetMinigameInputBlocked(miniGame, true);
        StageEventSystem.SetBlocking(true);

        float elapsed = 0f;
        float phoneAnimDuration = Mathf.Max(0.1f, _phoneIconDisplaySeconds);
        while (elapsed < SuddenCallSystem.BlockSeconds)
        {
            if (mission == null || !mission.isStarted)
            {
                break;
            }

            if (overlay != null && elapsed < phoneAnimDuration)
            {
                float pulse = 1f + Mathf.Sin(elapsed * Mathf.PI * 6f) * 0.12f;
                overlay.SetPhoneScale(pulse);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        StageEventSystem.SetBlocking(false);
        SetMinigameInputBlocked(miniGame, false);

        if (overlay != null)
        {
            overlay.Hide();
        }
        StopLoopingEventSound(_momCallingEventSource);
    }

    private IEnumerator PlayChairFell(MissionObjects mission, GameObject miniGame)
    {
        StageEventSystem.MarkTriggered();

        string deviceType = mission != null && mission.milestone != null
            ? mission.milestone.devicetype
            : "Concept";

        Transform minigameCanvas = GetFliesParent(miniGame != null ? miniGame.transform : null);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.ChairFell),
            RandomEventSystem.GetEventDescription(RandomEventType.ChairFell),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.ChairFell, RandomEventDuration.NextProgrammingStage),
            minigameCanvas);

        ChairFellOverlay overlay = ChairFellOverlay.Create(miniGame.transform, GetChairSprite(), deviceType);
        if (overlay != null)
        {
            overlay.Show();
            yield return overlay.PlayFallAnimation();
        }

        SetMinigameInputBlocked(miniGame, true);
        StageEventSystem.SetBlocking(true);

        float elapsed = 0f;
        while (elapsed < ChairFellSystem.BlockSeconds)
        {
            if (mission == null || !mission.isStarted)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        StageEventSystem.SetBlocking(false);
        SetMinigameInputBlocked(miniGame, false);

        if (overlay != null)
        {
            overlay.Hide();
        }
    }

    private IEnumerator PlayCatDroppedCup(MissionObjects mission, GameObject miniGame)
    {
        StageEventSystem.MarkTriggered();

        Transform minigameCanvas = GetFliesParent(miniGame != null ? miniGame.transform : null);
        ShowEventPopup(
            RandomEventSystem.GetEventTitle(RandomEventType.CatDroppedCup),
            RandomEventSystem.GetEventDescription(RandomEventType.CatDroppedCup),
            RandomEventSystem.GetEventEffectSummary(RandomEventType.CatDroppedCup, RandomEventDuration.NextProgrammingStage),
            minigameCanvas);

        CatDroppedCupOverlay overlay = CatDroppedCupOverlay.Create(
            miniGame.transform,
            GetCupSprite(),
            _cupShardSprite,
            _cupBodySize);

        if (overlay != null)
        {
            overlay.Show();
            yield return overlay.PlayFallAnimation();
            PlayCupSmashSound();
        }

        SetMinigameInputBlocked(miniGame, true);
        StageEventSystem.SetBlocking(true);

        if (overlay != null)
        {
            yield return overlay.PlayBlockPhase();
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < CatDroppedCupSystem.BlockSeconds)
            {
                if (mission == null || !mission.isStarted)
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        StageEventSystem.SetBlocking(false);
        SetMinigameInputBlocked(miniGame, false);

        if (overlay != null)
        {
            overlay.Hide();
        }
    }

    private void PlayCupSmashSound()
    {
        if (_cupSmashSound == null)
        {
            return;
        }

        EnsureAudioSource();
        _audioSource.PlayOneShot(_cupSmashSound, _cupSmashSoundVolume);
    }

    private Sprite GetCupSprite()
    {
        if (_cupSprite != null)
        {
            return _cupSprite;
        }

        GameObject coffee = GameObject.Find("Coffee");
        if (coffee != null)
        {
            SpriteRenderer renderer = coffee.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                return renderer.sprite;
            }
        }

        return null;
    }

    private Sprite GetChairSprite()
    {
        if (_chairSprite != null)
        {
            return _chairSprite;
        }

        if (!string.IsNullOrEmpty(_chairSourceObjectName))
        {
            GameObject chairObject = GameObject.Find(_chairSourceObjectName);
            if (chairObject != null)
            {
                SpriteRenderer renderer = chairObject.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    return renderer.sprite;
                }
            }
        }

        GameController gameController = FindFirstObjectByType<GameController>();
        if (gameController != null && gameController.Desks != null)
        {
            foreach (GameObject desk in gameController.Desks)
            {
                if (desk == null)
                {
                    continue;
                }

                SpriteRenderer renderer = desk.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    return renderer.sprite;
                }
            }
        }

        return null;
    }

    private Sprite GetPhoneSprite()
    {
        if (_phoneSprite != null)
        {
            return _phoneSprite;
        }

        if (_phoneIcon != null && _phoneIcon.sprite != null)
        {
            return _phoneIcon.sprite;
        }

        if (!string.IsNullOrEmpty(_phoneImageObjectName))
        {
            GameObject phoneObject = GameObject.Find(_phoneImageObjectName);
            if (phoneObject != null)
            {
                Image image = phoneObject.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    return image.sprite;
                }
            }
        }

        return null;
    }

    private static void SetMinigameInputBlocked(GameObject miniGame, bool blocked)
    {
        if (miniGame == null)
        {
            return;
        }

        TMP_InputField[] inputFields = miniGame.GetComponentsInChildren<TMP_InputField>(true);
        foreach (TMP_InputField inputField in inputFields)
        {
            if (inputField == null)
            {
                continue;
            }

            inputField.interactable = !blocked;
            inputField.readOnly = blocked;
            if (blocked)
            {
                inputField.DeactivateInputField();
                inputField.ReleaseSelection();
            }
        }

        Selectable[] selectables = miniGame.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable selectable in selectables)
        {
            if (selectable == null)
            {
                continue;
            }

            selectable.interactable = !blocked;
        }
    }

    private IEnumerator ShowPhoneIconAnimation()
    {
        EnsurePhoneIcon();

        Image icon = GetPhoneIcon();
        if (icon == null)
        {
            yield break;
        }

        icon.transform.SetAsLastSibling();
        icon.gameObject.SetActive(true);

        float duration = Mathf.Max(0.1f, _phoneIconDisplaySeconds);
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(elapsed * Mathf.PI * 6f) * 0.12f;
            icon.transform.localScale = baseScale * pulse;
            yield return null;
        }

        HidePhoneIcon();
    }

    private void EnsurePhoneIcon()
    {
        if (_phoneIcon != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject iconObject = new GameObject("SuddenCallPhoneIcon");
        iconObject.transform.SetParent(canvas.transform, false);

        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(120f, 120f);

        _runtimePhoneIcon = iconObject.AddComponent<Image>();
        _runtimePhoneIcon.color = new Color(0.2f, 0.75f, 1f, 0.95f);
        _runtimePhoneIcon.raycastTarget = false;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(iconObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "CALL";
        label.fontSize = 28f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        iconObject.SetActive(false);
    }

    private Image GetPhoneIcon()
    {
        return _phoneIcon != null ? _phoneIcon : _runtimePhoneIcon;
    }

    private void HidePhoneIcon()
    {
        Image icon = GetPhoneIcon();
        if (icon == null)
        {
            return;
        }

        icon.transform.localScale = Vector3.one;
        icon.gameObject.SetActive(false);
    }

    private ParticleSystem GetActiveSmokeParticles()
    {
        if (_runtimeSmokeInstance != null)
        {
            return _runtimeSmokeInstance;
        }

        return _smokeParticles;
    }

    private void PlaySmokeVisual()
    {
        EnsureSmokeAnchor();

        ParticleSystem particles = GetActiveSmokeParticles();
        if (particles == null)
        {
            particles = CreateFallbackSmokeParticles();
        }

        if (particles == null)
        {
            return;
        }

        particles.gameObject.SetActive(true);
        if (!particles.isPlaying)
        {
            particles.Play(true);
        }
    }

    private void StopSmokeVisual()
    {
        ParticleSystem particles = GetActiveSmokeParticles();
        if (particles == null)
        {
            return;
        }

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.gameObject.SetActive(false);
    }

    private void EnsureSmokeAnchor()
    {
        if (_smokeAnchor != null)
        {
            return;
        }

        if (string.IsNullOrEmpty(_programmingPcObjectName))
        {
            return;
        }

        GameObject programmingPc = GameObject.Find(_programmingPcObjectName);
        if (programmingPc != null)
        {
            _smokeAnchor = programmingPc.transform;
        }
    }

    private ParticleSystem CreateFallbackSmokeParticles()
    {
        if (_smokeParticlePrefab != null)
        {
            EnsureSmokeAnchor();
            Transform parent = _smokeAnchor != null ? _smokeAnchor : transform;
            _runtimeSmokeInstance = Instantiate(_smokeParticlePrefab, parent);
            _runtimeSmokeInstance.transform.localPosition = _smokeLocalOffset;
            _runtimeSmokeInstance.transform.localRotation = Quaternion.identity;
            _runtimeSmokeInstance.gameObject.SetActive(false);
            _createdRuntimeSmoke = true;
            return _runtimeSmokeInstance;
        }

        if (_smokeParticles != null)
        {
            return _smokeParticles;
        }

        EnsureSmokeAnchor();
        if (_smokeAnchor == null)
        {
            return null;
        }

        GameObject smokeObject = new GameObject("OverheatSmoke");
        smokeObject.transform.SetParent(_smokeAnchor, false);
        smokeObject.transform.localPosition = _smokeLocalOffset;

        _runtimeSmokeInstance = smokeObject.AddComponent<ParticleSystem>();
        ConfigureDefaultSmoke(_runtimeSmokeInstance);
        _runtimeSmokeInstance.gameObject.SetActive(false);
        _createdRuntimeSmoke = true;
        return _runtimeSmokeInstance;
    }

    private static void ConfigureDefaultSmoke(ParticleSystem smokeParticles)
    {
        var main = smokeParticles.main;
        main.loop = true;
        main.startLifetime = 1.4f;
        main.startSpeed = 0.7f;
        main.startSize = 0.35f;
        main.startColor = new Color(0.15f, 0.15f, 0.15f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        main.playOnAwake = false;

        var emission = smokeParticles.emission;
        emission.rateOverTime = 24f;

        var shape = smokeParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;

        var velocity = smokeParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);

        var colorOverLifetime = smokeParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 0f),
                new GradientColorKey(new Color(0.05f, 0.05f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient; 

        ParticleSystemRenderer renderer = smokeParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 20;
        }
    }

    private void OnDestroy()
    {
        if (_createdRuntimeSmoke && _runtimeSmokeInstance != null)
        {
            Destroy(_runtimeSmokeInstance.gameObject);
        }
    }
}

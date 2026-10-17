using System;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(InteractiveObj))]
public class MissionObjects : MonoBehaviour
{
    public static event Action<MissionObjects, GameObject> MissionStarted;
    public static event Action<MissionObjects, float> MissionEnded;

    private const float ClosedMissionScore = 0f;

    private GameObject playerObj;
    private GameObject MiniGameObj;
    public bool isStarted = false;
    public Milestone milestone;
    public ActiveOrder activeOrder;
    public GameObject ActiveMiniGame => MiniGameObj;
    public float timer = -1;
    public static bool IsInMission;
    public bool CanAcceptCompletion => isStarted && !hasEnded && playerObj != null && activeOrder != null;
    private float missionStartTime;
    private bool hasEnded;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        RemoveHoverEffects();
    }

    void Start()
    {
    }
    private void OnEnable()
    {
        GetComponent<InteractiveObj>().OnInteractionAccept += MissionStart;
    }

    private void OnDisable()
    {
        GetComponent<InteractiveObj>().OnInteractionAccept -= MissionStart;
    }

    private void RemoveHoverEffects()
    {
        InteractiveHoverHighlight highlight = GetComponent<InteractiveHoverHighlight>();
        if (highlight != null)
        {
            Destroy(highlight);
        }

        HoverScaleEffect scaleEffect = GetComponent<HoverScaleEffect>();
        if (scaleEffect != null)
        {
            Destroy(scaleEffect);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isStarted || hasEnded || activeOrder == null)
        {
            return;
        }

        float strictTimeLimit = activeOrder.GetConditionValue(SpecialConditionType.StrictTimer, -1f);
        if (strictTimeLimit > 0f && Time.time - missionStartTime > strictTimeLimit)
        {
            MissionEnd(0f);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMiniGame();
        }
    }
    void MissionStart(GameObject obj)
    {
        if (hasEnded || isStarted)
        {
            return;
        }

        isStarted = true;
        hasEnded = false;
        missionStartTime = Time.time;
        GetComponent<InteractiveObj>().enabled = false;
        MiniGameObj = Instantiate(milestone.minigamePrefab, new Vector3(200, 200, 0), Quaternion.identity, transform);
        ConfigureSpecialConditions(MiniGameObj);
        ConfigureBossChallenge(MiniGameObj);
        ConfigureSelectedEvents(MiniGameObj);
        ConfigureCloseControls(MiniGameObj);
        ConfigureTypingSound(MiniGameObj);
        playerObj = obj;
        IsInMission = true;
        MissionStarted?.Invoke(this, obj);
    }

    private void ConfigureTypingSound(GameObject miniGameObj)
    {
        GameController gameController = FindFirstObjectByType<GameController>();
        if (gameController != null)
        {
            gameController.AddTypingSoundToMinigame(miniGameObj);
        }
    }

    private void ConfigureSelectedEvents(GameObject miniGameObj)
    {
        if (miniGameObj == null)
        {
            return;
        }

        bool isConceptGame = miniGameObj.GetComponentInChildren<ConceptPuzzle>(true) != null;
        bool isDesignGame = miniGameObj.GetComponentInChildren<DesignPuzzle>(true) != null;
        bool isProgrammingGame = miniGameObj.GetComponentInChildren<ProgrammingBugGame>(true) != null;

        bool shouldMirror = (isConceptGame || isDesignGame || isProgrammingGame)
            && MinigameEventSelectionController.IsMirrorScreenSelected;
        if (!shouldMirror)
        {
            return;
        }

        MirrorScreenEvent mirrorEvent = miniGameObj.GetComponent<MirrorScreenEvent>();
        if (mirrorEvent == null)
        {
            mirrorEvent = miniGameObj.AddComponent<MirrorScreenEvent>();
        }

        string mirrorTargetName = isConceptGame
            ? "BossWordInput"
            : (isDesignGame ? "BugHuntArea" : "Screen");
        float minimumMirrorDuration = isProgrammingGame ? 35f : 3f;
        float maximumMirrorDuration = isProgrammingGame ? 35f : 5f;
        mirrorEvent.Configure(
            2f,
            3f,
            minimumMirrorDuration,
            maximumMirrorDuration,
            mirrorTargetName);
    }

    private void ConfigureSpecialConditions(GameObject miniGameObj)
    {
        if (miniGameObj == null || activeOrder == null)
        {
            return;
        }

        ProgrammingBugGame programmingGame = miniGameObj.GetComponentInChildren<ProgrammingBugGame>();
        if (programmingGame != null)
        {
            programmingGame.ConfigureConditions(
                activeOrder.HasCondition(SpecialConditionType.GreenBugs),
                activeOrder.HasCondition(SpecialConditionType.PurpleBugs));
        }
    }

    private void ConfigureBossChallenge(GameObject miniGameObj)
    {
        if (miniGameObj == null || activeOrder == null || !BossMinigameConfig.IsFinalBoss(activeOrder.manager))
        {
            return;
        }

        ProgrammingBugGame programmingGame = miniGameObj.GetComponentInChildren<ProgrammingBugGame>();
        if (programmingGame != null)
        {
            programmingGame.ConfigureBossChallenge(activeOrder.manager);
        }
    }

    private void ConfigureCloseControls(GameObject miniGameObj)
    {
        if (miniGameObj == null)
        {
            return;
        }

        Button[] buttons = miniGameObj.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null || !button.name.ToLowerInvariant().Contains("close"))
            {
                continue;
            }

            button.onClick.AddListener(CloseMiniGame);
        }
    }

    public void CloseMiniGame()
    {
        if (!CanAcceptCompletion)
        {
            AbortMission();
            return;
        }

        MissionEnd(ClosedMissionScore);
    }

    public void AbortMission()
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        IsInMission = false;
        GetComponent<InteractiveObj>().enabled = false;

        if (MiniGameObj != null)
        {
            Destroy(MiniGameObj);
            MiniGameObj = null;
        }
    }

    public static void ForceCloseAllActive()
    {
        MissionObjects[] missions = FindObjectsByType<MissionObjects>(FindObjectsSortMode.None);
        foreach (MissionObjects mission in missions)
        {
            if (mission != null)
            {
                mission.AbortMission();
            }
        }

        IsInMission = false;
    }

    public void MissionEnd(float score)
    {
        if (hasEnded)
        {
            return;
        }

        if (playerObj == null || activeOrder == null || milestone == null)
        {
            AbortMission();
            return;
        }

        hasEnded = true;
        IsInMission = false;
        GetComponent<InteractiveObj>().enabled = false;
        if (MiniGameObj != null)
        {
            Destroy(MiniGameObj.gameObject, 1.5f);
            MiniGameObj = null;
        }
        Destroy(this, 1.5f);
        MissionEnded?.Invoke(this, score);

        DeveloperSimulation developer = playerObj.GetComponent<DeveloperSimulation>();
        if (developer != null)
        {
            developer.ProgressComplete(milestone, score);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
public enum ManagerState
{
    Idle,
    WaitingForTaskCompletion,
}

public enum ManagerEmotion
{
    Satisfied,
    Dissatisfied,
    Sad
}

public class ManagerSimulation : MonoBehaviour
{
    public static event Action<ManagerSimulation, ActiveOrder> OrderOffered;
    public static event Action<ManagerSimulation, ActiveOrder, DeveloperSimulation> OrderAccepted;

    public ManagerId managerId;
    public ActiveOrder currentOrder;
    public GameObject taskBalloon;
    public TextMeshPro tasktext;
    public TextMeshPro Nametext;
    public TextMeshPro Companytext;
    public float StartTime;
    public float DestroyTime = 10;
    public float deadline;
    public string Name;
    public string Companyname;
    public ManagerState managerState = ManagerState.Idle;
    
    [Header("Emotions (assign sprites in prefab)")]
    [SerializeField] private SpriteRenderer _emotionRenderer;
    [SerializeField] private Sprite _emotionSatisfiedSprite;
    [SerializeField] private Sprite _emotionDissatisfiedSprite;
    [SerializeField] private Sprite _emotionSadSprite;
    [SerializeField] private float _emotionDisplaySeconds = 2f;
    [SerializeField] [Range(0f, 1f)] private float _satisfiedMinQuality = 0.9f;
    [SerializeField] [Range(0f, 1f)] private float _sadMaxQuality = 0.5f;
    [SerializeField] [Min(0.1f)] private float _infoAppearDistance = 2.5f;

    private OrderGenerator orderGenerator;
    private ActiveOrder _acceptedOrder;
    private ManagerEmotion _pendingEmotion = ManagerEmotion.Dissatisfied;
    private bool _hasPendingEmotion;
    private Coroutine _emotionRoutine;
    private CharacterNameHoverDisplay _hoverInfoDisplay;
    private CharacterNameHoverDisplay _hoverBalloonDisplay;
    private GameObject _managerInfoRoot;
    private GameObject[] _managerInfoPortraitObjects;
    private bool _hideInfoAfterOrderFinished;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Companytext.text = Companyname;
        Nametext.text = Name;
        tasktext = taskBalloon != null
            ? taskBalloon.GetComponentInChildren<TextMeshPro>(true)
            : tasktext;
        HideOrderTextInSpeakBalloon();
        orderGenerator = FindFirstObjectByType<OrderGenerator>();
        currentOrder = null;
        ConfigureHoverNameLabels();
        ConfigureHoverSpeakBalloon();
        HideSpeakBalloon();

        if (_emotionRenderer != null)
        {
            _emotionRenderer.gameObject.SetActive(false);
        }
    }
    private void OnEnable()
    {
        GetComponent<InteractiveObj>().OnInteractionAccept += taskAccept;
        DeveloperSimulation.OrderSubmittedDetailed += HandleOrderSubmittedDetailed;
    }

    private void OnDisable()
    {
        GetComponent<InteractiveObj>().OnInteractionAccept -= taskAccept;
        DeveloperSimulation.OrderSubmittedDetailed -= HandleOrderSubmittedDetailed;
    }
    private void taskAccept(GameObject sender)
    {
        if (currentOrder == null) { return; }
        if (managerState == ManagerState.Idle)
        {
            DeveloperSimulation developer = sender.GetComponent<DeveloperSimulation>();
            if (developer == null)
            {
                return;
            }

            ActiveOrder acceptedOrder = currentOrder;
            developer.receiveTask(acceptedOrder);
            deadline = currentOrder.deadline;
            if (managerId == ManagerId.HideoK && MinigameEventSelectionController.IsOldAgeSelected)
            {
                deadline *= 0.8f;
            }
            StartTime = Time.time;
            _acceptedOrder = acceptedOrder;
            _hideInfoAfterOrderFinished = false;
            currentOrder = null;
            managerState = ManagerState.WaitingForTaskCompletion;
            MinigameEventSelectionController.ActivateForHideoTask(acceptedOrder);
            OrderAccepted?.Invoke(this, acceptedOrder, developer);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (GameController.GameState == GameState.Stopped) return;

        if (currentOrder != null)
        {
            tasktext.text = " Genre:" + currentOrder.genre
                + "\n Budget:" + currentOrder.budget
                + "\n Deadline:" + currentOrder.deadline + "min"
                + "\n " + currentOrder.GetConditionsText();
        }
        else if (managerState == ManagerState.WaitingForTaskCompletion)
        {
            float elapsedSeconds = TrafficJamSystem.GetAdjustedElapsedSeconds(StartTime);
            tasktext.text = "Waiting for task..." + (int)elapsedSeconds / 60 + "min " + (int)elapsedSeconds % 60 + "s";
            if (elapsedSeconds > deadline * 60)
            {
                tasktext.text += "(Deadline!)";
                tasktext.color = Color.red;
            }
            if (elapsedSeconds > (deadline + 1) * 60)
            {
                MissionObjects.ForceCloseAllActive();

                DeveloperSimulation developer = FindFirstObjectByType<DeveloperSimulation>();
                if (developer != null && developer.HasActiveOrder())
                {
                    developer.Cheat(-1);
                }

                EndTask();
            }
        }
        else
        {
            tasktext.text = "No order data";
        }
        HideOrderTextInSpeakBalloon();
        GetComponent<InteractiveObj>().enabled = currentOrder != null || GetComponent<PurchaseScript>();
        UpdateManagerInfoVisibility();
    }
    public bool isOveredDeadline()
    {
        return TrafficJamSystem.GetAdjustedElapsedSeconds(StartTime) > deadline * 60;
    }
    public void PlusTask()
    {
        if (currentOrder == null && managerState == ManagerState.Idle)
        {
            DeveloperSimulation developer = FindFirstObjectByType<DeveloperSimulation>();
            int playerLevel = developer != null ? developer.devInfo.level : 1;
            if (orderGenerator == null)
            {
                orderGenerator = FindFirstObjectByType<OrderGenerator>();
            }

            currentOrder = orderGenerator != null
                ? orderGenerator.GenerateOrder(managerId, playerLevel)
                : null;
            if (currentOrder != null)
            {
                _hideInfoAfterOrderFinished = false;
                OrderOffered?.Invoke(this, currentOrder);
            }
        }

        HideSpeakBalloon();
        print("OK!");
        GetComponent<Animator>().speed = 0;
    }
    public void EndTask()
    {
        GetComponent<Animator>().speed = 0;
        Destroy(gameObject, DestroyTime);
    }

    private void HandleOrderSubmittedDetailed(DeveloperSimulation developer, ActiveOrder order, float quality, bool overDeadline)
    {
        if (order == null || _acceptedOrder == null || order != _acceptedOrder)
        {
            return;
        }

        _pendingEmotion = EvaluateEmotion(quality, overDeadline);
        _hasPendingEmotion = true;
        _acceptedOrder = null;
        _hideInfoAfterOrderFinished = true;
        UpdateManagerInfoVisibility();
    }

    private ManagerEmotion EvaluateEmotion(float quality, bool overDeadline)
    {
        if (quality <= _sadMaxQuality)
        {
            return ManagerEmotion.Sad;
        }

        if (!overDeadline && quality >= _satisfiedMinQuality)
        {
            return ManagerEmotion.Satisfied;
        }

        return ManagerEmotion.Dissatisfied;
    }

    public void PlayEmotionThenExit()
    {
        if (_emotionRoutine != null)
        {
            return;
        }

        ManagerEmotion emotion = _hasPendingEmotion ? _pendingEmotion : ManagerEmotion.Dissatisfied;
        _hasPendingEmotion = false;
        _emotionRoutine = StartCoroutine(PlayEmotionThenExitRoutine(emotion));
    }

    private IEnumerator PlayEmotionThenExitRoutine(ManagerEmotion emotion)
    {
        ShowEmotion(emotion);

        float seconds = Mathf.Max(0f, _emotionDisplaySeconds);
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        HideEmotion();

        // Let the manager walk out (animator running) then disappear.
        GetComponent<InteractiveObj>().enabled = false;
        GetComponent<Animator>().speed = 1;
        Destroy(gameObject, Mathf.Max(0.1f, DestroyTime));
        _emotionRoutine = null;
    }

    private void ShowEmotion(ManagerEmotion emotion)
    {
        if (_emotionRenderer == null)
        {
            return;
        }

        Sprite sprite = emotion switch
        {
            ManagerEmotion.Satisfied => _emotionSatisfiedSprite,
            ManagerEmotion.Sad => _emotionSadSprite,
            _ => _emotionDissatisfiedSprite
        };

        _emotionRenderer.sprite = sprite;
        _emotionRenderer.gameObject.SetActive(sprite != null);
    }

    private void HideEmotion()
    {
        if (_emotionRenderer == null)
        {
            return;
        }

        _emotionRenderer.gameObject.SetActive(false);
    }

    private void ConfigureHoverNameLabels()
    {
        _hoverInfoDisplay = GetComponent<CharacterNameHoverDisplay>();
        if (_hoverInfoDisplay == null)
        {
            _hoverInfoDisplay = gameObject.AddComponent<CharacterNameHoverDisplay>();
        }

        _managerInfoPortraitObjects = GetManagerInfoPortraitObjects();
        _hoverInfoDisplay.ConfigureInfo(null);
        _hoverInfoDisplay.SetMouseHoverEnabled(false);
        SetManagerInfoObjectsVisible(false);
    }

    private void ConfigureHoverSpeakBalloon()
    {
        _hoverBalloonDisplay = null;
        CharacterNameHoverDisplay[] displays = GetComponents<CharacterNameHoverDisplay>();
        foreach (CharacterNameHoverDisplay display in displays)
        {
            if (display != null && display != _hoverInfoDisplay)
            {
                _hoverBalloonDisplay = display;
                break;
            }
        }

        if (_hoverBalloonDisplay == null)
        {
            _hoverBalloonDisplay = gameObject.AddComponent<CharacterNameHoverDisplay>();
        }

        _hoverBalloonDisplay.ConfigureInfo(null, taskBalloon);
        _hoverBalloonDisplay.SetMouseHoverEnabled(false);
    }

    private void UpdateManagerInfoVisibility()
    {
        if (_hoverInfoDisplay == null)
        {
            return;
        }

        bool showForProximity = !_hideInfoAfterOrderFinished && IsPlayerNearManager();
        bool orderIsActive = managerState == ManagerState.WaitingForTaskCompletion
            && _acceptedOrder != null;

        bool showManagerInfo = showForProximity || orderIsActive;
        _hoverInfoDisplay.SetForceVisible(showManagerInfo);
        SetManagerInfoObjectsVisible(showManagerInfo);

        if (_hoverBalloonDisplay != null)
        {
            _hoverBalloonDisplay.SetMouseHoverEnabled(false);
            _hoverBalloonDisplay.SetForceVisible(showManagerInfo);
        }

        bool showOrderWindow = !_hideInfoAfterOrderFinished && (showForProximity || orderIsActive);
        if (showOrderWindow && tasktext != null)
        {
            AdaptiveDialogWindow.Show(this, tasktext.text);
        }
        else
        {
            AdaptiveDialogWindow.Hide(this);
        }
    }

    private GameObject[] GetManagerInfoPortraitObjects()
    {
        List<GameObject> portraitObjects = new List<GameObject>();
        Transform info = transform.Find("Info");
        if (info == null)
        {
            return portraitObjects.ToArray();
        }

        _managerInfoRoot = info.gameObject;
        Transform[] children = info.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == null || child == info)
            {
                continue;
            }

            string childName = child.name.ToLowerInvariant();
            if (childName.Contains("avatar") || child.GetComponent<SpriteRenderer>() != null)
            {
                portraitObjects.Add(child.gameObject);
            }
        }

        return portraitObjects.ToArray();
    }

    private void SetManagerInfoObjectsVisible(bool visible)
    {
        if (_managerInfoRoot == null)
        {
            Transform info = transform.Find("Info");
            if (info != null)
            {
                _managerInfoRoot = info.gameObject;
            }
        }

        if (_managerInfoRoot != null)
        {
            _managerInfoRoot.SetActive(visible);
        }

        if (Nametext != null)
        {
            Nametext.gameObject.SetActive(visible);
        }

        if (Companytext != null)
        {
            Companytext.gameObject.SetActive(visible);
        }

        if (_managerInfoPortraitObjects == null)
        {
            _managerInfoPortraitObjects = GetManagerInfoPortraitObjects();
        }

        foreach (GameObject portraitObject in _managerInfoPortraitObjects)
        {
            if (portraitObject != null)
            {
                portraitObject.SetActive(visible);
            }
        }
    }

    private bool IsPlayerNearManager()
    {
        DeveloperSimulation developer = FindFirstObjectByType<DeveloperSimulation>();
        if (developer == null)
        {
            return false;
        }

        float maxDistance = Mathf.Max(0.1f, _infoAppearDistance);
        Vector2 playerPosition = developer.transform.position;
        Collider2D[] managerColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D managerCollider in managerColliders)
        {
            if (managerCollider == null
                || !managerCollider.enabled
                || !managerCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector2 closestPoint = managerCollider.ClosestPoint(playerPosition);
            if (Vector2.Distance(playerPosition, closestPoint) <= maxDistance)
            {
                return true;
            }
        }

        return Vector2.Distance(playerPosition, transform.position) <= maxDistance;
    }

    private void HideSpeakBalloon()
    {
        if (taskBalloon != null)
        {
            taskBalloon.SetActive(false);
            return;
        }

        Transform speakBalloon = transform.Find("speak_balloon");
        if (speakBalloon != null)
        {
            speakBalloon.gameObject.SetActive(false);
        }
    }

    private void HideOrderTextInSpeakBalloon()
    {
        if (tasktext == null)
        {
            return;
        }

        if (tasktext == Nametext || tasktext == Companytext)
        {
            return;
        }

        tasktext.gameObject.SetActive(false);
    }
}

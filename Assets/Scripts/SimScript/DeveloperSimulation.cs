
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
public struct TaskProgress
{
    public ActiveOrder order;
    public Milestone[] milestones;
}
[Serializable]
public struct DevInfo
{
    public float money;
    public float experience;
    public int level;
    public int taskCompleted;
    public int perfectQualityOrdersCompleted;
    public int highQualityOrdersCompleted;
}
public class DeveloperSimulation : MonoBehaviour
{
    public static event Action<DeveloperSimulation, ActiveOrder> OrderReceived;
    public static event Action<DeveloperSimulation, Milestone> MilestoneCompleted;
    public static event Action<DeveloperSimulation, ActiveOrder> OrderReadyToSubmit;
    public static event Action<DeveloperSimulation, ActiveOrder> OrderSubmitted;
    public static event Action<DeveloperSimulation, ActiveOrder, float, bool> OrderSubmittedDetailed;

    public TaskProgress currentTask;
    public TextMeshPro tasktext;
    public TextMeshProUGUI cointext;
    private InteractiveObj targetedInteractiveObj;
    public GameObject InteractText;
    public GameObject USBIcon;
    public PlayerControls controls;
    public bool scoreBoost = false;
    public bool moneyboost = false;
    public DevInfo devInfo;

    [Header("Money Gain Sound")]
    [SerializeField] private AudioClip moneyGainSound;
    [SerializeField] private AudioSource moneyGainAudioSource;
    [SerializeField] [Range(0f, 1f)] private float moneyGainVolume = 0.8f;
    [SerializeField] private Vector2 moneyGainPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Money Loss Sound")]
    [SerializeField] private AudioClip moneyLossSound;
    [SerializeField] private AudioSource moneyLossAudioSource;
    [SerializeField] [Range(0f, 1f)] private float moneyLossVolume = 0.8f;
    [SerializeField] private Vector2 moneyLossPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Resource Menu Underlay")]
    [SerializeField] private Image resourceMenuUnderlay;
    [SerializeField] private Color resourceMenuUnderlayColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Vector2 resourceMenuUnderlaySize = new Vector2(260f, 170f);
    [SerializeField] private Vector2 resourceMenuUnderlayAnchoredPosition = new Vector2(95f, -85f);
    [SerializeField] private bool createResourceMenuUnderlay = true;

    private const float BaseExperienceRequirement = 700f;
    private const float ExperienceRequirementGrowth = 2.0f;
    private const float MinTaxRate = 0.05f;
    private const float MaxTaxRate = 0.10f;
    private const float PerfectQualityThreshold = 0.999f;
    private const float HighQualityThreshold = 0.9f;
    private Transform developerSpeakBalloon;
    private Quaternion developerSpeakBalloonOriginalRotation;
    private bool hasDeveloperSpeakBalloonOriginalRotation;
    private bool progressWindowVisible;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentTask = new TaskProgress();
        EnsureResourceMenuUnderlay();
        CacheDeveloperSpeakBalloonVisualAngle();
        HideSpeakBalloon();
    }
    public void puchase()
    {
        if (currentTask.order == null || currentTask.milestones == null || currentTask.milestones.Length == 0)
        {
            return;
        }

        ActiveOrder completedOrder = currentTask.order;
        float totalScore = 0;
        foreach (Milestone m in currentTask.milestones)
        {
            totalScore += m.progress;
        }
        totalScore /= currentTask.milestones.Length;
        float multiplier = 1;
        ManagerSimulation manager = FindFirstObjectByType<ManagerSimulation>();
        bool overDeadline = manager != null && manager.isOveredDeadline();
        if (overDeadline)
        {
            multiplier = 0.8f;
        }
        float reward = currentTask.order.budget * totalScore * multiplier;
        if (currentTask.order.paymentCap > 0)
        {
            reward = Mathf.Min(reward, currentTask.order.paymentCap);
        }

        float paidReward = completedOrder.isTutorialOrder && completedOrder.guaranteedReward > 0
            ? completedOrder.guaranteedReward
            : ApplyIncomeTax(reward);

        AddMoney(paidReward);
        devInfo.experience += completedOrder.isTutorialOrder ? paidReward : reward;
        RegisterCompletedOrderQuality(totalScore);
        currentTask = new TaskProgress();
        USBIcon.SetActive(false);
        levelCheck();
        if (completedOrder.isTutorialOrder)
        {
            PlayerPrefs.SetInt(TutorialController.TutorialCompleteKey, 1);
            PlayerPrefs.SetInt(TutorialController.TutorialStepKey, (int)TutorialStep.Done);
            PlayerPrefs.Save();
        }
        OrderSubmitted?.Invoke(this, completedOrder);
        OrderSubmittedDetailed?.Invoke(this, completedOrder, totalScore, overDeadline);
        MinigameEventSelectionController.CompleteOrder(completedOrder);
    }

    private void RegisterCompletedOrderQuality(float quality)
    {
        devInfo.taskCompleted += 1;
        if (quality >= PerfectQualityThreshold)
        {
            devInfo.perfectQualityOrdersCompleted += 1;
        }
        if (quality >= HighQualityThreshold)
        {
            devInfo.highQualityOrdersCompleted += 1;
        }
    }

    private void levelCheck()
    {
        while (devInfo.experience > GetExperienceRequiredForLevel(devInfo.level))
        {
            devInfo.experience -= GetExperienceRequiredForLevel(devInfo.level);
            devInfo.level += 1;
        }
    }

    public void AddExperience(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        devInfo.experience += amount;
        levelCheck();
    }

    public void AddMoney(float amount)
    {
        devInfo.money += amount;
        if (amount <= 0f || moneyGainSound == null || moneyGainAudioSource == null)
        {
            return;
        }

        float minimumPitch = Mathf.Min(moneyGainPitchRange.x, moneyGainPitchRange.y);
        float maximumPitch = Mathf.Max(moneyGainPitchRange.x, moneyGainPitchRange.y);
        moneyGainAudioSource.pitch = Random.Range(minimumPitch, maximumPitch);
        moneyGainAudioSource.PlayOneShot(moneyGainSound, moneyGainVolume);
    }

    public void RemoveMoney(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        devInfo.money -= amount;
        if (moneyLossSound == null || moneyLossAudioSource == null)
        {
            return;
        }

        float minimumPitch = Mathf.Min(moneyLossPitchRange.x, moneyLossPitchRange.y);
        float maximumPitch = Mathf.Max(moneyLossPitchRange.x, moneyLossPitchRange.y);
        moneyLossAudioSource.pitch = Random.Range(minimumPitch, maximumPitch);
        moneyLossAudioSource.PlayOneShot(moneyLossSound, moneyLossVolume);
    }

    private float GetExperienceRequiredForLevel(int level)
    {
        return BaseExperienceRequirement * Mathf.Pow(ExperienceRequirementGrowth, Mathf.Max(0, level - 1));
    }

    private float ApplyIncomeTax(float income)
    {
        float taxRate = Random.Range(MinTaxRate, MaxTaxRate);
        return income * (1f - taxRate);
    }
    public void ProgressComplete(Milestone milestone, float score)
    {
        if (!HasActiveOrder() || milestone == null)
        {
            return;
        }

        float baseProgress = Mathf.Clamp01(score);
        milestone.progress = baseProgress <= 0f
            ? 0f
            : Mathf.Clamp01(baseProgress + (scoreBoost ? 0.1f : 0f));
        float requiredQuality = GetRequiredQuality();
        if (milestone.progress <= 0f || (requiredQuality > 0f && milestone.progress < requiredQuality))
        {
            milestone.isCompleted = false;
            SetupMissionForMilestone(milestone);
            return;
        }

        milestone.isCompleted = true;
        MilestoneCompleted?.Invoke(this, milestone);
        CheckCompleted();
    }
    public bool HasActiveOrder()
    {
        return currentTask.order != null
            && currentTask.milestones != null
            && currentTask.milestones.Length > 0;
    }

    public bool IsCurrentTaskCompleted()
    {
        if (!HasActiveOrder())
        {
            return false;
        }

        foreach (Milestone milestone in currentTask.milestones)
        {
            if (milestone == null || !milestone.isCompleted)
            {
                return false;
            }
        }

        return true;
    }

    public bool CompleteCurrentTask(float score, int paymentCap = 0)
    {
        if (!HasActiveOrder() || IsCurrentTaskCompleted())
        {
            return false;
        }

        currentTask.order.paymentCap = paymentCap;
        float completedScore = Mathf.Clamp01(score);
        MissionObjects[] missions = FindObjectsByType<MissionObjects>(FindObjectsSortMode.None);
        foreach (MissionObjects mission in missions)
        {
            if (mission == null || mission.milestone == null || mission.activeOrder != currentTask.order)
            {
                continue;
            }

            mission.milestone.progress = completedScore;
            mission.milestone.isCompleted = true;
            mission.GetComponent<InteractiveObj>().enabled = false;
            Destroy(mission);
        }

        foreach (Milestone milestone in currentTask.milestones)
        {
            if (milestone == null)
            {
                continue;
            }

            milestone.progress = completedScore;
            milestone.isCompleted = true;
        }

        CheckCompleted();
        return true;
    }

    private void CheckCompleted()
    {
        if (!HasActiveOrder())
        {
            return;
        }

        bool isCompleted = true;
        foreach (Milestone m in currentTask.milestones)
        {
            if (m == null || !m.isCompleted)
            {
                isCompleted = false;
                break;
            }
        }
        if (isCompleted)
        {
            ManagerSimulation manager = FindFirstObjectByType<ManagerSimulation>();
            if (manager == null)
            {
                return;
            }

            if (manager.GetComponent<PurchaseScript>() == null)
            {
                manager.AddComponent<PurchaseScript>();
            }

            manager.GetComponent<InteractiveObj>().enabled = true;
            OrderReadyToSubmit?.Invoke(this, currentTask.order);
        }
    }
    public void receiveTask(ActiveOrder order)
    {
        if (order == null)
        {
            return;
        }

        currentTask = new TaskProgress() { order = order, milestones = order.milestones };
        USBIcon.SetActive(true);
        foreach (Milestone milestone in currentTask.milestones)
        {
            milestone.progress = 0;
            milestone.isCompleted = false;
            SetupMissionForMilestone(milestone);
        }
        OrderReceived?.Invoke(this, order);
    }

    private void SetupMissionForMilestone(Milestone milestone)
    {
        GameObject station = GameObject.Find(milestone.devicetype + "_PC");
        if (station == null)
        {
            Debug.LogWarning("Missing mission station for device type: " + milestone.devicetype);
            return;
        }

        MissionObjects mission = station.GetComponent<MissionObjects>();
        if (mission == null || mission.isStarted)
        {
            mission = station.AddComponent<MissionObjects>();
        }

        mission.milestone = milestone;
        mission.activeOrder = currentTask.order;
        station.GetComponent<InteractiveObj>().enabled = true;
    }

    private float GetRequiredQuality()
    {
        if (currentTask.order == null)
        {
            return 0f;
        }

        float requiredQuality = currentTask.order.GetConditionValue(SpecialConditionType.Quality, 0f);
        return requiredQuality > 1f ? requiredQuality / 100f : requiredQuality;
    }
    private void OnEnable() { controls.Player.Enable(); }
    private void Awake()
    {
        controls = new PlayerControls();
        controls.Player.Interact.canceled += context => Interact();

        if (moneyGainAudioSource == null)
        {
            moneyGainAudioSource = gameObject.AddComponent<AudioSource>();
        }
        moneyGainAudioSource.playOnAwake = false;

        if (moneyLossAudioSource == null)
        {
            moneyLossAudioSource = gameObject.AddComponent<AudioSource>();
        }
        moneyLossAudioSource.playOnAwake = false;
    }
    private void FixedUpdate()
    {
        if (GameController.GameState == GameState.Stopped) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1);
        foreach (Collider2D hit in hits)
        {
            targetedInteractiveObj = hit.GetComponent<InteractiveObj>();
            if (targetedInteractiveObj)
            {
                break;
            }
        }
        InteractText.SetActive(targetedInteractiveObj != null && targetedInteractiveObj.enabled);

    }

    public void Cheat(int index)
    {
        if (index == -1)
        {
            if (!HasActiveOrder())
            {
                return;
            }

            MissionObjects.ForceCloseAllActive();

            MissionObjects[] missions = FindObjectsByType<MissionObjects>(FindObjectsSortMode.None);
            foreach (MissionObjects mission in missions)
            {
                if (mission.milestone == null)
                {
                    continue;
                }

                mission.milestone.progress = 0;
                mission.milestone.isCompleted = true;
                mission.GetComponent<InteractiveObj>().enabled = false;
                Destroy(mission);
            }

            foreach (Milestone milestone in currentTask.milestones)
            {
                if (milestone == null)
                {
                    continue;
                }

                milestone.progress = 0;
                milestone.isCompleted = true;
            }

            CheckCompleted();
            puchase();
        }
        if (index == 0)
        {
            CompleteCurrentTask(1f);
        }
        else if (index == 1)
        {
            AddMoney(1000);
            devInfo.experience += 1000;
            levelCheck();
        }
    }
    private void Interact()
    {
        if (!targetedInteractiveObj || targetedInteractiveObj.enabled == false)
        {
            return;
        }
        Debug.Log(targetedInteractiveObj.name);
        targetedInteractiveObj.Interact(gameObject);
    }
    void HandleCheat()
    {

        if (Input.GetKeyDown(KeyCode.Home))
        {
            Cheat(0);
        }
        if (Input.GetKeyDown(KeyCode.F12))
        {
            Cheat(1);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleProgressWindow();
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (GameController.GameState == GameState.Stopped) return;
        HandleCheat();
        UIElements();
    }
    void UIElements()
    {
        UpdateResourceMenuUnderlay();
        Transform speakBalloon = GetDeveloperSpeakBalloon();
        if (speakBalloon != null)
        {
            speakBalloon.position = transform.position + Vector3.right * 1.5f;
            PreserveDeveloperSpeakBalloonVisualAngle();
        }
        if (currentTask.milestones != null && currentTask.milestones.Length > 0)
        {
            tasktext.text = "";
            foreach (Milestone milestone in currentTask.milestones)
            {
                tasktext.text += milestone.isCompleted ? "[V]" : "[X]";
                tasktext.text += milestone.title + "  " + (milestone.progress * 100).ToString("F1") + "%\n";
            }
        }
        else
        {
            tasktext.text = "No task";
        }
        cointext.text = "      " + devInfo.money.ToString("F0");
        cointext.text += "\n      " + devInfo.experience.ToString("F0") + "/" + GetExperienceRequiredForLevel(devInfo.level).ToString("F0") + "\n      " + devInfo.level.ToString("F0");
        cointext.text += "\n      " + devInfo.taskCompleted.ToString("F0");
        cointext.text += "\n      " + devInfo.perfectQualityOrdersCompleted.ToString("F0") + "\n      " + devInfo.highQualityOrdersCompleted.ToString("F0");
        SetSpeakBalloonVisible(progressWindowVisible);
    }

    private void EnsureResourceMenuUnderlay()
    {
        if (!createResourceMenuUnderlay || cointext == null)
        {
            return;
        }

        if (resourceMenuUnderlay == null)
        {
            Transform parent = cointext.transform.parent != null
                ? cointext.transform.parent
                : cointext.transform;

            Transform existing = parent.Find("ResourceMenuUnderlay");
            if (existing != null)
            {
                resourceMenuUnderlay = existing.GetComponent<Image>();
            }

            if (resourceMenuUnderlay == null)
            {
                GameObject underlayObject = new GameObject(
                    "ResourceMenuUnderlay",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                underlayObject.transform.SetParent(parent, false);
                underlayObject.transform.SetAsFirstSibling();
                resourceMenuUnderlay = underlayObject.GetComponent<Image>();
            }
        }

        if (resourceMenuUnderlay != null)
        {
            resourceMenuUnderlay.raycastTarget = false;
        }

        UpdateResourceMenuUnderlay();
    }

    private void UpdateResourceMenuUnderlay()
    {
        if (!createResourceMenuUnderlay || resourceMenuUnderlay == null)
        {
            return;
        }

        RectTransform rect = resourceMenuUnderlay.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = resourceMenuUnderlaySize;
        rect.anchoredPosition = resourceMenuUnderlayAnchoredPosition;
        resourceMenuUnderlay.color = resourceMenuUnderlayColor;
        resourceMenuUnderlay.gameObject.SetActive(true);
        resourceMenuUnderlay.transform.SetAsFirstSibling();
    }

    private void HideSpeakBalloon()
    {
        progressWindowVisible = false;
        Transform speakBalloon = GetDeveloperSpeakBalloon();
        if (speakBalloon != null)
        {
            speakBalloon.gameObject.SetActive(false);
        }
    }

    private void ToggleProgressWindow()
    {
        progressWindowVisible = !progressWindowVisible;
        SetSpeakBalloonVisible(progressWindowVisible);
    }

    private void SetSpeakBalloonVisible(bool visible)
    {
        Transform speakBalloon = GetDeveloperSpeakBalloon();
        if (speakBalloon != null)
        {
            speakBalloon.gameObject.SetActive(false);
            PreserveDeveloperSpeakBalloonVisualAngle();
        }

        if (visible && tasktext != null)
        {
            AdaptiveDialogWindow.Show(this, tasktext.text, 5);
        }
        else
        {
            AdaptiveDialogWindow.Hide(this);
        }
    }

    private Transform GetDeveloperSpeakBalloon()
    {
        if (developerSpeakBalloon != null)
        {
            return developerSpeakBalloon;
        }

        developerSpeakBalloon = transform.Find("speak_balloon");
        if (developerSpeakBalloon == null && tasktext != null && tasktext.transform.parent != null)
        {
            developerSpeakBalloon = tasktext.transform.parent;
        }

        return developerSpeakBalloon;
    }

    private void CacheDeveloperSpeakBalloonVisualAngle()
    {
        Transform speakBalloon = GetDeveloperSpeakBalloon();
        if (speakBalloon == null)
        {
            return;
        }

        developerSpeakBalloonOriginalRotation = speakBalloon.rotation;
        hasDeveloperSpeakBalloonOriginalRotation = true;
    }

    private void PreserveDeveloperSpeakBalloonVisualAngle()
    {
        Transform speakBalloon = GetDeveloperSpeakBalloon();
        if (speakBalloon == null)
        {
            return;
        }

        if (!hasDeveloperSpeakBalloonOriginalRotation)
        {
            CacheDeveloperSpeakBalloonVisualAngle();
        }

        if (hasDeveloperSpeakBalloonOriginalRotation)
        {
            speakBalloon.rotation = developerSpeakBalloonOriginalRotation;
        }
    }

    private bool IsMouseHoveringDeveloper()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        Vector3 mouseWorldPosition = camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePoint = new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders)
        {
            if (collider != null
                && collider.enabled
                && collider.gameObject.activeInHierarchy
                && collider.OverlapPoint(mousePoint))
            {
                return true;
            }
        }

        return false;
    }
}

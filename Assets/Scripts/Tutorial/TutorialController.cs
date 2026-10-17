using UnityEngine;

public enum TutorialStep
{
    ApproachManagerDesk = 0,
    AcceptOrder = 1,
    TakeFlashDrive = 2,
    ApproachComputer = 3,
    CompleteCurrentMilestone = 4,
    ReturnToManager = 5,
    SubmitOrder = 6,
    Done = 7
}

public class TutorialController : MonoBehaviour
{
    public const string TutorialCompleteKey = "TutorialComplete";
    public const string TutorialStepKey = "TutorialStep";

    [SerializeField] private TutorialPromptUI promptUI;
    [SerializeField] private TutorialArrow arrow;
    [SerializeField] private DeveloperSimulation developer;
    [SerializeField] private Transform flashDriveTarget;
    [SerializeField] private float proximityDistance = 1.5f;
    [SerializeField] private bool pauseForExplanationPrompts = true;

    private TutorialStep currentStep;
    private ManagerSimulation manager;
    private MissionObjects currentMission;
    private Milestone currentMilestone;
    private bool isComplete;
    private bool isShowingBlockingPrompt;

    private void OnEnable()
    {
        ManagerSimulation.OrderOffered += HandleOrderOffered;
        ManagerSimulation.OrderAccepted += HandleOrderAccepted;
        DeveloperSimulation.OrderReceived += HandleOrderReceived;
        DeveloperSimulation.MilestoneCompleted += HandleMilestoneCompleted;
        DeveloperSimulation.OrderReadyToSubmit += HandleOrderReadyToSubmit;
        DeveloperSimulation.OrderSubmitted += HandleOrderSubmitted;
        MissionObjects.MissionStarted += HandleMissionStarted;
        MissionObjects.MissionEnded += HandleMissionEnded;
    }

    private void OnDisable()
    {
        ManagerSimulation.OrderOffered -= HandleOrderOffered;
        ManagerSimulation.OrderAccepted -= HandleOrderAccepted;
        DeveloperSimulation.OrderReceived -= HandleOrderReceived;
        DeveloperSimulation.MilestoneCompleted -= HandleMilestoneCompleted;
        DeveloperSimulation.OrderReadyToSubmit -= HandleOrderReadyToSubmit;
        DeveloperSimulation.OrderSubmitted -= HandleOrderSubmitted;
        MissionObjects.MissionStarted -= HandleMissionStarted;
        MissionObjects.MissionEnded -= HandleMissionEnded;
    }

    private void Start()
    {
        if (GameController.IsLoadingStoryIntro)
        {
            return;
        }

        if (PlayerPrefs.GetInt(TutorialCompleteKey, 0) == 1)
        {
            CompleteTutorial(false);
            return;
        }

        developer = developer != null ? developer : FindFirstObjectByType<DeveloperSimulation>();
        currentStep = GetValidSavedStep();
        ShowStep(currentStep);
    }

    private void Update()
    {
        if (isComplete || developer == null)
        {
            return;
        }

        manager = manager != null ? manager : FindFirstObjectByType<ManagerSimulation>();

        switch (currentStep)
        {
            case TutorialStep.ApproachManagerDesk:
                UpdateApproachManagerStep();
                break;
            case TutorialStep.AcceptOrder:
                UpdateAcceptOrderStep();
                break;
            case TutorialStep.ApproachComputer:
                UpdateApproachComputerStep();
                break;
            case TutorialStep.CompleteCurrentMilestone:
                UpdateCompleteMilestoneStep();
                break;
            case TutorialStep.ReturnToManager:
                UpdateReturnToManagerStep();
                break;
            case TutorialStep.SubmitOrder:
                UpdateSubmitOrderStep();
                break;
        }
    }

    private void UpdateApproachManagerStep()
    {
        if (manager == null)
        {
            SetArrowTarget(null);
            ShowPrompt("Wait for the manager to arrive at the desk.", false);
            return;
        }

        SetArrowTarget(manager.transform);
        ShowPrompt("Approach the manager's desk.", false);
        if (IsPlayerNear(manager.transform))
        {
            AdvanceTo(TutorialStep.AcceptOrder);
        }
    }

    private void UpdateAcceptOrderStep()
    {
        if (developer.HasActiveOrder())
        {
            AdvanceTo(TutorialStep.TakeFlashDrive);
            return;
        }

        if (manager != null)
        {
            SetArrowTarget(manager.transform);
        }

        ShowPrompt("Press Interact to accept the 100-coin training order.", false);
    }

    private void UpdateApproachComputerStep()
    {
        ShowApproachComputerPrompt();
    }

    private void UpdateCompleteMilestoneStep()
    {
        if (developer.IsCurrentTaskCompleted())
        {
            AdvanceTo(TutorialStep.ReturnToManager);
            return;
        }

        if (currentMission != null && currentMission.milestone != null)
        {
            ShowPrompt("Complete the " + currentMission.milestone.title + " mini-game.", false);
            return;
        }

        Milestone nextMilestone = GetNextIncompleteMilestone();
        Transform target = GetStationTarget(nextMilestone);
        SetArrowTarget(target);
        if (nextMilestone != null)
        {
            ShowPrompt("Go to the next computer and complete " + nextMilestone.title + ".", false);
        }
    }

    private void UpdateReturnToManagerStep()
    {
        if (manager == null)
        {
            return;
        }

        SetArrowTarget(manager.transform);
        ShowPrompt("The order is complete. Return to the manager.", false);
        if (manager.GetComponent<PurchaseScript>() != null && IsPlayerNear(manager.transform))
        {
            AdvanceTo(TutorialStep.SubmitOrder);
        }
    }

    private void UpdateSubmitOrderStep()
    {
        if (!developer.HasActiveOrder())
        {
            CompleteTutorial(true);
            return;
        }

        if (manager != null)
        {
            SetArrowTarget(manager.transform);
        }

        ShowPrompt("Press Interact to submit the order and receive 100 coins.", false);
    }

    private void ShowStep(TutorialStep step)
    {
        currentStep = step;
        PlayerPrefs.SetInt(TutorialStepKey, (int)currentStep);
        PlayerPrefs.Save();

        if (step == TutorialStep.TakeFlashDrive)
        {
            SetArrowTarget(flashDriveTarget);
            ShowBlockingPrompt("You received the flash drive. It contains the files for your first order.", TutorialStep.ApproachComputer);
        }
        else if (step == TutorialStep.ApproachComputer)
        {
            ShowApproachComputerPrompt();
        }
        else if (step == TutorialStep.Done)
        {
            CompleteTutorial(true);
        }
    }

    private void AdvanceTo(TutorialStep step)
    {
        ClearBlockingPrompt();
        ShowStep(step);
    }

    private void ShowPrompt(string message, bool showNextButton)
    {
        if (isShowingBlockingPrompt || promptUI == null)
        {
            return;
        }

        promptUI.Show(message, showNextButton);
    }

    private void ShowBlockingPrompt(string message, TutorialStep nextStep)
    {
        isShowingBlockingPrompt = true;
        if (pauseForExplanationPrompts)
        {
            GameController.GameState = GameState.Stopped;
        }

        if (promptUI == null)
        {
            AdvanceTo(nextStep);
            return;
        }

        promptUI.Show(message, true, () => AdvanceTo(nextStep));
    }

    private void ClearBlockingPrompt()
    {
        if (!isShowingBlockingPrompt)
        {
            return;
        }

        isShowingBlockingPrompt = false;
        if (pauseForExplanationPrompts)
        {
            GameController.GameState = GameState.Running;
        }
    }

    private void ShowApproachComputerPrompt()
    {
        if (!developer.HasActiveOrder())
        {
            SetArrowTarget(null);
            AdvanceTo(TutorialStep.ApproachManagerDesk);
            return;
        }

        currentMilestone = GetNextIncompleteMilestone();
        Transform target = GetStationTarget(currentMilestone);
        SetArrowTarget(target);

        if (currentMilestone == null)
        {
            ShowPrompt("The order milestones are complete. Return to the manager.", false);
            AdvanceTo(TutorialStep.ReturnToManager);
            return;
        }

        string title = string.IsNullOrWhiteSpace(currentMilestone.title) ? "task" : currentMilestone.title;
        string deviceName = string.IsNullOrWhiteSpace(currentMilestone.devicetype)
            ? "the highlighted computer"
            : currentMilestone.devicetype + "_PC";

        if (target == null)
        {
            ShowPrompt("Cannot find " + deviceName + ". Check that the PC object name matches the milestone device type.", false);
            return;
        }

        ShowPrompt("Approach " + deviceName + " and press Interact to start the " + title + " mini-game.", false);
    }

    private TutorialStep GetValidSavedStep()
    {
        bool hasSavedStep = PlayerPrefs.HasKey(TutorialStepKey);
        TutorialStep savedStep = (TutorialStep)PlayerPrefs.GetInt(TutorialStepKey, (int)TutorialStep.ApproachManagerDesk);
        if (ShouldCompleteSavedTutorial(hasSavedStep, savedStep))
        {
            PlayerPrefs.SetInt(TutorialCompleteKey, 1);
            PlayerPrefs.SetInt(TutorialStepKey, (int)TutorialStep.Done);
            PlayerPrefs.Save();
            return TutorialStep.Done;
        }

        return savedStep;
    }

    private bool ShouldCompleteSavedTutorial(bool hasSavedStep, TutorialStep savedStep)
    {
        if (!hasSavedStep)
        {
            return false;
        }

        return savedStep == TutorialStep.Done
            || developer == null
            || !developer.HasActiveOrder();
    }

    private void CompleteTutorial(bool save)
    {
        isComplete = true;
        currentStep = TutorialStep.Done;
        ClearBlockingPrompt();

        if (promptUI != null)
        {
            promptUI.Hide();
        }

        if (arrow != null)
        {
            arrow.Hide();
        }

        if (save)
        {
            PlayerPrefs.SetInt(TutorialCompleteKey, 1);
            PlayerPrefs.SetInt(TutorialStepKey, (int)TutorialStep.Done);
            PlayerPrefs.Save();
        }
    }

    private void HandleOrderOffered(ManagerSimulation offeredManager, ActiveOrder order)
    {
        if (isComplete || currentStep != TutorialStep.ApproachManagerDesk)
        {
            return;
        }

        manager = offeredManager;
    }

    private void HandleOrderAccepted(ManagerSimulation acceptedManager, ActiveOrder order, DeveloperSimulation acceptedDeveloper)
    {
        if (isComplete || acceptedDeveloper != developer)
        {
            return;
        }

        manager = acceptedManager;
        if (currentStep == TutorialStep.AcceptOrder)
        {
            AdvanceTo(TutorialStep.TakeFlashDrive);
        }
    }

    private void HandleOrderReceived(DeveloperSimulation receivingDeveloper, ActiveOrder order)
    {
        if (isComplete || receivingDeveloper != developer)
        {
            return;
        }

        if (currentStep == TutorialStep.AcceptOrder)
        {
            AdvanceTo(TutorialStep.TakeFlashDrive);
        }
    }

    private void HandleMissionStarted(MissionObjects mission, GameObject player)
    {
        if (isComplete || player == null || player.GetComponent<DeveloperSimulation>() != developer)
        {
            return;
        }

        currentMission = mission;
        currentMilestone = mission.milestone;
        if (currentStep == TutorialStep.ApproachComputer)
        {
            AdvanceTo(TutorialStep.CompleteCurrentMilestone);
        }
    }

    private void HandleMissionEnded(MissionObjects mission, float score)
    {
        if (currentMission == mission)
        {
            currentMission = null;
        }
    }

    private void HandleMilestoneCompleted(DeveloperSimulation completingDeveloper, Milestone milestone)
    {
        if (isComplete || completingDeveloper != developer)
        {
            return;
        }

        currentMilestone = milestone;
        if (developer.IsCurrentTaskCompleted())
        {
            AdvanceTo(TutorialStep.ReturnToManager);
        }
        else if (currentStep == TutorialStep.CompleteCurrentMilestone)
        {
            AdvanceTo(TutorialStep.ApproachComputer);
        }
    }

    private void HandleOrderReadyToSubmit(DeveloperSimulation readyDeveloper, ActiveOrder order)
    {
        if (isComplete || readyDeveloper != developer)
        {
            return;
        }

        if (currentStep == TutorialStep.CompleteCurrentMilestone || currentStep == TutorialStep.ApproachComputer)
        {
            AdvanceTo(TutorialStep.ReturnToManager);
        }
    }

    private void HandleOrderSubmitted(DeveloperSimulation submittingDeveloper, ActiveOrder order)
    {
        if (isComplete || submittingDeveloper != developer)
        {
            return;
        }

        CompleteTutorial(true);
    }

    private Milestone GetNextIncompleteMilestone()
    {
        if (!developer.HasActiveOrder())
        {
            return null;
        }

        foreach (Milestone milestone in developer.currentTask.milestones)
        {
            if (milestone != null && !milestone.isCompleted)
            {
                return milestone;
            }
        }

        return null;
    }

    private Transform GetStationTarget(Milestone milestone)
    {
        if (milestone == null || string.IsNullOrEmpty(milestone.devicetype))
        {
            return null;
        }

        GameObject station = GameObject.Find(milestone.devicetype + "_PC");
        return station != null ? station.transform : null;
    }

    private bool IsPlayerNear(Transform target)
    {
        return target != null
            && Vector3.Distance(developer.transform.position, target.position) <= proximityDistance;
    }

    private void SetArrowTarget(Transform target)
    {
        if (arrow == null)
        {
            return;
        }

        arrow.PointTo(target);
    }
}

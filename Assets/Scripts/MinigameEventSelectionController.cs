using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class MinigameSelectionStartEvent : UnityEvent<int, int, int>
{
}

public class MinigameEventSelectionController : MonoBehaviour
{
    public static event Action<ActiveOrder> HideoEventTaskStarted;

    public const string ConceptEventKey = "SelectedConceptEvent";
    public const string DesignEventKey = "SelectedDesignEvent";
    public const string ProgrammingEventKey = "SelectedProgrammingEvent";
    private const string HideoEventTaskPendingKey = "HideoEventTaskPending";
    private const string HideoEventTaskActiveKey = "HideoEventTaskActive";
    private const string HideoCompletionNpcUnlockedKey = "HideoCompletionNpcUnlocked";
    private const string HideoCompletionNpcAppearanceKey = "HideoCompletionNpcAppearance";

    [Header("Event Groups")]
    [SerializeField] private ExclusiveButtonGroup _conceptEventGroup;
    [SerializeField] private ExclusiveButtonGroup _designEventGroup;
    [SerializeField] private ExclusiveButtonGroup _programmingEventGroup;

    [Header("Start")]
    [SerializeField] private Button _startButton;
    [Tooltip("Office scene where the player can talk to Hideo K after confirming these choices.")]
    [SerializeField] private string _officeSceneName;
    [SerializeField] private TMP_Text _validationText;
    [SerializeField] private MinigameSelectionStartEvent _onStartRequested;

    public static int SelectedConceptEvent => PlayerPrefs.GetInt(ConceptEventKey, -1);
    public static int SelectedDesignEvent => PlayerPrefs.GetInt(DesignEventKey, -1);
    public static int SelectedProgrammingEvent => PlayerPrefs.GetInt(ProgrammingEventKey, -1);
    public static bool IsHideoEventTaskPending => PlayerPrefs.GetInt(HideoEventTaskPendingKey, 0) == 1;
    public static bool IsHideoEventTaskActive => PlayerPrefs.GetInt(HideoEventTaskActiveKey, 0) == 1;
    public static bool IsMirrorScreenSelected => SelectedConceptEvent == 0
        || SelectedDesignEvent == 0
        || SelectedProgrammingEvent == 1;
    public static bool IsConceptMirrorScreenSelected => IsMirrorScreenSelected;
    public static bool IsDesignMirrorScreenSelected => IsMirrorScreenSelected;
    public static bool IsProgrammingMirrorScreenSelected => IsMirrorScreenSelected;
    public static bool IsProgrammingChaoticBugsSelected => SelectedProgrammingEvent == 0;
    public static bool IsProgrammingCreepingBugsSelected => SelectedProgrammingEvent == 2;
    public static bool IsProgrammingInvertColorsSelected => SelectedProgrammingEvent == 3;
    public static bool IsOldAgeSelected => SelectedConceptEvent == 1
        || SelectedDesignEvent == 1
        || SelectedProgrammingEvent == 4;
    public static bool IsProgrammingOldAgeSelected => IsOldAgeSelected;
    public static bool IsProgrammingSimultaneousBugsSelected => SelectedProgrammingEvent == 5;
    public static bool IsConceptOldAgeSelected => IsOldAgeSelected;
    public static bool IsDesignOldAgeSelected => IsOldAgeSelected;

    private void OnEnable()
    {
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(StartMinigames);
        }
    }

    private void OnDisable()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(StartMinigames);
        }
    }

    public void StartMinigames()
    {
        GetSelections(out int concept, out int design, out int programming);

        PlayerPrefs.SetInt(ConceptEventKey, concept);
        PlayerPrefs.SetInt(DesignEventKey, design);
        PlayerPrefs.SetInt(ProgrammingEventKey, programming);
        PlayerPrefs.SetInt(HideoEventTaskPendingKey, 0);
        PlayerPrefs.SetInt(HideoEventTaskActiveKey, 1);
        ResetManagerProgression();
        PlayerPrefs.Save();

        SetOldAgeUpgradeSuppression(IsOldAgeSelected);
        ShowValidationMessage(string.Empty);
        _onStartRequested?.Invoke(concept, design, programming);

        if (string.IsNullOrWhiteSpace(_officeSceneName))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(_officeSceneName))
        {
            ShowValidationMessage("Scene '" + _officeSceneName + "' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(_officeSceneName);
    }

    public static void ActivateForHideoTask(ActiveOrder order)
    {
        if (order == null || order.manager != ManagerId.HideoK || !IsHideoEventTaskPending)
        {
            return;
        }

        PlayerPrefs.SetInt(HideoEventTaskPendingKey, 0);
        PlayerPrefs.SetInt(HideoEventTaskActiveKey, 1);
        PlayerPrefs.Save();
        SetOldAgeUpgradeSuppression(IsOldAgeSelected);
        HideoEventTaskStarted?.Invoke(order);
    }

    public static void CompleteOrder(ActiveOrder order)
    {
        if (order != null
            && order.manager == ManagerId.HideoK
            && IsHideoEventTaskActive)
        {
            ResetAllEvents();
        }
    }

    private static void ResetManagerProgression()
    {
        PlayerPrefs.SetInt("Level", 1);
        PlayerPrefs.SetInt("Experience", 0);
        PlayerPrefs.SetInt("TaskCompleted", 0);
        PlayerPrefs.SetInt("PerfectQualityOrdersCompleted", 0);
        PlayerPrefs.SetInt("HighQualityOrdersCompleted", 0);
        PlayerPrefs.DeleteKey(HideoCompletionNpcUnlockedKey);
        PlayerPrefs.DeleteKey(HideoCompletionNpcAppearanceKey);
    }

    public static void ResetAllEvents()
    {
        SetOldAgeUpgradeSuppression(false);
        PlayerPrefs.DeleteKey(ConceptEventKey);
        PlayerPrefs.DeleteKey(DesignEventKey);
        PlayerPrefs.DeleteKey(ProgrammingEventKey);
        PlayerPrefs.DeleteKey(HideoEventTaskPendingKey);
        PlayerPrefs.DeleteKey(HideoEventTaskActiveKey);
        PlayerPrefs.Save();
    }

    private static void SetOldAgeUpgradeSuppression(bool suppressed)
    {
        GameController gameController = FindFirstObjectByType<GameController>();
        if (gameController != null)
        {
            gameController.SetOfficeUpgradesSuppressed(suppressed);
        }
    }

    private void GetSelections(out int concept, out int design, out int programming)
    {
        concept = _conceptEventGroup != null ? _conceptEventGroup.SelectedIndex : -1;
        design = _designEventGroup != null ? _designEventGroup.SelectedIndex : -1;
        programming = _programmingEventGroup != null ? _programmingEventGroup.SelectedIndex : -1;
    }

    private void ShowValidationMessage(string message)
    {
        if (_validationText == null)
        {
            return;
        }

        _validationText.text = message;
        _validationText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}

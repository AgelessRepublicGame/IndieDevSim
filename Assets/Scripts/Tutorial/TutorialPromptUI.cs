using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Button nextButton;

    private System.Action nextAction;
    private CanvasGroup panelCanvasGroup;

    private void Awake()
    {
        EnsureCanvasGroup();
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(HandleNextClicked);
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(HandleNextClicked);
        }
    }

    public void Show(string message, bool showNextButton, System.Action onNext = null)
    {
        EnsureCanvasGroup();
        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.text = message;
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(showNextButton);
        }

        SetBlocksRaycasts(showNextButton);
        nextAction = onNext;
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        nextAction = null;
        SetBlocksRaycasts(false);
    }

    private void HandleNextClicked()
    {
        System.Action action = nextAction;
        nextAction = null;
        action?.Invoke();
    }

    private void EnsureCanvasGroup()
    {
        if (panelCanvasGroup != null || panel == null)
        {
            return;
        }

        panelCanvasGroup = panel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = panel.AddComponent<CanvasGroup>();
        }
    }

    private void SetBlocksRaycasts(bool blocksRaycasts)
    {
        if (panelCanvasGroup == null)
        {
            return;
        }

        panelCanvasGroup.blocksRaycasts = blocksRaycasts;
        panelCanvasGroup.interactable = blocksRaycasts;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExclusiveButtonGroup : MonoBehaviour
{
    [Header("Group Buttons")]
    [SerializeField] private Button[] _buttons;

    [Header("Selected Badge")]
    [SerializeField] private Sprite _badgeSprite;
    [SerializeField] private Vector2 _badgeSize = new Vector2(32f, 32f);
    [SerializeField] private Vector2 _badgeOffset = Vector2.zero;

    [Header("Hover Description")]
    [Tooltip("Descriptions must use the same order as the Buttons array.")]
    [SerializeField] [TextArea] private string[] _buttonDescriptions;
    [SerializeField] private GameObject _descriptionPanel;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("Selection Event")]
    [SerializeField] private UnityEvent<int> _onSelectionChanged;

    private readonly List<Image> _badges = new List<Image>();
    private int _selectedIndex = -1;

    public int SelectedIndex => _selectedIndex;

    private void Awake()
    {
        SetupButtons();
        HideDescription();
    }

    private void OnDisable()
    {
        HideDescription();
    }

    private void SetupButtons()
    {
        _badges.Clear();

        if (_buttons == null)
        {
            return;
        }

        for (int i = 0; i < _buttons.Length; i++)
        {
            Button button = _buttons[i];
            if (button == null)
            {
                _badges.Add(null);
                continue;
            }

            int buttonIndex = i;
            button.onClick.AddListener(() => Select(buttonIndex));
            AddHoverEvents(button.gameObject, buttonIndex);
            _badges.Add(CreateBadge(button.transform));
        }
    }

    public void Select(int index)
    {
        if (_buttons == null
            || index < 0
            || index >= _buttons.Length
            || _buttons[index] == null)
        {
            return;
        }

        _selectedIndex = index;
        RefreshVisuals();
        _onSelectionChanged?.Invoke(index);
    }

    public void ResetSelection()
    {
        _selectedIndex = -1;
        RefreshVisuals();
    }

    public void ShowDescription(int index)
    {
        if (_descriptionPanel == null
            || _descriptionText == null
            || _buttonDescriptions == null
            || index < 0
            || index >= _buttonDescriptions.Length
            || string.IsNullOrWhiteSpace(_buttonDescriptions[index]))
        {
            HideDescription();
            return;
        }

        _descriptionText.text = _buttonDescriptions[index];
        _descriptionPanel.SetActive(true);
    }

    public void HideDescription()
    {
        if (_descriptionPanel != null)
        {
            _descriptionPanel.SetActive(false);
        }
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            if (i < _badges.Count && _badges[i] != null)
            {
                _badges[i].gameObject.SetActive(i == _selectedIndex);
            }

        }
    }

    private Image CreateBadge(Transform buttonTransform)
    {
        GameObject badgeObject = new GameObject("Selected Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObject.transform.SetParent(buttonTransform, false);

        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = Vector2.one;
        badgeRect.anchorMax = Vector2.one;
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.anchoredPosition = _badgeOffset;
        badgeRect.sizeDelta = _badgeSize;

        Image badge = badgeObject.GetComponent<Image>();
        badge.sprite = _badgeSprite;
        badge.preserveAspect = true;
        badge.raycastTarget = false;
        badgeObject.SetActive(false);
        return badge;
    }

    private void AddHoverEvents(GameObject buttonObject, int buttonIndex)
    {
        EventTrigger eventTrigger = buttonObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = buttonObject.AddComponent<EventTrigger>();
        }
        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener(_ => ShowDescription(buttonIndex));
        eventTrigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener(_ => HideDescription());
        eventTrigger.triggers.Add(exitEntry);
    }
}

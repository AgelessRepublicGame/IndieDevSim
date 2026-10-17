using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(InteractiveObj))]
public class SceneChatNpc : MonoBehaviour
{
    [Header("NPC Description")]
    [SerializeField] private string _npcName = "Manager";
    [SerializeField] private string _description = "Talk to me";
    [SerializeField] private TextMeshPro _nameText;
    [SerializeField] private TextMeshPro _descriptionText;
    [SerializeField] [Min(0.1f)] private float _descriptionFontSize = 3f;
    [SerializeField] private Vector3 _descriptionLocalPosition = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private bool _showDescription = true;

    [Header("Chat Destination")]
    [SerializeField] private string _destinationSceneName;

    private InteractiveObj _interactiveObj;
    private Transform _managerInfo;
    private SpriteRenderer _managerAvatar;

    private void Awake()
    {
        _interactiveObj = GetComponent<InteractiveObj>();
        EnsureCollider();
        FindManagerStyleTextReferences();
        RefreshDescription();
        ConfigureHoverNameLabel();
    }

    private void OnEnable()
    {
        if (_interactiveObj == null)
        {
            _interactiveObj = GetComponent<InteractiveObj>();
        }

        _interactiveObj.OnInteractionAccept += Chat;
    }

    private void OnDisable()
    {
        if (_interactiveObj != null)
        {
            _interactiveObj.OnInteractionAccept -= Chat;
        }
    }

    public void Configure(string npcName, string description, string destinationSceneName)
    {
        _npcName = npcName;
        _description = description;
        SetDestinationScene(destinationSceneName);
        FindManagerStyleTextReferences();
        RefreshDescription();
        ConfigureHoverNameLabel();
    }

    public void SetDestinationScene(string sceneName)
    {
        _destinationSceneName = sceneName;
    }

    public void SetDescriptionFontSize(float fontSize)
    {
        _descriptionFontSize = Mathf.Max(0.1f, fontSize);
        FindManagerStyleTextReferences();
        if (_descriptionText != null)
        {
            _descriptionText.fontSize = _descriptionFontSize;
        }
    }

    public void SetDescriptionPosition(Vector3 localPosition)
    {
        _descriptionLocalPosition = localPosition;
        FindManagerStyleTextReferences();
        if (_descriptionText != null)
        {
            _descriptionText.transform.localPosition = _descriptionLocalPosition;
        }
    }

    public void UseManagerStyleWithoutDescription()
    {
        _showDescription = false;
        EnsureManagerStyleHierarchy();
        FindManagerStyleTextReferences();

        if (_nameText != null)
        {
            _nameText.gameObject.SetActive(false);
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = string.Empty;
            _descriptionText.gameObject.SetActive(false);
        }
    }

    public void SetManagerPortrait(Sprite portrait)
    {
        EnsureManagerStyleHierarchy();
        if (_managerAvatar != null)
        {
            _managerAvatar.sprite = portrait;
            _managerAvatar.enabled = portrait != null;
            _managerAvatar.gameObject.SetActive(portrait != null);
        }

        RefreshCollider();
    }

    public void SetManagerPortraitLayout(Vector3 localPosition, Vector3 localScale)
    {
        EnsureManagerStyleHierarchy();
        if (_managerInfo != null)
        {
            _managerInfo.localPosition = localPosition;
            _managerInfo.gameObject.SetActive(true);
        }

        if (_managerAvatar != null)
        {
            _managerAvatar.transform.localPosition = Vector3.zero;
            _managerAvatar.transform.localScale = localScale;
            _managerAvatar.gameObject.SetActive(true);
        }

        RefreshCollider();
    }

    private void Chat(GameObject sender)
    {
        if (string.IsNullOrWhiteSpace(_destinationSceneName))
        {
            Debug.LogWarning(name + " has no destination scene configured.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(_destinationSceneName))
        {
            Debug.LogError("Cannot load NPC chat scene '" + _destinationSceneName
                + "'. Add it to the Build Settings scene list.");
            return;
        }

        SceneManager.LoadScene(_destinationSceneName);
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector3 worldSize = spriteRenderer.bounds.size;
            Vector3 scale = transform.lossyScale;
            boxCollider.size = new Vector2(
                scale.x == 0f ? worldSize.x : worldSize.x / Mathf.Abs(scale.x),
                scale.y == 0f ? worldSize.y : worldSize.y / Mathf.Abs(scale.y));
        }
    }

    private void RefreshCollider()
    {
        Collider2D existingCollider = GetComponent<Collider2D>();
        BoxCollider2D boxCollider = existingCollider as BoxCollider2D;
        if (boxCollider == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = _managerAvatar != null
            ? _managerAvatar
            : GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        boxCollider.size = spriteRenderer.sprite.bounds.size;
        boxCollider.offset = spriteRenderer.transform.localPosition;
    }

    private void EnsureManagerStyleHierarchy()
    {
        DisableLegacyLabel("NameText");
        DisableLegacyLabel("DescriptionText");

        Transform speakBalloon = FindOrCreateChild(transform, "speak_balloon");
        speakBalloon.gameObject.SetActive(false);
        Transform speechTextObject = FindOrCreateChild(speakBalloon, "Text (TMP)");
        EnsureWorldText(speechTextObject, Vector3.zero);

        _managerInfo = FindOrCreateChild(transform, "Info");
        _managerInfo.gameObject.SetActive(true);
        Transform nameObject = FindOrCreateChild(_managerInfo, "Name");
        Transform companyObject = FindOrCreateChild(_managerInfo, "Company");
        Transform avatarObject = FindAvatarChild(_managerInfo)
            ?? FindOrCreateChild(_managerInfo, "avatars_NPC");

        _nameText = EnsureWorldText(nameObject, new Vector3(0f, 1.7f, 0f));
        _descriptionText = EnsureWorldText(companyObject, new Vector3(0f, 1.2f, 0f));
        companyObject.gameObject.SetActive(false);

        _managerAvatar = avatarObject.GetComponent<SpriteRenderer>();
        if (_managerAvatar == null)
        {
            _managerAvatar = avatarObject.gameObject.AddComponent<SpriteRenderer>();
        }
        avatarObject.gameObject.SetActive(true);

        SpriteRenderer oldRenderer = FindNonManagerAvatarRenderer();
        if (oldRenderer != null)
        {
            _managerAvatar.sprite = oldRenderer.sprite;
            _managerAvatar.sortingLayerID = oldRenderer.sortingLayerID;
            _managerAvatar.sortingOrder = oldRenderer.sortingOrder;
            oldRenderer.enabled = false;
        }

        _nameText.renderer.sortingLayerID = _managerAvatar.sortingLayerID;
        _nameText.renderer.sortingOrder = _managerAvatar.sortingOrder + 1;

        Transform emotion = FindOrCreateChild(transform, "bubbleEmotion");
        emotion.gameObject.SetActive(false);
    }

    private void DisableLegacyLabel(string labelName)
    {
        Transform legacyLabel = transform.Find(labelName);
        if (legacyLabel != null)
        {
            legacyLabel.gameObject.SetActive(false);
        }
    }

    private SpriteRenderer FindNonManagerAvatarRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer != _managerAvatar)
            {
                return renderer;
            }
        }

        return null;
    }

    private static Transform FindAvatarChild(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLowerInvariant().Contains("avatar"))
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private TextMeshPro EnsureWorldText(Transform textTransform, Vector3 localPosition)
    {
        TextMeshPro text = textTransform.GetComponent<TextMeshPro>();
        if (text == null)
        {
            text = textTransform.gameObject.AddComponent<TextMeshPro>();
        }

        textTransform.localPosition = localPosition;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 3f;
        text.rectTransform.sizeDelta = new Vector2(5f, 1f);
        return text;
    }

    private void FindManagerStyleTextReferences()
    {
        ManagerSimulation manager = GetComponent<ManagerSimulation>();
        if (manager != null)
        {
            _nameText ??= manager.Nametext;
            _descriptionText ??= manager.Companytext;
        }

        TextMeshPro[] labels = GetComponentsInChildren<TextMeshPro>(true);
        foreach (TextMeshPro label in labels)
        {
            string labelName = label.gameObject.name.ToLowerInvariant();
            if (_nameText == null && labelName.Contains("name"))
            {
                _nameText = label;
            }
            else if (_descriptionText == null
                && (labelName.Contains("company") || labelName.Contains("description")))
            {
                _descriptionText = label;
            }
        }

        if (_nameText == null)
        {
            _nameText = CreateOverheadLabel("NameText", new Vector3(0f, 1.7f, 0f));
        }
        if (_descriptionText == null)
        {
            _descriptionText = CreateOverheadLabel("DescriptionText", new Vector3(0f, 1.2f, 0f));
        }
    }

    private TextMeshPro CreateOverheadLabel(string objectName, Vector3 localPosition)
    {
        GameObject labelObject = new GameObject(objectName);
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = localPosition;

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 3f;
        label.rectTransform.sizeDelta = new Vector2(5f, 1f);

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            label.renderer.sortingLayerID = spriteRenderer.sortingLayerID;
            label.renderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        return label;
    }

    private void RefreshDescription()
    {
        if (_nameText != null)
        {
            _nameText.text = _npcName;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = _showDescription ? _description : string.Empty;
            _descriptionText.fontSize = _descriptionFontSize;
            _descriptionText.transform.localPosition = _descriptionLocalPosition;
            _descriptionText.gameObject.SetActive(false);
        }

        ConfigureHoverNameLabel();
    }

    private void ConfigureHoverNameLabel()
    {
        if (_nameText == null)
        {
            return;
        }

        CharacterNameHoverDisplay hoverDisplay = GetComponent<CharacterNameHoverDisplay>();
        if (hoverDisplay == null)
        {
            hoverDisplay = gameObject.AddComponent<CharacterNameHoverDisplay>();
        }

        GameObject speakBalloon = null;
        Transform speakBalloonTransform = transform.Find("speak_balloon");
        if (speakBalloonTransform != null)
        {
            speakBalloon = speakBalloonTransform.gameObject;
        }

        hoverDisplay.ConfigureInfo(
            new[] { _nameText, _showDescription ? _descriptionText : null },
            speakBalloon);
    }
}

using UnityEngine;
using UnityEngine.UI;

public class HideoCompletionNpcPortrait : MonoBehaviour
{
    [Header("Portrait Sprite")]
    [Tooltip("Portraits matching the GameController NPC sprite array order.")]
    [SerializeField] private Sprite[] _portraits;
    [Tooltip("Assign this for a Canvas UI portrait.")]
    [SerializeField] private Image _portraitImage;
    [Tooltip("Assign this instead when the portrait is a world SpriteRenderer.")]
    [SerializeField] private SpriteRenderer _portraitSpriteRenderer;

    [Header("UI Portrait Layout")]
    [Tooltip("NPC object that the portrait follows. Assign the NPC shown in this scene.")]
    [SerializeField] private Transform _npcTarget;
    [Tooltip("When enabled, the portrait is placed at the upper-left of the assigned NPC.")]
    [SerializeField] private bool _followNpc = true;
    [Tooltip("Screen-space offset from the NPC's upper-left corner.")]
    [SerializeField] private Vector2 _portraitPosition = new Vector2(-20f, 20f);
    [SerializeField] private Vector2 _portraitSize = new Vector2(180f, 180f);

    private void Start()
    {
        EnsurePortraitRenderer();
        ApplyPortraitLayout();
        ShowSelectedPortrait();
    }

    private void EnsurePortraitRenderer()
    {
        if (_portraitImage == null)
        {
            _portraitImage = GetComponent<Image>();
        }
        if (_portraitImage == null)
        {
            _portraitImage = GetComponentInChildren<Image>(true);
        }

        if (_portraitImage == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas != null)
            {
                GameObject portraitObject = new GameObject(
                    "Hideo Completion NPC Portrait",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                portraitObject.transform.SetParent(canvas.transform, false);
                _portraitImage = portraitObject.GetComponent<Image>();
                _portraitImage.raycastTarget = false;
            }
        }

        if (_portraitSpriteRenderer == null)
        {
            _portraitSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (_followNpc && _npcTarget != null)
        {
            ApplyPortraitLayout();
        }
    }

    public void SetNpcTarget(Transform npcTarget)
    {
        _npcTarget = npcTarget;
        ApplyPortraitLayout();
    }

    public void SetPortraitPosition(Vector2 anchoredPosition)
    {
        _portraitPosition = anchoredPosition;
        ApplyPortraitLayout();
    }

    public void SetPortraitSize(Vector2 size)
    {
        _portraitSize = size;
        ApplyPortraitLayout();
    }

    private void ApplyPortraitLayout()
    {
        if (_portraitImage == null)
        {
            return;
        }

        RectTransform portraitRect = _portraitImage.rectTransform;
        portraitRect.sizeDelta = new Vector2(
            Mathf.Max(1f, _portraitSize.x),
            Mathf.Max(1f, _portraitSize.y));

        if (_followNpc && _npcTarget != null && TryGetNpcPortraitPosition(out Vector2 localPosition))
        {
            RectTransform portraitParent = portraitRect.parent as RectTransform;
            Vector2 parentPivot = portraitParent != null
                ? portraitParent.pivot
                : new Vector2(0.5f, 0.5f);
            portraitRect.anchorMin = parentPivot;
            portraitRect.anchorMax = parentPivot;
            portraitRect.pivot = new Vector2(1f, 0f);
            portraitRect.anchoredPosition = localPosition + _portraitPosition;
            return;
        }

        portraitRect.anchorMin = new Vector2(0f, 1f);
        portraitRect.anchorMax = new Vector2(0f, 1f);
        portraitRect.pivot = new Vector2(0f, 1f);
        portraitRect.anchoredPosition = _portraitPosition;
    }

    private bool TryGetNpcPortraitPosition(out Vector2 localPosition)
    {
        localPosition = Vector2.zero;
        RectTransform portraitParent = _portraitImage.rectTransform.parent as RectTransform;
        if (portraitParent == null)
        {
            return false;
        }

        Vector3 worldPosition = _npcTarget.position;
        SpriteRenderer npcRenderer = _npcTarget.GetComponentInChildren<SpriteRenderer>();
        if (npcRenderer != null)
        {
            Bounds bounds = npcRenderer.bounds;
            worldPosition = new Vector3(bounds.min.x, bounds.max.y, bounds.center.z);
        }

        Canvas canvas = _portraitImage.canvas;
        Camera worldCamera = Camera.main;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            portraitParent,
            screenPosition,
            uiCamera,
            out localPosition);
    }

    public void ShowSelectedPortrait()
    {
        EnsurePortraitRenderer();

        if (_portraits == null || _portraits.Length == 0)
        {
            Debug.LogWarning(
                "No Hideo completion NPC portrait was found. Assign Portrait Sprite in GameController's NPC variant.");
            return;
        }

        int selectedIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(GameController.HideoCompletionNpcAppearanceKey, 0),
            0,
            _portraits.Length - 1);
        SetPortrait(selectedIndex);
    }

    public void SetPortrait(int index)
    {
        if (_portraits == null || index < 0 || index >= _portraits.Length)
        {
            return;
        }

        ApplyPortrait(_portraits[index]);
    }

    public void SetPortraitSprite(Sprite portrait)
    {
        ApplyPortrait(portrait);
    }

    private void ApplyPortrait(Sprite portrait)
    {
        ApplyPortraitLayout();

        if (_portraitImage != null)
        {
            _portraitImage.sprite = portrait;
            _portraitImage.enabled = portrait != null;
        }
        if (_portraitSpriteRenderer != null)
        {
            _portraitSpriteRenderer.sprite = portrait;
            _portraitSpriteRenderer.enabled = portrait != null;
        }
    }
}

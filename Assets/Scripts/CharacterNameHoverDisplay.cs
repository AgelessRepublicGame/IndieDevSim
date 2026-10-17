using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterNameHoverDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool showOnlyWhenInteractiveEnabled = false;
    [SerializeField] private bool useMouseHover = true;
    [SerializeField] private TextMeshPro[] labels;
    [SerializeField] private GameObject[] infoObjects;

    private InteractiveObj interactiveObj;
    private Collider2D[] hoverColliders;
    private bool hovering;
    private bool pointerHovering;
    private bool forceVisible;

    private void Awake()
    {
        interactiveObj = GetComponent<InteractiveObj>();
        hoverColliders = GetComponentsInChildren<Collider2D>(true);
        SetInfoVisible(false);
    }

    private void OnEnable()
    {
        SetInfoVisible(false);
        hovering = false;
        pointerHovering = false;
        forceVisible = false;
    }

    private void OnDisable()
    {
        SetInfoVisible(false);
        hovering = false;
        pointerHovering = false;
        forceVisible = false;
    }

    private void Update()
    {
        hovering = useMouseHover && (pointerHovering || IsMouseOverCharacter());
        SetInfoVisible((forceVisible || hovering) && CanShowLabels());
    }

    public void Configure(params TextMeshPro[] targetLabels)
    {
        labels = targetLabels;
        hoverColliders = GetComponentsInChildren<Collider2D>(true);
        SetInfoVisible(false);
    }

    public void SetForceVisible(bool visible)
    {
        forceVisible = visible;
        SetInfoVisible((forceVisible || hovering) && CanShowLabels());
    }

    public void SetMouseHoverEnabled(bool enabled)
    {
        useMouseHover = enabled;
        if (!useMouseHover)
        {
            pointerHovering = false;
            hovering = false;
            SetInfoVisible(forceVisible && CanShowLabels());
        }
    }

    public void ConfigureInfo(TextMeshPro[] targetLabels, params GameObject[] targetInfoObjects)
    {
        labels = targetLabels;
        infoObjects = targetInfoObjects;
        hoverColliders = GetComponentsInChildren<Collider2D>(true);
        SetInfoVisible(false);
    }

    private void OnMouseEnter()
    {
        if (!useMouseHover)
        {
            return;
        }

        pointerHovering = true;
        SetInfoVisible(CanShowLabels());
    }

    private void OnMouseExit()
    {
        if (!useMouseHover)
        {
            return;
        }

        pointerHovering = false;
        SetInfoVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!useMouseHover)
        {
            return;
        }

        pointerHovering = true;
        SetInfoVisible(CanShowLabels());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!useMouseHover)
        {
            return;
        }

        pointerHovering = false;
        SetInfoVisible(false);
    }

    private bool CanShowLabels()
    {
        return !showOnlyWhenInteractiveEnabled
            || interactiveObj == null
            || interactiveObj.enabled;
    }

    private bool IsMouseOverCharacter()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        Vector3 mouseWorldPosition = camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePoint = new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);

        if (hoverColliders == null || hoverColliders.Length == 0)
        {
            hoverColliders = GetComponentsInChildren<Collider2D>(true);
        }

        foreach (Collider2D hoverCollider in hoverColliders)
        {
            if (hoverCollider != null
                && hoverCollider.enabled
                && hoverCollider.gameObject.activeInHierarchy
                && hoverCollider.OverlapPoint(mousePoint))
            {
                return true;
            }
        }

        return false;
    }

    private void SetInfoVisible(bool visible)
    {
        if (labels != null)
        {
            foreach (TextMeshPro label in labels)
            {
                if (label != null)
                {
                    label.gameObject.SetActive(visible);
                }
            }
        }

        if (infoObjects == null)
        {
            return;
        }

        foreach (GameObject infoObject in infoObjects)
        {
            if (infoObject != null)
            {
                infoObject.SetActive(visible);
            }
        }
    }
}

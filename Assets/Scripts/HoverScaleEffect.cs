using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HoverScaleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] [Min(1f)] private float hoverScale = 1.08f;
    [SerializeField] [Min(0.01f)] private float scaleSpeed = 14f;
    [SerializeField] private bool onlyWhenInteractiveEnabled = true;

    private InteractiveObj interactiveObj;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool hovering;

    private void Awake()
    {
        interactiveObj = GetComponent<InteractiveObj>();
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void OnEnable()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        hovering = false;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
        hovering = false;
    }

    private void Update()
    {
        bool canHoverScale = CanScale();
        targetScale = hovering && canHoverScale
            ? originalScale * hoverScale
            : originalScale;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            scaleSpeed * Time.deltaTime);
    }

    private void OnMouseEnter()
    {
        hovering = true;
    }

    private void OnMouseExit()
    {
        hovering = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    private bool CanScale()
    {
        return !onlyWhenInteractiveEnabled
            || interactiveObj == null
            || interactiveObj.enabled;
    }
}

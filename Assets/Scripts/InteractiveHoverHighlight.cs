using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractiveHoverHighlight : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.2f, 0.45f);
    [SerializeField] [Min(1f)] private float highlightScale = 1.08f;
    [SerializeField] private int sortingOrderOffset = -1;
    [SerializeField] private bool includeChildSprites = true;
    [SerializeField] private bool onlyWhenInteractiveEnabled = true;

    private readonly List<GameObject> highlightObjects = new List<GameObject>();
    private InteractiveObj interactiveObj;
    private bool isHovering;

    private void Awake()
    {
        interactiveObj = GetComponent<InteractiveObj>();
        BuildHighlightSprites();
        SetHighlightVisible(false);
    }

    private void OnEnable()
    {
        SetHighlightVisible(false);
    }

    private void OnDisable()
    {
        SetHighlightVisible(false);
        isHovering = false;
    }

    private void Update()
    {
        if (isHovering && !CanShowHighlight())
        {
            SetHighlightVisible(false);
        }
    }

    private void OnMouseEnter()
    {
        isHovering = true;
        if (CanShowHighlight())
        {
            SetHighlightVisible(true);
        }
    }

    private void OnMouseExit()
    {
        isHovering = false;
        SetHighlightVisible(false);
    }

    public void RefreshHighlight()
    {
        ClearHighlightSprites();
        BuildHighlightSprites();
        SetHighlightVisible(isHovering && CanShowHighlight());
    }

    private bool CanShowHighlight()
    {
        return !onlyWhenInteractiveEnabled
            || interactiveObj == null
            || interactiveObj.enabled;
    }

    private void BuildHighlightSprites()
    {
        SpriteRenderer[] renderers = includeChildSprites
            ? GetComponentsInChildren<SpriteRenderer>(true)
            : GetComponents<SpriteRenderer>();

        foreach (SpriteRenderer sourceRenderer in renderers)
        {
            if (sourceRenderer == null
                || sourceRenderer.sprite == null
                || sourceRenderer.GetComponentInParent<InteractiveHoverHighlightMarker>() != null)
            {
                continue;
            }

            GameObject highlightObject = new GameObject(sourceRenderer.gameObject.name + "_HoverHighlight");
            highlightObject.transform.SetParent(sourceRenderer.transform, false);
            highlightObject.transform.localPosition = Vector3.zero;
            highlightObject.transform.localRotation = Quaternion.identity;
            highlightObject.transform.localScale = new Vector3(highlightScale, highlightScale, 1f);

            InteractiveHoverHighlightMarker marker = highlightObject.AddComponent<InteractiveHoverHighlightMarker>();
            marker.hideFlags = HideFlags.HideInInspector;

            SpriteRenderer highlightRenderer = highlightObject.AddComponent<SpriteRenderer>();
            CopyRenderer(sourceRenderer, highlightRenderer);
            highlightRenderer.color = highlightColor;
            highlightRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;
            highlightObject.SetActive(false);

            highlightObjects.Add(highlightObject);
        }
    }

    private void CopyRenderer(SpriteRenderer source, SpriteRenderer target)
    {
        target.sprite = source.sprite;
        target.drawMode = source.drawMode;
        target.size = source.size;
        target.tileMode = source.tileMode;
        target.maskInteraction = source.maskInteraction;
        target.spriteSortPoint = source.spriteSortPoint;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
    }

    private void SetHighlightVisible(bool visible)
    {
        foreach (GameObject highlightObject in highlightObjects)
        {
            if (highlightObject != null)
            {
                highlightObject.SetActive(visible);
            }
        }
    }

    private void ClearHighlightSprites()
    {
        foreach (GameObject highlightObject in highlightObjects)
        {
            if (highlightObject != null)
            {
                Destroy(highlightObject);
            }
        }

        highlightObjects.Clear();
    }
}

public class InteractiveHoverHighlightMarker : MonoBehaviour
{
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChairFellOverlay : MonoBehaviour
{
    private RectTransform _chairRect;
    private Image _blockPanel;

    public static ChairFellOverlay Create(Transform minigameRoot, Sprite chairSprite, string deviceType)
    {
        if (minigameRoot == null)
        {
            return null;
        }

        Canvas canvas = minigameRoot.GetComponentInChildren<Canvas>(true);
        Transform parent = canvas != null ? canvas.transform : minigameRoot;

        GameObject overlayObject = new GameObject("ChairFellOverlay");
        overlayObject.transform.SetParent(parent, false);

        RectTransform rootRect = overlayObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();

        Image dimBackground = overlayObject.AddComponent<Image>();
        dimBackground.color = new Color(0f, 0f, 0f, 0.18f);
        dimBackground.raycastTarget = true;

        ChairFellOverlay overlay = overlayObject.AddComponent<ChairFellOverlay>();

        Vector2 inconvenientAnchor = StageEventSystem.GetInconvenientAnchor(deviceType);
        GameObject blockObject = new GameObject("FreezeZone");
        blockObject.transform.SetParent(overlayObject.transform, false);
        RectTransform blockRect = blockObject.AddComponent<RectTransform>();
        blockRect.anchorMin = inconvenientAnchor;
        blockRect.anchorMax = inconvenientAnchor;
        blockRect.pivot = new Vector2(0.5f, 0.5f);
        blockRect.sizeDelta = new Vector2(360f, 280f);

        Image blockPanel = blockObject.AddComponent<Image>();
        blockPanel.color = new Color(0.12f, 0.1f, 0.08f, 0.82f);
        blockPanel.raycastTarget = true;
        overlay._blockPanel = blockPanel;

        if (chairSprite != null)
        {
            GameObject chairObject = new GameObject("FallingChair");
            chairObject.transform.SetParent(blockObject.transform, false);
            RectTransform chairRect = chairObject.AddComponent<RectTransform>();
            chairRect.anchorMin = new Vector2(0.5f, 0.5f);
            chairRect.anchorMax = new Vector2(0.5f, 0.5f);
            chairRect.pivot = new Vector2(0.5f, 0.2f);
            chairRect.sizeDelta = new Vector2(210f, 210f);
            chairRect.anchoredPosition = new Vector2(0f, 120f);
            chairRect.localRotation = Quaternion.Euler(0f, 0f, -24f);

            Image chairImage = chairObject.AddComponent<Image>();
            chairImage.sprite = chairSprite;
            chairImage.preserveAspect = true;
            chairImage.color = new Color(0.72f, 0.55f, 0.38f, 1f);
            chairImage.raycastTarget = false;

            overlay._chairRect = chairRect;
        }

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(blockObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(320f, 56f);
        labelRect.anchoredPosition = new Vector2(0f, 12f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "Everyone stared...";
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.95f, 0.85f, 0.95f);
        label.raycastTarget = false;

        return overlay;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public IEnumerator PlayFallAnimation()
    {
        if (_chairRect == null)
        {
            yield break;
        }

        float duration = ChairFellSystem.FallAnimationSeconds;
        float elapsed = 0f;
        Vector2 startPos = new Vector2(0f, 120f);
        Vector2 endPos = new Vector2(18f, -28f);
        float startRot = -24f;
        float endRot = 78f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t;

            _chairRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            _chairRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startRot, endRot, eased));

            if (_blockPanel != null)
            {
                float shake = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t) * 6f;
                _blockPanel.rectTransform.anchoredPosition = new Vector2(shake, 0f);
            }

            yield return null;
        }

        if (_blockPanel != null)
        {
            _blockPanel.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    public void Hide()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}

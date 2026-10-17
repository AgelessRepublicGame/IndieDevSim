using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SuddenCallOverlay : MonoBehaviour
{
    private RectTransform _phoneRect;

    public static SuddenCallOverlay Create(Transform minigameRoot, Sprite phoneSprite)
    {
        if (minigameRoot == null)
        {
            return null;
        }

        Canvas canvas = minigameRoot.GetComponentInChildren<Canvas>(true);
        Transform parent = canvas != null ? canvas.transform : minigameRoot;

        GameObject overlayObject = new GameObject("SuddenCallBlockOverlay");
        overlayObject.transform.SetParent(parent, false);

        RectTransform rect = overlayObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        Image background = overlayObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.05f, 0.08f, 0.72f);
        background.raycastTarget = true;

        SuddenCallOverlay overlay = overlayObject.AddComponent<SuddenCallOverlay>();

        if (phoneSprite != null)
        {
            GameObject phoneObject = new GameObject("PhoneImage");
            phoneObject.transform.SetParent(overlayObject.transform, false);
            RectTransform phoneRect = phoneObject.AddComponent<RectTransform>();
            phoneRect.anchorMin = new Vector2(0.5f, 0.5f);
            phoneRect.anchorMax = new Vector2(0.5f, 0.5f);
            phoneRect.pivot = new Vector2(0.5f, 0.5f);
            phoneRect.sizeDelta = new Vector2(500f, 500f);
            phoneRect.anchoredPosition = new Vector2(0f, 90f);

            Image phoneImage = phoneObject.AddComponent<Image>();
            phoneImage.sprite = phoneSprite;
            phoneImage.preserveAspect = true;
            phoneImage.color = Color.white;
            phoneImage.raycastTarget = false;

            overlay._phoneRect = phoneRect;
        }

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(overlayObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(520f, 72f);
        labelRect.anchoredPosition = new Vector2(0f, phoneSprite != null ? -190f : -40f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "Mom is calling...";
        label.fontSize = 40f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 1f, 0.9f);
        label.raycastTarget = false;

        return overlay;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void SetPhoneScale(float scale)
    {
        if (_phoneRect != null)
        {
            _phoneRect.localScale = Vector3.one * scale;
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

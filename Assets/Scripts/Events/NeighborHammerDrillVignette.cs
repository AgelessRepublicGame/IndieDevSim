using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NeighborHammerDrillVignette : MonoBehaviour
{
    public static NeighborHammerDrillVignette Create(Transform canvasRoot)
    {
        if (canvasRoot == null)
        {
            return null;
        }

        GameObject rootObject = new GameObject("NeighborHammerDrillVignette");
        rootObject.transform.SetParent(canvasRoot, false);

        RectTransform rootRect = rootObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();

        Image blocker = rootObject.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0f);
        blocker.raycastTarget = false;

        NeighborHammerDrillVignette vignette = rootObject.AddComponent<NeighborHammerDrillVignette>();
        vignette.BuildEdgeBlur(rootObject.transform);
        vignette.BuildLabel(rootObject.transform);

        rootObject.SetActive(false);
        return vignette;
    }

    private void BuildEdgeBlur(Transform parent)
    {
        CreateEdgePanel(parent, "TopBlur", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -140f), new Vector2(0f, 0f), 0.42f);
        CreateEdgePanel(parent, "BottomBlur", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 140f), 0.42f);
        CreateEdgePanel(parent, "LeftBlur", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(140f, 0f), 0.36f);
        CreateEdgePanel(parent, "RightBlur", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-140f, 0f), new Vector2(0f, 0f), 0.36f);

        CreateEdgePanel(parent, "TopInner", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -70f), new Vector2(0f, 0f), 0.18f);
        CreateEdgePanel(parent, "BottomInner", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 70f), 0.18f);
        CreateEdgePanel(parent, "LeftInner", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(70f, 0f), 0.14f);
        CreateEdgePanel(parent, "RightInner", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-70f, 0f), new Vector2(0f, 0f), 0.14f);
    }

    private static void CreateEdgePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        float alpha)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = panelObject.AddComponent<Image>();
        image.color = new Color(0.55f, 0.58f, 0.62f, alpha);
        image.raycastTarget = false;
    }

    private void BuildLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("DrillLabel");
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -24f);
        labelRect.sizeDelta = new Vector2(760f, 48f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "BZZZT... BZZZT...";
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Italic;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.9f, 0.92f, 0.95f, 0.85f);
        label.raycastTarget = false;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}

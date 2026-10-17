using UnityEngine;
using UnityEngine.UI;

public class MonitorBrokenFlicker : MonoBehaviour
{
    [SerializeField] private float _minAlpha = 0.12f;
    [SerializeField] private float _maxAlpha = 0.42f;
    [SerializeField] private float _cycleSeconds = 1.1f;

    private Image _overlay;

    public static MonitorBrokenFlicker Create(Transform canvasRoot)
    {
        if (canvasRoot == null)
        {
            return null;
        }

        GameObject rootObject = new GameObject("MonitorBrokenFlicker");
        rootObject.transform.SetParent(canvasRoot, false);

        RectTransform rect = rootObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        Image overlay = rootObject.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.12f);
        overlay.raycastTarget = false;

        MonitorBrokenFlicker flicker = rootObject.AddComponent<MonitorBrokenFlicker>();
        flicker._overlay = overlay;
        rootObject.SetActive(false);
        return flicker;
    }

    private void Update()
    {
        if (_overlay == null)
        {
            return;
        }

        float wave = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.1f, _cycleSeconds)) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(_minAlpha, _maxAlpha, wave);
        _overlay.color = new Color(0f, 0f, 0f, alpha);
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

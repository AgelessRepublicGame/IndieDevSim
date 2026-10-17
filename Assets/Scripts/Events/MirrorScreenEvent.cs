using System.Collections;
using UnityEngine;

public class MirrorScreenEvent : MonoBehaviour
{
    private float _minimumDelay = 2f;
    private float _maximumDelay = 3f;
    private float _minimumDuration = 3f;
    private float _maximumDuration = 5f;
    private string _panelName = "Screen";
    private RectTransform _screenPanel;
    private Quaternion _originalRotation;
    private Coroutine _eventRoutine;

    public void Configure(
        float minimumDelay,
        float maximumDelay,
        float minimumDuration,
        float maximumDuration,
        string panelName = "Screen")
    {
        _minimumDelay = Mathf.Max(0f, minimumDelay);
        _maximumDelay = Mathf.Max(_minimumDelay, maximumDelay);
        _minimumDuration = Mathf.Max(0f, minimumDuration);
        _maximumDuration = Mathf.Max(_minimumDuration, maximumDuration);
        _panelName = string.IsNullOrWhiteSpace(panelName) ? "Screen" : panelName;
    }

    private void Start()
    {
        _eventRoutine = StartCoroutine(MirrorRoutine());
    }

    private IEnumerator MirrorRoutine()
    {
        yield return WaitUnscaled(Random.Range(_minimumDelay, _maximumDelay));

        _screenPanel = FindScreenPanel();
        if (_screenPanel == null)
        {
            Debug.LogWarning("Mirror Screen event could not find a UI element named '"
                + _panelName + "' inside " + name + ".");
            yield break;
        }

        _originalRotation = _screenPanel.localRotation;
        _screenPanel.localRotation = _originalRotation * Quaternion.Euler(0f, 0f, 180f);
        yield return WaitUnscaled(Random.Range(_minimumDuration, _maximumDuration));

        if (_screenPanel != null)
        {
            _screenPanel.localRotation = _originalRotation;
        }
        _eventRoutine = null;
    }

    private RectTransform FindScreenPanel()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        Transform searchRoot = canvas != null ? canvas.transform : transform;
        RectTransform[] rectTransforms = searchRoot.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform != null
                && rectTransform.gameObject.name.Equals(_panelName, System.StringComparison.OrdinalIgnoreCase))
            {
                return rectTransform;
            }
        }

        return null;
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void OnDisable()
    {
        if (_eventRoutine != null)
        {
            StopCoroutine(_eventRoutine);
            _eventRoutine = null;
        }

        if (_screenPanel != null)
        {
            _screenPanel.localRotation = _originalRotation;
        }
    }
}

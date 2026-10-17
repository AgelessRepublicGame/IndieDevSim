using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class FliesInvasionSettings
{
    [Tooltip("Optional sprite for each fly. Leave empty for a black dot.")]
    public Sprite flySprite;
    public Color flyColor = Color.black;

    [Tooltip("Width and height of each fly body in pixels.")]
    public Vector2 flyBodySize = new Vector2(12f, 8f);

    [Min(1)] public int flyCount = 24;

    [Tooltip("Base movement speed in UI pixels per second.")]
    [Min(1f)] public float flySpeed = 110f;

    [Tooltip("Random speed multiplier range for each fly.")]
    public Vector2 flySpeedVariation = new Vector2(0.65f, 1.35f);

    [Tooltip("How quickly each fly turns toward a new heading.")]
    [Min(0.1f)] public float turnRate = 5f;

    [Tooltip("Seconds between choosing a new heading for each fly.")]
    public Vector2 headingChangeInterval = new Vector2(0.25f, 1.1f);

    [Tooltip("How much each fly wobbles while flying.")]
    [Range(0f, 1.5f)] public float wobbleAmount = 0.45f;

    [Tooltip("How fast the wobble oscillates.")]
    [Min(0.1f)] public float wobbleFrequency = 7f;

    [Tooltip("Rotate the fly sprite to face its movement direction.")]
    public bool rotateWithMovement = true;
}

public class FliesInvasionVisual : MonoBehaviour
{
    private class FlyAgent
    {
        public RectTransform Rect;
        public float Heading;
        public float Speed;
        public float TurnRate;
        public float TargetHeading;
        public float HeadingTimer;
        public float WobblePhase;
        public float WobbleStrength;
        public Vector2 BodyHalfSize;
    }

    private readonly List<FlyAgent> _flies = new List<FlyAgent>();
    private RectTransform _bounds;
    private FliesInvasionSettings _settings;

    public static FliesInvasionVisual Create(Transform parent, FliesInvasionSettings settings)
    {
        if (parent == null || settings == null)
        {
            return null;
        }

        GameObject rootObject = new GameObject("FliesInvasionVisual");
        rootObject.transform.SetParent(parent, false);

        RectTransform rootRect = rootObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();

        Image blocker = rootObject.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0f);
        blocker.raycastTarget = false;

        FliesInvasionVisual visual = rootObject.AddComponent<FliesInvasionVisual>();
        visual._bounds = rootRect;
        visual._settings = settings;
        visual.SpawnFlies();
        rootObject.SetActive(false);
        return visual;
    }

    private void SpawnFlies()
    {
        _flies.Clear();

        Vector2 bodySize = GetBodySize();
        Vector2 halfBounds = GetHalfBounds();
        Vector2 padding = bodySize * 0.5f;

        int count = Mathf.Max(1, _settings.flyCount);
        for (int i = 0; i < count; i++)
        {
            GameObject flyObject = new GameObject("Fly");
            flyObject.transform.SetParent(transform, false);

            RectTransform flyRect = flyObject.AddComponent<RectTransform>();
            flyRect.sizeDelta = bodySize;
            flyRect.anchorMin = new Vector2(0.5f, 0.5f);
            flyRect.anchorMax = new Vector2(0.5f, 0.5f);
            flyRect.pivot = new Vector2(0.5f, 0.5f);
            flyRect.anchoredPosition = new Vector2(
                Random.Range(-halfBounds.x + padding.x, halfBounds.x - padding.x),
                Random.Range(-halfBounds.y + padding.y, halfBounds.y - padding.y));

            Image flyImage = flyObject.AddComponent<Image>();
            flyImage.sprite = _settings.flySprite;
            // flyImage.color = _settings.flyColor;
            flyImage.raycastTarget = false;
            flyImage.preserveAspect = true;

            float heading = Random.Range(0f, Mathf.PI * 2f);
            float speedMultiplier = Random.Range(
                _settings.flySpeedVariation.x,
                _settings.flySpeedVariation.y);

            FlyAgent agent = new FlyAgent
            {
                Rect = flyRect,
                Heading = heading,
                TargetHeading = heading,
                Speed = _settings.flySpeed * speedMultiplier,
                TurnRate = _settings.turnRate * Random.Range(0.75f, 1.25f),
                HeadingTimer = Random.Range(_settings.headingChangeInterval.x, _settings.headingChangeInterval.y),
                WobblePhase = Random.Range(0f, Mathf.PI * 2f),
                WobbleStrength = _settings.wobbleAmount * Random.Range(0.6f, 1.4f),
                BodyHalfSize = bodySize * 0.5f
            };
            ApplyRotation(agent, heading);
            _flies.Add(agent);
        }
    }

    private void Update()
    {
        if (_bounds == null || _settings == null || _flies.Count == 0)
        {
            return;
        }

        Vector2 halfBounds = GetHalfBounds();
        float deltaTime = Time.unscaledDeltaTime;

        foreach (FlyAgent fly in _flies)
        {
            if (fly.Rect == null)
            {
                continue;
            }

            fly.HeadingTimer -= deltaTime;
            if (fly.HeadingTimer <= 0f)
            {
                fly.TargetHeading = fly.Heading + Random.Range(-Mathf.PI * 0.95f, Mathf.PI * 0.95f);
                fly.HeadingTimer = Random.Range(
                    _settings.headingChangeInterval.x,
                    _settings.headingChangeInterval.y);
            }

            float headingDelta = Mathf.DeltaAngle(
                fly.Heading * Mathf.Rad2Deg,
                fly.TargetHeading * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float maxTurn = fly.TurnRate * deltaTime;
            fly.Heading += Mathf.Clamp(headingDelta, -maxTurn, maxTurn);

            float wobble = Mathf.Sin((Time.unscaledTime * _settings.wobbleFrequency) + fly.WobblePhase)
                * fly.WobbleStrength;
            float moveHeading = fly.Heading + wobble;
            Vector2 velocity = new Vector2(Mathf.Cos(moveHeading), Mathf.Sin(moveHeading)) * fly.Speed;
            Vector2 anchored = fly.Rect.anchoredPosition + velocity * deltaTime;

            if (anchored.x < -halfBounds.x + fly.BodyHalfSize.x || anchored.x > halfBounds.x - fly.BodyHalfSize.x)
            {
                fly.Heading = Mathf.PI - fly.Heading;
                fly.TargetHeading = ReflectHeading(fly.TargetHeading, true);
                anchored.x = Mathf.Clamp(anchored.x, -halfBounds.x + fly.BodyHalfSize.x, halfBounds.x - fly.BodyHalfSize.x);
            }

            if (anchored.y < -halfBounds.y + fly.BodyHalfSize.y || anchored.y > halfBounds.y - fly.BodyHalfSize.y)
            {
                fly.Heading = -fly.Heading;
                fly.TargetHeading = ReflectHeading(fly.TargetHeading, false);
                anchored.y = Mathf.Clamp(anchored.y, -halfBounds.y + fly.BodyHalfSize.y, halfBounds.y - fly.BodyHalfSize.y);
            }

            fly.Rect.anchoredPosition = anchored;
            ApplyRotation(fly, moveHeading);
        }
    }

    private Vector2 GetBodySize()
    {
        return new Vector2(
            Mathf.Max(1f, _settings.flyBodySize.x),
            Mathf.Max(1f, _settings.flyBodySize.y));
    }

    private Vector2 GetHalfBounds()
    {
        Canvas.ForceUpdateCanvases();
        return _bounds.rect.size * 0.5f;
    }

    private static float ReflectHeading(float heading, bool reflectX)
    {
        if (reflectX)
        {
            return Mathf.PI - heading;
        }

        return -heading;
    }

    private void ApplyRotation(FlyAgent fly, float moveHeading)
    {
        if (!_settings.rotateWithMovement || fly.Rect == null)
        {
            return;
        }

        float angle = moveHeading * Mathf.Rad2Deg - 90f;
        fly.Rect.localRotation = Quaternion.Euler(0f, 0f, angle);
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

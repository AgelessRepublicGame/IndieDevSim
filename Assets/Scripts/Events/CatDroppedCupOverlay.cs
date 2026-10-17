using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatDroppedCupOverlay : MonoBehaviour
{
    private class Shard
    {
        public RectTransform Rect;
        public Vector2 Velocity;
        public float Lifetime;
        public Image Image;
    }

    private RectTransform _cupRect;
    private Image _fullScreenBlocker;
    private readonly List<Shard> _shards = new List<Shard>();

    public static CatDroppedCupOverlay Create(Transform minigameRoot, Sprite cupSprite, Sprite shardSprite, Vector2 cupSize)
    {
        if (minigameRoot == null)
        {
            return null;
        }

        Canvas canvas = minigameRoot.GetComponentInChildren<Canvas>(true);
        Transform parent = canvas != null ? canvas.transform : minigameRoot;

        GameObject overlayObject = new GameObject("CatDroppedCupOverlay");
        overlayObject.transform.SetParent(parent, false);

        RectTransform rootRect = overlayObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();

        Image dimBackground = overlayObject.AddComponent<Image>();
        dimBackground.color = new Color(0f, 0f, 0f, 0.15f);
        dimBackground.raycastTarget = false;

        CatDroppedCupOverlay overlay = overlayObject.AddComponent<CatDroppedCupOverlay>();

        GameObject blockerObject = new GameObject("FullScreenBlocker");
        blockerObject.transform.SetParent(overlayObject.transform, false);
        RectTransform blockerRect = blockerObject.AddComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        overlay._fullScreenBlocker = blockerObject.AddComponent<Image>();
        overlay._fullScreenBlocker.color = new Color(0f, 0f, 0f, 0f);
        overlay._fullScreenBlocker.raycastTarget = true;
        blockerObject.SetActive(false);

        GameObject cupObject = new GameObject("FallingCup");
        cupObject.transform.SetParent(overlayObject.transform, false);
        RectTransform cupRect = cupObject.AddComponent<RectTransform>();
        cupRect.anchorMin = new Vector2(0.5f, 0.5f);
        cupRect.anchorMax = new Vector2(0.5f, 0.5f);
        cupRect.pivot = new Vector2(0.5f, 0.15f);
        cupRect.sizeDelta = cupSize;
        cupRect.anchoredPosition = new Vector2(-40f, 180f);
        cupRect.localRotation = Quaternion.Euler(0f, 0f, 12f);

        Image cupImage = cupObject.AddComponent<Image>();
        cupImage.sprite = cupSprite;
        cupImage.preserveAspect = true;
        cupImage.color = cupSprite != null ? Color.white : new Color(0.75f, 0.55f, 0.35f, 1f);
        cupImage.raycastTarget = false;

        overlay._cupRect = cupRect;
        overlay._shardSprite = shardSprite;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(overlayObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(420f, 56f);
        labelRect.anchoredPosition = new Vector2(0f, 24f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "CRASH!";
        label.fontSize = 28f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.92f, 0.8f, 0f);
        label.raycastTarget = false;
        overlay._crashLabel = label;

        return overlay;
    }

    private Sprite _shardSprite;
    private TextMeshProUGUI _crashLabel;
    private bool _shardsSpawned;

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public IEnumerator PlayFallAnimation()
    {
        float duration = CatDroppedCupSystem.FallAnimationSeconds;
        float elapsed = 0f;
        Vector2 startPos = new Vector2(-40f, 180f);
        Vector2 endPos = new Vector2(24f, -60f);
        float startRot = 12f;
        float endRot = 108f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t;

            if (_cupRect != null)
            {
                _cupRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                _cupRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startRot, endRot, eased));
                _cupRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, eased);
            }

            yield return null;
        }

        if (_cupRect != null)
        {
            _cupRect.gameObject.SetActive(false);
        }

        SpawnShards(endPos);
        if (_crashLabel != null)
        {
            _crashLabel.color = new Color(1f, 0.92f, 0.8f, 0.95f);
        }

        if (_fullScreenBlocker != null)
        {
            _fullScreenBlocker.gameObject.SetActive(true);
        }
    }

    public IEnumerator PlayBlockPhase()
    {
        float elapsed = 0f;
        while (elapsed < CatDroppedCupSystem.BlockSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateShards(Time.unscaledDeltaTime);
            yield return null;
        }
    }

    public IEnumerator PlaySequence()
    {
        yield return PlayFallAnimation();
        yield return PlayBlockPhase();
    }

    private void SpawnShards(Vector2 impactPosition)
    {
        if (_shardsSpawned)
        {
            return;
        }

        _shardsSpawned = true;
        int shardCount = 10;

        for (int i = 0; i < shardCount; i++)
        {
            GameObject shardObject = new GameObject("Shard");
            shardObject.transform.SetParent(transform, false);

            RectTransform shardRect = shardObject.AddComponent<RectTransform>();
            shardRect.anchorMin = new Vector2(0.5f, 0.5f);
            shardRect.anchorMax = new Vector2(0.5f, 0.5f);
            shardRect.pivot = new Vector2(0.5f, 0.5f);
            shardRect.sizeDelta = new Vector2(Random.Range(8f, 18f), Random.Range(8f, 18f));
            shardRect.anchoredPosition = impactPosition + Random.insideUnitCircle * 12f;
            shardRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Image shardImage = shardObject.AddComponent<Image>();
            shardImage.sprite = _shardSprite;
            shardImage.color = _shardSprite != null
                ? new Color(1f, 1f, 1f, 0.95f)
                : new Color(0.85f, 0.8f, 0.72f, 0.95f);
            shardImage.raycastTarget = false;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(90f, 220f);
            _shards.Add(new Shard
            {
                Rect = shardRect,
                Image = shardImage,
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                Lifetime = Random.Range(0.35f, 0.9f)
            });
        }
    }

    private void UpdateShards(float deltaTime)
    {
        for (int i = _shards.Count - 1; i >= 0; i--)
        {
            Shard shard = _shards[i];
            if (shard.Rect == null || shard.Image == null)
            {
                _shards.RemoveAt(i);
                continue;
            }

            shard.Lifetime -= deltaTime;
            shard.Velocity += new Vector2(0f, -260f) * deltaTime;
            shard.Rect.anchoredPosition += shard.Velocity * deltaTime;
            shard.Rect.Rotate(0f, 0f, 420f * deltaTime);

            Color color = shard.Image.color;
            color.a = Mathf.Clamp01(shard.Lifetime / 0.5f);
            shard.Image.color = color;

            if (shard.Lifetime <= 0f)
            {
                Destroy(shard.Rect.gameObject);
                _shards.RemoveAt(i);
            }
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

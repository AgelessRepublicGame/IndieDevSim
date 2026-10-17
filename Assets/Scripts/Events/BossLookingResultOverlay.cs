using UnityEngine;

public class BossLookingResultOverlay : MonoBehaviour
{
    private BossLookingSettings _settings;
    private SpriteRenderer _emotionRenderer;
    private ParticleSystem _fireworksInstance;
    private Transform _followTarget;

    public static BossLookingResultOverlay Create(BossLookingSettings settings)
    {
        if (settings == null)
        {
            return null;
        }

        GameObject rootObject = new GameObject("BossLookingResultOverlay");
        BossLookingResultOverlay overlay = rootObject.AddComponent<BossLookingResultOverlay>();
        overlay._settings = settings;
        overlay.BuildEmotionRenderer();
        return overlay;
    }

    private void BuildEmotionRenderer()
    {
        GameObject emotionObject = new GameObject("BossEmotion");
        emotionObject.transform.SetParent(transform, false);

        _emotionRenderer = emotionObject.AddComponent<SpriteRenderer>();
        _emotionRenderer.sortingOrder = _settings.sortingOrder + 2;
        _emotionRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (_followTarget == null)
        {
            return;
        }

        transform.position = _followTarget.position;
        UpdateEmotionTransform();
    }

    public void ShowAt(Transform followTarget, BossLookingResult result, bool playFireworks)
    {
        _followTarget = followTarget;
        if (_followTarget != null)
        {
            transform.position = _followTarget.position;
        }

        Sprite emotionSprite = GetEmotionSprite(result);
        if (_emotionRenderer != null && emotionSprite != null)
        {
            _emotionRenderer.sprite = emotionSprite;
            _emotionRenderer.enabled = true;
            UpdateEmotionTransform();
        }

        if (playFireworks)
        {
            PlayFireworks();
        }
    }

    private void UpdateEmotionTransform()
    {
        if (_emotionRenderer == null)
        {
            return;
        }

        Transform emotionTransform = _emotionRenderer.transform;
        emotionTransform.localPosition = _settings.emotionWorldOffset;
        emotionTransform.localScale = Vector3.one * _settings.emotionWorldScale;
    }

    public void PlayFireworks()
    {
        if (_settings.fireworksPrefab == null)
        {
            return;
        }

        StopFireworks();

        Vector3 spawnPosition = transform.position + _settings.fireworksWorldOffset;
        _fireworksInstance = Instantiate(_settings.fireworksPrefab);
        _fireworksInstance.transform.position = spawnPosition;
        _fireworksInstance.transform.rotation = Quaternion.identity;

        var main = _fireworksInstance.main;
        main.loop = false;
        main.playOnAwake = false;

        var emission = _fireworksInstance.emission;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)_settings.fireworksBurstCount)
        });

        _fireworksInstance.Play(true);
        Destroy(_fireworksInstance.gameObject, _settings.fireworksDuration);
    }

    public void StopFireworks()
    {
        if (_fireworksInstance == null)
        {
            return;
        }

        Destroy(_fireworksInstance.gameObject);
        _fireworksInstance = null;
    }

    public void Hide()
    {
        _followTarget = null;

        if (_emotionRenderer != null)
        {
            _emotionRenderer.enabled = false;
            _emotionRenderer.sprite = null;
        }

        StopFireworks();
        Destroy(gameObject);
    }

    private Sprite GetEmotionSprite(BossLookingResult result)
    {
        switch (result)
        {
            case BossLookingResult.Satisfied:
                return _settings.satisfiedEmotion;
            case BossLookingResult.Dissatisfied:
                return _settings.dissatisfiedEmotion;
            default:
                return _settings.sadEmotion;
        }
    }
}

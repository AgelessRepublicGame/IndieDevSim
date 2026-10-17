using System.Collections;
using UnityEngine;

[System.Serializable]
public class BossLookingSettings
{
    [Header("Room Positions")]
    public Transform positionA;
    public Transform positionB;
    public Transform positionC;
    public string positionAObjectName = "PositionA";
    public string positionBObjectName = "PositionB";
    public string positionCObjectName = "PositionC";

    [Header("Visual")]
    [Tooltip("Optional character prefab (e.g. manager-style with Animator).")]
    public GameObject bossVisualPrefab;
    [Tooltip("Used when no prefab is assigned.")]
    public Sprite bossBodySprite;
    public RuntimeAnimatorController walkAnimatorController;
    public float bodyScale = 1f;
    public int sortingOrder = 50;

    [Header("Rotation")]
    [Tooltip("How fast the boss turns toward movement, like the player character.")]
    public float rotationSpeed = 720f;
    [Tooltip("Adjust so the sprite faces the movement direction. -90 works for sprites that face up.")]
    public float rotationOffset = -90f;

    [Header("Animation")]
    public float walkAnimatorSpeed = 1f;

    [Header("Emotion At Player")]
    public Sprite satisfiedEmotion;
    public Sprite dissatisfiedEmotion;
    public Sprite sadEmotion;
    public Vector3 emotionWorldOffset = new Vector3(0f, 1.2f, 0f);
    public float emotionWorldScale = 1f;

    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float arrivalThreshold = 0.05f;
    public float enterPauseSeconds = 0.35f;
    public Vector2 patrolPauseSeconds = new Vector2(1f, 2f);
    public Vector3 playerMeetOffset = new Vector3(0.8f, 0f, 0f);

    [Header("Collider")]
    public Vector2 colliderSize = new Vector2(0.6f, 0.9f);
    public Vector2 colliderOffset = Vector2.zero;

    [Header("Result")]
    [Range(0f, 1f)] public float experienceBonusPercent = 0.1f;
    [Range(0.5f, 1f)] public float perfectScoreThreshold = 0.999f;
    [Range(0.5f, 1f)] public float highQualityThreshold = 0.9f;
    public float resultDisplaySeconds = 2f;

    [Header("Fireworks At Player")]
    public ParticleSystem fireworksPrefab;
    public Vector3 fireworksWorldOffset = Vector3.zero;
    [Min(1)] public int fireworksBurstCount = 40;
    public float fireworksDuration = 1.5f;
}

public enum BossLookingResult
{
    Satisfied,
    Dissatisfied,
    Sad
}

public class BossLookingVisual : MonoBehaviour
{
    private BossLookingSettings _settings;
    private Transform _positionA;
    private Transform _positionB;
    private Transform _positionC;
    private Transform _bodyRoot;
    private SpriteRenderer _bodyRenderer;
    private Animator _walkAnimator;
    private Coroutine _patrolRoutine;
    private Coroutine _finishRoutine;
    private bool _movingToA = true;

    public static BossLookingVisual Create(Transform parent, BossLookingSettings settings)
    {
        if (settings == null)
        {
            return null;
        }

        Transform positionA = ResolvePosition(settings.positionA, settings.positionAObjectName);
        Transform positionB = ResolvePosition(settings.positionB, settings.positionBObjectName);
        Transform positionC = ResolvePosition(settings.positionC, settings.positionCObjectName);
        if (positionA == null || positionB == null || positionC == null)
        {
            Debug.LogWarning("Boss Looking: assign PositionA, PositionB, and PositionC in the Inspector or scene.");
            return null;
        }

        GameObject rootObject = new GameObject("BossLookingVisual");
        if (parent != null)
        {
            rootObject.transform.SetParent(parent, false);
        }

        BossLookingVisual visual = rootObject.AddComponent<BossLookingVisual>();
        visual._settings = settings;
        visual._positionA = positionA;
        visual._positionB = positionB;
        visual._positionC = positionC;
        visual.BuildComponents();
        rootObject.SetActive(false);
        return visual;
    }

    private static Transform ResolvePosition(Transform assigned, string objectName)
    {
        if (assigned != null)
        {
            return assigned;
        }

        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }

    private void BuildComponents()
    {
        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = _settings.colliderSize;
        collider.offset = _settings.colliderOffset;
        collider.isTrigger = false;

        if (_settings.bossVisualPrefab != null)
        {
            BuildFromPrefab();
        }
        else
        {
            BuildFromSprite();
        }

        SetWalking(false);
    }

    private void BuildFromPrefab()
    {
        GameObject instance = Instantiate(_settings.bossVisualPrefab, transform);
        instance.name = "BossBody";
        _bodyRoot = instance.transform;
        _bodyRoot.localPosition = Vector3.zero;
        _bodyRoot.localRotation = Quaternion.identity;
        _bodyRoot.localScale = Vector3.one * _settings.bodyScale;

        StripGameplayComponents(instance);
        HideUiChildren(instance);

        _walkAnimator = instance.GetComponentInChildren<Animator>();
        if (_settings.walkAnimatorController != null)
        {
            if (_walkAnimator == null)
            {
                _walkAnimator = instance.AddComponent<Animator>();
            }

            _walkAnimator.runtimeAnimatorController = _settings.walkAnimatorController;
        }
        else if (_walkAnimator != null)
        {
            _walkAnimator.enabled = false;
            _walkAnimator = null;
        }

        _bodyRenderer = instance.GetComponentInChildren<SpriteRenderer>();
    }

    private void BuildFromSprite()
    {
        GameObject bodyObject = new GameObject("BossBody");
        bodyObject.transform.SetParent(transform, false);
        _bodyRoot = bodyObject.transform;
        _bodyRoot.localScale = Vector3.one * _settings.bodyScale;

        _bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
        _bodyRenderer.sprite = _settings.bossBodySprite;
        _bodyRenderer.sortingOrder = _settings.sortingOrder;

        if (_settings.walkAnimatorController != null)
        {
            _walkAnimator = bodyObject.AddComponent<Animator>();
            _walkAnimator.runtimeAnimatorController = _settings.walkAnimatorController;
        }
    }

    private static void StripGameplayComponents(GameObject instance)
    {
        foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour is ManagerSimulation
                || behaviour is InteractiveObj
                || behaviour is PurchaseScript
                || behaviour is BossLookingVisual)
            {
                Destroy(behaviour);
            }
        }
    }

    private static void HideUiChildren(GameObject instance)
    {
        foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child.gameObject == instance)
            {
                continue;
            }

            string childName = child.name.ToLowerInvariant();
            if (childName.Contains("balloon") || childName.Contains("task"))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    public void BeginVisit()
    {
        if (_finishRoutine != null)
        {
            StopCoroutine(_finishRoutine);
            _finishRoutine = null;
        }

        if (_patrolRoutine != null)
        {
            StopCoroutine(_patrolRoutine);
            _patrolRoutine = null;
        }

        gameObject.SetActive(true);
        transform.position = _positionC.position;
        FaceDirection(_positionA.position - transform.position, instant: true);
        _patrolRoutine = StartCoroutine(BeginPatrolAfterPause());
    }

    private IEnumerator BeginPatrolAfterPause()
    {
        SetWalking(false);

        if (_settings.enterPauseSeconds > 0f)
        {
            yield return new WaitForSeconds(_settings.enterPauseSeconds);
        }

        _patrolRoutine = StartCoroutine(PatrolLoop());
    }

    private IEnumerator PatrolLoop()
    {
        Transform target = _positionA;
        _movingToA = true;

        while (true)
        {
            yield return MoveTo(target.position);
            SetWalking(false);
            yield return WaitPatrolPause();

            _movingToA = !_movingToA;
            target = _movingToA ? _positionA : _positionB;
        }
    }

    private IEnumerator WaitPatrolPause()
    {
        float pause = Random.Range(_settings.patrolPauseSeconds.x, _settings.patrolPauseSeconds.y);
        if (pause > 0f)
        {
            yield return new WaitForSeconds(pause);
        }
    }

    public IEnumerator WalkTo(Vector3 targetPosition)
    {
        if (_patrolRoutine != null)
        {
            StopCoroutine(_patrolRoutine);
            _patrolRoutine = null;
        }

        yield return MoveTo(targetPosition);
    }

    public IEnumerator WalkToExit()
    {
        if (_patrolRoutine != null)
        {
            StopCoroutine(_patrolRoutine);
            _patrolRoutine = null;
        }

        if (_finishRoutine != null)
        {
            StopCoroutine(_finishRoutine);
        }

        SetWalking(false);
        yield return MoveTo(_positionC.position);
        Hide();
        _finishRoutine = null;
    }

    public bool IsPerfectQuality(float averageQuality)
    {
        return averageQuality >= _settings.perfectScoreThreshold;
    }

    public BossLookingResult EvaluateResult(float averageQuality)
    {
        if (averageQuality >= _settings.perfectScoreThreshold)
        {
            return BossLookingResult.Satisfied;
        }

        if (averageQuality >= _settings.highQualityThreshold)
        {
            return BossLookingResult.Dissatisfied;
        }

        return BossLookingResult.Sad;
    }

    private IEnumerator MoveTo(Vector3 targetPosition)
    {
        SetWalking(true);

        while (Vector3.Distance(transform.position, targetPosition) > _settings.arrivalThreshold)
        {
            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                _settings.walkSpeed * Time.deltaTime);

            Vector3 moveDirection = nextPosition - transform.position;
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                FaceDirection(moveDirection, instant: false);
            }

            transform.position = nextPosition;
            yield return null;
        }

        transform.position = targetPosition;
        SetWalking(false);
    }

    private void FaceDirection(Vector3 moveDirection, bool instant)
    {
        if (_bodyRoot == null || moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float targetAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + _settings.rotationOffset;
        if (instant)
        {
            _bodyRoot.rotation = Quaternion.Euler(0f, 0f, targetAngle);
            return;
        }

        float currentAngle = _bodyRoot.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _settings.rotationSpeed * Time.deltaTime);
        _bodyRoot.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    private void SetWalking(bool walking)
    {
        if (_walkAnimator == null)
        {
            return;
        }

        _walkAnimator.speed = walking ? _settings.walkAnimatorSpeed : 0f;
    }

    public void Hide()
    {
        if (_patrolRoutine != null)
        {
            StopCoroutine(_patrolRoutine);
            _patrolRoutine = null;
        }

        if (_finishRoutine != null)
        {
            StopCoroutine(_finishRoutine);
            _finishRoutine = null;
        }

        SetWalking(false);
        Destroy(gameObject);
    }
}

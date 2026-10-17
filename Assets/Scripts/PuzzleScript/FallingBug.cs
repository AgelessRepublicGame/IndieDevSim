using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FallingBug : MonoBehaviour, IPointerClickHandler
{
    private ProgrammingBugGame game;
    private TMP_Text _label;
    private float fallSpeed;
    private BugLineType lineType;
    private bool moveHorizontally;
    private float horizontalSeed;
    private bool _clickable = true;
    private string _displayText = "";
    private Color _baseColor = Color.white;
    private bool _chaoticMovement;
    private float _chaoticMinTurnInterval;
    private float _chaoticMaxTurnInterval;
    private float _chaoticMinHorizontalMultiplier;
    private float _chaoticMaxHorizontalMultiplier;
    private float _chaoticMinFallMultiplier;
    private float _chaoticMaxFallMultiplier;
    private float _chaoticDirection;
    private float _chaoticHorizontalMultiplier;
    private float _chaoticFallMultiplier;
    private float _nextChaoticTurnTime;
    private bool _creepingMovement;
    private float _creepingSpeed;
    private float _creepingContactDistance;
    private bool _resolved;
    private RectTransform _clickAreaRect;
    private BoxCollider2D _clickCollider;

    public bool IsFixableBug => lineType == BugLineType.RedBug || lineType == BugLineType.PurpleBug;
    public bool IsRedBug => lineType == BugLineType.RedBug;
    public BugLineType LineType => lineType;
    public string DisplayText => _displayText;

    public void Init(
        ProgrammingBugGame gameManager,
        TMP_Text label,
        float speed,
        BugLineType bugLineType,
        bool horizontalMovement,
        string displayText,
        Color baseColor,
        bool clickable = true)
    {
        game = gameManager;
        _label = label;
        fallSpeed = speed;
        lineType = bugLineType;
        moveHorizontally = horizontalMovement;
        horizontalSeed = Random.Range(0f, 10f);
        _displayText = displayText ?? "";
        _baseColor = baseColor;
        _clickable = clickable;
        ResetDisplayText();
        CreateWordSizedClickCollider();
    }

    private void CreateWordSizedClickCollider()
    {
        if (!_clickable || _label == null)
        {
            return;
        }

        _label.ForceMeshUpdate();
        Bounds wordBounds = _label.textBounds;
        Vector2 wordSize = new Vector2(
            Mathf.Max(1f, wordBounds.size.x),
            Mathf.Max(1f, wordBounds.size.y));

        if (_clickAreaRect == null)
        {
            GameObject clickAreaObject = new GameObject("Click Area");
            clickAreaObject.transform.SetParent(transform, false);
            clickAreaObject.transform.SetAsLastSibling();

            _clickAreaRect = clickAreaObject.AddComponent<RectTransform>();
            _clickAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
            _clickAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
            _clickAreaRect.pivot = new Vector2(0.5f, 0.5f);

            Image clickImage = clickAreaObject.AddComponent<Image>();
            clickImage.color = new Color(1f, 1f, 1f, 0.001f);
            clickImage.raycastTarget = true;

            FallingBugClickArea clickArea = clickAreaObject.AddComponent<FallingBugClickArea>();
            clickArea.Configure(this);

            _clickCollider = clickAreaObject.AddComponent<BoxCollider2D>();
            _clickCollider.isTrigger = true;

            FallingBugColliderClick colliderClick = clickAreaObject.AddComponent<FallingBugColliderClick>();
            colliderClick.Configure(this);
        }

        _clickAreaRect.anchoredPosition = new Vector2(wordBounds.center.x, wordBounds.center.y);
        _clickAreaRect.sizeDelta = wordSize;
        if (_clickCollider != null)
        {
            _clickCollider.size = wordSize;
            _clickCollider.offset = Vector2.zero;
        }

        _label.raycastTarget = true;
    }

    public void EnableChaoticMovement(
        float minTurnInterval,
        float maxTurnInterval,
        float minHorizontalMultiplier,
        float maxHorizontalMultiplier,
        float minFallMultiplier,
        float maxFallMultiplier)
    {
        _chaoticMovement = true;
        _chaoticMinTurnInterval = Mathf.Max(0.05f, minTurnInterval);
        _chaoticMaxTurnInterval = Mathf.Max(_chaoticMinTurnInterval, maxTurnInterval);
        _chaoticMinHorizontalMultiplier = Mathf.Max(0f, minHorizontalMultiplier);
        _chaoticMaxHorizontalMultiplier = Mathf.Max(_chaoticMinHorizontalMultiplier, maxHorizontalMultiplier);
        _chaoticMinFallMultiplier = Mathf.Max(0.05f, minFallMultiplier);
        _chaoticMaxFallMultiplier = Mathf.Max(_chaoticMinFallMultiplier, maxFallMultiplier);
        ChooseChaoticDirection();
    }

    public void EnableCreepingMovement(float speed, float contactDistance)
    {
        _creepingMovement = true;
        _creepingSpeed = Mathf.Max(0f, speed);
        _creepingContactDistance = Mathf.Max(1f, contactDistance);
    }

    public void ResetDisplayText()
    {
        if (_label == null)
        {
            return;
        }

        _label.text = _displayText;
        _label.color = _baseColor;
    }

    public void SetTypingProgress(int matchedLength)
    {
        if (_label == null || string.IsNullOrEmpty(_displayText))
        {
            return;
        }

        matchedLength = Mathf.Clamp(matchedLength, 0, _displayText.Length);
        if (matchedLength <= 0)
        {
            ResetDisplayText();
            return;
        }

        string typedPart = _displayText.Substring(0, matchedLength);
        string remainingPart = _displayText.Substring(matchedLength);
        _label.text = $"<color=#7CFF7C>{typedPart}</color>{remainingPart}";
        _label.color = Color.white;
    }

    private void Update()
    {
        HandleColliderClick();

        RectTransform rect = GetComponent<RectTransform>();
        if (_creepingMovement)
        {
            UpdateCreepingMovement(rect);
            CheckForMissedRequiredBug(rect);
            return;
        }

        if (_chaoticMovement)
        {
            UpdateChaoticMovement(rect);
            CheckForMissedRequiredBug(rect);
            return;
        }

        rect.anchoredPosition += Vector2.down * fallSpeed * Time.deltaTime;
        if (moveHorizontally)
        {
            rect.anchoredPosition += Vector2.right * Mathf.Sin(Time.time * 5f + horizontalSeed) * fallSpeed * 0.6f * Time.deltaTime;
        }
        CheckForMissedRequiredBug(rect);
    }

    private void CheckForMissedRequiredBug(RectTransform rect)
    {
        if (_resolved || game == null || !IsFixableBug || !game.ShouldFailWhenRealBugIsMissed)
        {
            return;
        }

        RectTransform parentRect = rect.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        float bottomEdge = -parentRect.rect.height * 0.5f - rect.rect.height;
        if (rect.anchoredPosition.y < bottomEdge)
        {
            _resolved = true;
            game.OnRequiredBugMissed(this);
        }
    }

    private void HandleColliderClick()
    {
        if (!_clickable
            || _resolved
            || _clickCollider == null
            || _clickAreaRect == null
            || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        Camera eventCamera = GetEventCamera();
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _clickAreaRect,
                Input.mousePosition,
                eventCamera,
                out Vector3 worldPoint))
        {
            return;
        }

        if (_clickCollider.OverlapPoint(worldPoint))
        {
            TryClick();
        }
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    private void UpdateCreepingMovement(RectTransform rect)
    {
        FallingBug target = FindNearestRedBug(rect.anchoredPosition);
        if (target == null)
        {
            rect.anchoredPosition += Vector2.down * fallSpeed * Time.deltaTime;
            return;
        }

        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (targetRect == null)
        {
            return;
        }

        rect.anchoredPosition = Vector2.MoveTowards(
            rect.anchoredPosition,
            targetRect.anchoredPosition,
            _creepingSpeed * Time.deltaTime);

        if (Vector2.Distance(rect.anchoredPosition, targetRect.anchoredPosition)
            <= _creepingContactDistance)
        {
            ResolveCreeperContact(target);
        }
    }

    private FallingBug FindNearestRedBug(Vector2 currentPosition)
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            return null;
        }

        FallingBug[] bugs = parent.GetComponentsInChildren<FallingBug>(true);
        FallingBug nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (FallingBug bug in bugs)
        {
            if (bug == null || bug == this || !bug.IsRedBug || bug._resolved)
            {
                continue;
            }

            RectTransform bugRect = bug.GetComponent<RectTransform>();
            if (bugRect == null)
            {
                continue;
            }

            float distance = Vector2.SqrMagnitude(bugRect.anchoredPosition - currentPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = bug;
            }
        }

        return nearest;
    }

    private void ResolveCreeperContact(FallingBug redBug)
    {
        if (_resolved || redBug == null || redBug._resolved || game == null)
        {
            return;
        }

        _resolved = true;
        redBug._resolved = true;
        game.OnCreeperTouchedRed(this, redBug);
    }

    private void UpdateChaoticMovement(RectTransform rect)
    {
        if (Time.time >= _nextChaoticTurnTime)
        {
            ChooseChaoticDirection();
        }

        rect.anchoredPosition += new Vector2(
            _chaoticDirection * fallSpeed * _chaoticHorizontalMultiplier,
            -fallSpeed * _chaoticFallMultiplier) * Time.deltaTime;

        RectTransform parentRect = rect.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        float horizontalLimit = Mathf.Max(0f,
            parentRect.rect.width * 0.5f - rect.rect.width * 0.5f);
        Vector2 position = rect.anchoredPosition;
        if (position.x <= -horizontalLimit || position.x >= horizontalLimit)
        {
            position.x = Mathf.Clamp(position.x, -horizontalLimit, horizontalLimit);
            rect.anchoredPosition = position;
            _chaoticDirection *= -1f;
            _nextChaoticTurnTime = Time.time
                + Random.Range(_chaoticMinTurnInterval, _chaoticMaxTurnInterval);
        }
    }

    private void ChooseChaoticDirection()
    {
        _chaoticDirection = Random.value < 0.5f ? -1f : 1f;
        _chaoticHorizontalMultiplier = Random.Range(
            _chaoticMinHorizontalMultiplier,
            _chaoticMaxHorizontalMultiplier);
        _chaoticFallMultiplier = Random.Range(
            _chaoticMinFallMultiplier,
            _chaoticMaxFallMultiplier);
        _nextChaoticTurnTime = Time.time
            + Random.Range(_chaoticMinTurnInterval, _chaoticMaxTurnInterval);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TryClick();
    }

    public void TryClick()
    {
        if (!_clickable || game == null || _resolved)
        {
            return;
        }

        _resolved = true;
        game.OnLineClicked(gameObject, lineType);
    }

    public void FixWithKeyboard()
    {
        if (game == null || _resolved)
        {
            return;
        }

        _resolved = true;
        game.OnLineClicked(gameObject, lineType);
    }
}

public class FallingBugClickArea : MonoBehaviour, IPointerClickHandler
{
    private FallingBug _owner;

    public void Configure(FallingBug owner)
    {
        _owner = owner;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_owner != null)
        {
            _owner.TryClick();
        }
    }
}

public class FallingBugColliderClick : MonoBehaviour
{
    private FallingBug _owner;

    public void Configure(FallingBug owner)
    {
        _owner = owner;
    }

    private void OnMouseDown()
    {
        if (_owner != null)
        {
            _owner.TryClick();
        }
    }
}

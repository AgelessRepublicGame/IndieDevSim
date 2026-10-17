using TMPro;
using UnityEngine;

[RequireComponent(typeof(InteractiveObj))]
public class ColleagueDesk : MonoBehaviour
{
    public float completionInterval = 300f;
    [Range(0f, 1f)] public float minQuality = 0.4f;
    [Range(0f, 1f)] public float maxQuality = 0.5f;
    public int paymentCap = 1000;
    public GameObject colleagueSprite;
    public TextMeshPro statusText;
    [SerializeField] [Min(0.1f)] private float statusAppearDistance = 1f;

    private InteractiveObj interactiveObj;
    private DeveloperSimulation developer;
    private Transform speakBalloon;
    private ActiveOrder trackedOrder;
    private bool isPurchased;
    private bool hasCompletedOrder;
    private float completedQuality;
    private float completionTime;
    private bool upgradeSuppressed;
    private bool statusWindowVisible;

    private void Awake()
    {
        interactiveObj = GetComponent<InteractiveObj>();
        if (colleagueSprite != null)
        {
            colleagueSprite.SetActive(isPurchased);
        }
        CacheSpeakBalloon();
        SetStatusWindowVisible(false);
        RemoveHoverEffects();
    }

    private void OnEnable()
    {
        interactiveObj.OnInteractionAccept += Interact;
    }

    private void OnDisable()
    {
        interactiveObj.OnInteractionAccept -= Interact;
    }

    private void RemoveHoverEffects()
    {
        InteractiveHoverHighlight highlight = GetComponent<InteractiveHoverHighlight>();
        if (highlight != null)
        {
            Destroy(highlight);
        }

        HoverScaleEffect scaleEffect = GetComponent<HoverScaleEffect>();
        if (scaleEffect != null)
        {
            Destroy(scaleEffect);
        }
    }

    private void Update()
    {
        if (GameController.GameState == GameState.Stopped)
        {
            return;
        }

        developer ??= FindFirstObjectByType<DeveloperSimulation>();
        statusWindowVisible = IsPlayerNearColleague();
        SetStatusWindowVisible(statusWindowVisible);

        if (!isPurchased)
        {
            SetStatus("Not purchased");
            return;
        }

        if (upgradeSuppressed)
        {
            ClearWork();
            SetStatus("Unavailable for this order");
            return;
        }

        if (developer == null)
        {
            SetStatus("No player found");
            return;
        }

        if (!developer.HasActiveOrder())
        {
            ClearWork();
            SetStatus("Waiting for spell");
            return;
        }

        if (developer.IsCurrentTaskCompleted())
        {
            ClearWork();
            SetStatus("Waiting for next spell");
            return;
        }

        if (trackedOrder != developer.currentTask.order)
        {
            StartWork(developer.currentTask.order);
        }

        if (hasCompletedOrder)
        {
            SetStatus("Spell complete\nPick up at desk");
            return;
        }

        if (Time.time >= completionTime)
        {
            completedQuality = Random.Range(minQuality, maxQuality);
            hasCompletedOrder = true;
            SetStatus("Spell complete\nPick up at desk");
            return;
        }

        float remainingTime = Mathf.Max(0f, completionTime - Time.time);
        SetStatus("Colleague working\n" + FormatTime(remainingTime));
    }

    private void Interact(GameObject sender)
    {
        DeveloperSimulation player = sender.GetComponent<DeveloperSimulation>();
        if (player == null)
        {
            return;
        }

        developer = player;

        if (!isPurchased || upgradeSuppressed)
        {
            return;
        }

        if (!hasCompletedOrder || trackedOrder != player.currentTask.order)
        {
            return;
        }

        if (player.CompleteCurrentTask(completedQuality, paymentCap))
        {
            ClearWork();
            statusWindowVisible = IsPlayerNearColleague();
            SetStatusWindowVisible(statusWindowVisible);
        }
    }

    public void SetPurchased(bool purchased)
    {
        isPurchased = purchased;
        if (colleagueSprite != null)
        {
            colleagueSprite.SetActive(isPurchased);
        }

        if (!isPurchased)
        {
            ClearWork();
            statusWindowVisible = IsPlayerNearColleague();
            SetStatusWindowVisible(false);
            return;
        }

        developer ??= FindFirstObjectByType<DeveloperSimulation>();
        if (developer != null && developer.HasActiveOrder() && !developer.IsCurrentTaskCompleted())
        {
            StartWork(developer.currentTask.order);
        }
    }

    public void SetUpgradeSuppressed(bool suppressed)
    {
        upgradeSuppressed = suppressed;
        if (upgradeSuppressed)
        {
            ClearWork();
            SetStatus("Unavailable for this order");
        }
    }

    private void StartWork(ActiveOrder order)
    {
        trackedOrder = order;
        hasCompletedOrder = false;
        completedQuality = 0f;
        completionTime = Time.time + completionInterval;
    }

    private void ClearWork()
    {
        trackedOrder = null;
        hasCompletedOrder = false;
        completedQuality = 0f;
        completionTime = 0f;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void SetStatusWindowVisible(bool visible)
    {
        Transform balloon = GetSpeakBalloon();
        if (balloon != null)
        {
            balloon.gameObject.SetActive(false);
        }

        if (visible && statusText != null)
        {
            AdaptiveDialogWindow.Show(this, statusText.text, 10);
        }
        else
        {
            AdaptiveDialogWindow.Hide(this);
        }
    }

    private void CacheSpeakBalloon()
    {
        speakBalloon = transform.Find("speak_balloon");
        if (speakBalloon == null && statusText != null && statusText.transform.parent != null)
        {
            speakBalloon = statusText.transform.parent;
        }
    }

    private Transform GetSpeakBalloon()
    {
        if (speakBalloon == null)
        {
            CacheSpeakBalloon();
        }

        return speakBalloon;
    }

    private bool IsPlayerNearColleague()
    {
        if (developer == null)
        {
            developer = FindFirstObjectByType<DeveloperSimulation>();
        }

        if (developer == null)
        {
            return false;
        }

        float maxDistance = Mathf.Max(0.1f, statusAppearDistance);
        Vector2 playerPosition = developer.transform.position;
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders)
        {
            if (collider == null
                || !collider.enabled
                || !collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector2 closestPoint = collider.ClosestPoint(playerPosition);
            if (Vector2.Distance(playerPosition, closestPoint) <= maxDistance)
            {
                return true;
            }
        }

        return Vector2.Distance(playerPosition, transform.position) <= maxDistance;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(seconds);
        return (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
    }
}

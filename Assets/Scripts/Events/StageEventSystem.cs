using UnityEngine;

public static class StageEventSystem
{
    private static bool _usedThisStage;

    public static bool IsBlocking { get; private set; }
    public static bool CanTriggerThisStage => !_usedThisStage;

    public static void ResetForStage()
    {
        _usedThisStage = false;
        SetBlocking(false);
    }

    public static void MarkTriggered()
    {
        _usedThisStage = true;
    }

    public static void SetBlocking(bool blocking)
    {
        IsBlocking = blocking;
    }

    public static Vector2 GetInconvenientAnchor(string deviceType)
    {
        switch (deviceType)
        {
            case "Design":
                return new Vector2(0.5f, 0.22f);
            case "Programming":
                return new Vector2(0.5f, 0.58f);
            default:
                return new Vector2(0.62f, 0.42f);
        }
    }
}

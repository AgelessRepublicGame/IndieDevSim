using UnityEngine;

public static class NeighborHammerDrillSystem
{
    public const float KeywordFadeMultiplier = 1.1f;
    public const float BugSpeedMultiplier = 1.1f;
    public const float DragSpeedMultiplier = 0.9f;

    private static bool _active;

    public static bool IsActive => _active;

    public static void Activate()
    {
        _active = true;
    }

    public static void Clear()
    {
        _active = false;
    }

    public static string Title => "Neighbor's Hammer Drill";

    public static string Description =>
        "A neighbor started repairs at 8 am. You didn't get enough sleep.";

    public static string EffectSummary =>
        "Blurry vision; keywords fade faster; bugs fall faster; dragging feels slower (current order)";
}

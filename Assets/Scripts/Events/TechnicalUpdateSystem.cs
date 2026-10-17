using UnityEngine;

public static class TechnicalUpdateSystem
{
    public const float StageSpeedBonus = 0.2f;
    public const float DragSpeedMultiplier = 1.2f;
    public const float ProgrammingBugSpeedMultiplier = 0.8f;
    public const float DesignKeywordVisibleMultiplier = 1.2f;

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

    public static string Title => "Technical Update";

    public static string Description =>
        "Technical support said that they have updated your software. Now it works faster.";

    public static string EffectSummary =>
        "Next stage is 20% easier: Concept pieces move faster, Design hints last longer, Programming bugs fall slower";
}

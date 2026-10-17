using UnityEngine;

public static class BossLookingSystem
{
    public static bool IsActive { get; private set; }

    public static void Activate()
    {
        IsActive = true;
    }

    public static void Clear()
    {
        IsActive = false;
    }

    public static string Title => "The Boss Looked Into the Office";

    public static string Description =>
        "The boss came to check on how things are going. You need to look busy.";

    public static string EffectSummary =>
        "+10% experience if you complete the project without mistakes";
}

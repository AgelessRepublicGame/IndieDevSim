using UnityEngine;

public static class CatDroppedCupSystem
{
    public const float BlockSeconds = 3f;
    public const float FallAnimationSeconds = 0.65f;

    public static string Title => "The Cat Dropped the Cup";

    public static string Description =>
        "The office cat threw the cup off the table. The sound of a broken mug.";

    public static string EffectSummary => "Cannot click for 3 seconds (current stage only)";
}

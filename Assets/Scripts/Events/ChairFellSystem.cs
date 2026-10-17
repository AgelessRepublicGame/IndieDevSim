using UnityEngine;

public static class ChairFellSystem
{
    public const float BlockSeconds = 2f;
    public const float FallAnimationSeconds = 0.6f;

    public static string Title => "Chair Fell";

    public static string Description =>
        "You moved awkwardly and dropped a chair. The sound caught everyone's attention.";

    public static string EffectSummary => "Cannot click for 2 seconds (current stage only)";
}

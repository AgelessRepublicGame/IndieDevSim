using UnityEngine;

public static class MonitorBrokenSystem
{
    public const float BugContrastBlend = 0.45f;

    public static string Title => "The Monitor Is Broken";

    public static string Description => "The monitor began to blink and darken.";

    public static string EffectSummary =>
        "Screen flickers; Programming bug colors are harder to distinguish"
        + " (next programming stage or next order)";
}

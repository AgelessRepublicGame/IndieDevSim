using UnityEngine;

public static class FliesInvasionSystem
{
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

    public static string Title => "Invasion of Flies";

    public static string Description =>
        "Flies flew into the office. They're buzzing and annoying.";

    public static string EffectSummary =>
        "Visual distraction only (current order)";
}

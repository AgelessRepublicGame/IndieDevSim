using UnityEngine;

public static class TrafficJamSystem
{
    public static float TimePenaltySeconds { get; private set; } = 10f;

    private static bool _active;

    public static bool IsActive => _active;

    public static void Configure(float penaltySeconds)
    {
        TimePenaltySeconds = Mathf.Max(0f, penaltySeconds);
    }

    public static void Activate()
    {
        _active = true;
    }

    public static void Clear()
    {
        _active = false;
    }

    public static float GetAdjustedElapsedSeconds(float startTime)
    {
        if (!_active)
        {
            return Time.time - startTime;
        }

        return Time.time - startTime + TimePenaltySeconds;
    }

    public static string Title => "Traffic Jam";

    public static string Description =>
        "You've been stuck in traffic for 20 minutes. The time for ordering is reduced.";

    public static string EffectSummary =>
        "Order deadline is 10 seconds shorter (current order)";
}

using UnityEngine;

public static class SuddenCallSystem
{
    public const float BlockSeconds = 5f;
    public const float PhoneIconSeconds = 1f;

    public static string Title => "Mom's Call";

    public static string Description => "Mom is calling. I had to pick up the phone.";

    public static string EffectSummary => "Cannot interact for 5 seconds (current stage only)";
}

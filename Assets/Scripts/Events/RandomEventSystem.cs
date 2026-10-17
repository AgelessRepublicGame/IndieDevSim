using UnityEngine;

public enum RandomEventType
{
    None,
    ComputerOverheating,
    CoffeeMissing,
    SuddenCall,
    ChairFell,
    NeighborHammerDrill,
    MonitorBroken,
    FliesInvasion,
    CatDroppedCup,
    BossLooking,
    TrafficJam,
    TechnicalUpdate,
    ColleagueJoke
}

public enum RandomEventDuration
{
    NextProgrammingStage,
    NextOrder,
    CurrentOrder
}

public static class RandomEventSystem
{
    public const float ComputerOverheatingBugSpeedMultiplier = 1.2f;
    public const float CoffeeMissingBugSpeedMultiplier = 1.1f;
    public const float CoffeeMissingReactionSpeedMultiplier = 0.9f;
    public const float CoffeeMissingKeywordFadeMultiplier = 1.1f;
    public const float NeighborHammerDrillKeywordFadeMultiplier = 1.1f;
    public const float NeighborHammerDrillBugSpeedMultiplier = 1.1f;
    public const float NeighborHammerDrillDragSpeedMultiplier = 0.9f;

    private static RandomEventType _activeEvent = RandomEventType.None;
    private static RandomEventDuration _activeDuration = RandomEventDuration.NextProgrammingStage;

    public static RandomEventType ActiveEvent => _activeEvent;
    public static bool HasActiveEvent => _activeEvent != RandomEventType.None;

    public static void Activate(RandomEventType eventType, RandomEventDuration duration)
    {
        _activeEvent = eventType;
        _activeDuration = duration;
    }

    public static void Clear()
    {
        _activeEvent = RandomEventType.None;
    }

    public static bool IsCoffeeMissingActive()
    {
        return _activeEvent == RandomEventType.CoffeeMissing;
    }

    public static bool IsNeighborHammerDrillActive()
    {
        return NeighborHammerDrillSystem.IsActive;
    }

    public static bool IsMonitorBrokenActive()
    {
        return _activeEvent == RandomEventType.MonitorBroken;
    }

    public static Color ApplyProgrammingBugColor(Color color)
    {
        if (!IsMonitorBrokenActive())
        {
            return color;
        }

        Color mutedGray = new Color(0.55f, 0.55f, 0.55f, color.a);
        return Color.Lerp(color, mutedGray, MonitorBrokenSystem.BugContrastBlend);
    }

    public static float GetProgrammingBugSpeedMultiplier()
    {
        float multiplier = 1f;

        if (_activeEvent == RandomEventType.ComputerOverheating)
        {
            multiplier *= ComputerOverheatingBugSpeedMultiplier;
        }

        if (_activeEvent == RandomEventType.CoffeeMissing)
        {
            multiplier *= CoffeeMissingBugSpeedMultiplier;
        }

        if (NeighborHammerDrillSystem.IsActive)
        {
            multiplier *= NeighborHammerDrillBugSpeedMultiplier;
        }

        if (TechnicalUpdateSystem.IsActive)
        {
            multiplier *= TechnicalUpdateSystem.ProgrammingBugSpeedMultiplier;
        }

        return multiplier;
    }

    public static float GetDesignKeywordFadeMultiplier()
    {
        float multiplier = 1f;

        if (_activeEvent == RandomEventType.CoffeeMissing)
        {
            multiplier *= CoffeeMissingKeywordFadeMultiplier;
        }

        if (NeighborHammerDrillSystem.IsActive)
        {
            multiplier *= NeighborHammerDrillKeywordFadeMultiplier;
        }

        return multiplier;
    }

    public static float GetDesignKeywordVisibleMultiplier()
    {
        if (TechnicalUpdateSystem.IsActive)
        {
            return TechnicalUpdateSystem.DesignKeywordVisibleMultiplier;
        }

        return 1f;
    }

    public static float GetReactionSpeedMultiplier()
    {
        if (_activeEvent == RandomEventType.CoffeeMissing)
        {
            return CoffeeMissingReactionSpeedMultiplier;
        }

        return 1f;
    }

    public static float GetDragSpeedMultiplier()
    {
        float multiplier = 1f;

        if (NeighborHammerDrillSystem.IsActive)
        {
            multiplier *= NeighborHammerDrillDragSpeedMultiplier;
        }

        if (TechnicalUpdateSystem.IsActive)
        {
            multiplier *= TechnicalUpdateSystem.DragSpeedMultiplier;
        }

        return multiplier;
    }

    public static void OnProgrammingStageCompleted()
    {
        if (_activeEvent == RandomEventType.None)
        {
            return;
        }

        if (_activeDuration == RandomEventDuration.NextProgrammingStage)
        {
            Clear();
        }
    }

    public static void OnOrderSubmitted()
    {
        if (_activeEvent == RandomEventType.None)
        {
            return;
        }

        if (_activeDuration == RandomEventDuration.NextOrder)
        {
            Clear();
        }
    }

    public static void OnCurrentOrderSubmitted()
    {
        if (_activeDuration == RandomEventDuration.CurrentOrder)
        {
            Clear();
        }

        NeighborHammerDrillSystem.Clear();
        FliesInvasionSystem.Clear();
        TrafficJamSystem.Clear();
    }

    public static string GetEventTitle(RandomEventType eventType)
    {
        switch (eventType)
        {
            case RandomEventType.ComputerOverheating:
                return "Computer Overheating";
            case RandomEventType.CoffeeMissing:
                return "Coffee Missing";
            case RandomEventType.SuddenCall:
                return SuddenCallSystem.Title;
            case RandomEventType.ChairFell:
                return ChairFellSystem.Title;
            case RandomEventType.NeighborHammerDrill:
                return NeighborHammerDrillSystem.Title;
            case RandomEventType.MonitorBroken:
                return MonitorBrokenSystem.Title;
            case RandomEventType.FliesInvasion:
                return FliesInvasionSystem.Title;
            case RandomEventType.CatDroppedCup:
                return CatDroppedCupSystem.Title;
            case RandomEventType.BossLooking:
                return BossLookingSystem.Title;
            case RandomEventType.TrafficJam:
                return TrafficJamSystem.Title;
            case RandomEventType.TechnicalUpdate:
                return TechnicalUpdateSystem.Title;
            case RandomEventType.ColleagueJoke:
                return ColleagueJokeSystem.Title;
            default:
                return "";
        }
    }

    public static string GetEventDescription(RandomEventType eventType)
    {
        switch (eventType)
        {
            case RandomEventType.ComputerOverheating:
                return "The computer has overheated. Smoke is pouring out of the system unit.";
            case RandomEventType.CoffeeMissing:
                return "Someone drank your last coffee. You're angry and sleep-deprived.";
            case RandomEventType.SuddenCall:
                return SuddenCallSystem.Description;
            case RandomEventType.ChairFell:
                return ChairFellSystem.Description;
            case RandomEventType.NeighborHammerDrill:
                return NeighborHammerDrillSystem.Description;
            case RandomEventType.MonitorBroken:
                return MonitorBrokenSystem.Description;
            case RandomEventType.FliesInvasion:
                return FliesInvasionSystem.Description;
            case RandomEventType.CatDroppedCup:
                return CatDroppedCupSystem.Description;
            case RandomEventType.BossLooking:
                return BossLookingSystem.Description;
            case RandomEventType.TrafficJam:
                return TrafficJamSystem.Description;
            case RandomEventType.TechnicalUpdate:
                return TechnicalUpdateSystem.Description;
            case RandomEventType.ColleagueJoke:
                return ColleagueJokeSystem.Description;
            default:
                return "";
        }
    }

    public static string GetEventEffectSummary(RandomEventType eventType, RandomEventDuration duration)
    {
        switch (eventType)
        {
            case RandomEventType.ComputerOverheating:
                return "+20% bug speed in Programming"
                    + (duration == RandomEventDuration.NextOrder ? " (next order)" : " (next programming stage)");
            case RandomEventType.CoffeeMissing:
                return "-10% reaction speed; Design hints fade faster; Programming bugs move faster (next order)";
            case RandomEventType.SuddenCall:
                return SuddenCallSystem.EffectSummary;
            case RandomEventType.ChairFell:
                return ChairFellSystem.EffectSummary;
            case RandomEventType.NeighborHammerDrill:
                return NeighborHammerDrillSystem.EffectSummary;
            case RandomEventType.MonitorBroken:
                return MonitorBrokenSystem.EffectSummary
                    + (duration == RandomEventDuration.NextOrder ? " (next order)" : " (next programming stage)");
            case RandomEventType.FliesInvasion:
                return FliesInvasionSystem.EffectSummary;
            case RandomEventType.CatDroppedCup:
                return CatDroppedCupSystem.EffectSummary;
            case RandomEventType.BossLooking:
                return BossLookingSystem.EffectSummary;
            case RandomEventType.TrafficJam:
                return TrafficJamSystem.EffectSummary;
            case RandomEventType.TechnicalUpdate:
                return TechnicalUpdateSystem.EffectSummary;
            case RandomEventType.ColleagueJoke:
                return ColleagueJokeSystem.EffectSummary;
            default:
                return "";
        }
    }
}

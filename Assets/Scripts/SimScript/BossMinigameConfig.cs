
using UnityEngine;

public static class BossMinigameConfig
{
    public static bool IsTadG(ManagerId managerId)
    {
        return managerId == ManagerId.TadG;
    }

    public static bool IsHideoK(ManagerId managerId)
    {
        return managerId == ManagerId.HideoK;
    }

    public static bool IsFinalBoss(ManagerId managerId)
    {
        return IsTadG(managerId) || IsHideoK(managerId);
    }
}

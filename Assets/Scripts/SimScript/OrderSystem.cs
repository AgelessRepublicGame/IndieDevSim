using System;
using System.Text;
using UnityEngine;

public enum ManagerId
{
    Metel,
    CDProjec,
    Ubisoff,
    ElArt,
    RockStarr,
    Betesda,
    TadG,
    HideoK
}

public enum SpecialConditionType
{
    None,
    StrictTimer,
    Quality,
    GreenBugs,
    PurpleBugs
}

[Serializable]
public class OrderCondition
{
    public SpecialConditionType type;
    public float value;
    public string stringValue;
}

[Serializable]
public class OrderDefinition
{
    public ManagerId manager;
    public string genre;
    public int budget;
    public int deadline;
    public int minLevel = 1;
    public Milestone[] milestones;
    public OrderCondition[] conditions;
}

[Serializable]
public class Milestone
{
    public string title;
    public float progress;
    public string devicetype;
    public bool isCompleted;
    public GameObject minigamePrefab;
}

[Serializable]
public class ActiveOrder
{
    public ManagerId manager;
    public string genre;
    public int budget;
    public bool isTutorialOrder;
    public int guaranteedReward;
    public int paymentCap;
    public int deadline;
    public Milestone[] milestones;
    public OrderCondition[] conditions;

    public bool HasCondition(SpecialConditionType type)
    {
        return GetCondition(type) != null;
    }

    public OrderCondition GetCondition(SpecialConditionType type)
    {
        if (conditions == null)
        {
            return null;
        }

        foreach (OrderCondition condition in conditions)
        {
            if (condition != null && condition.type == type)
            {
                return condition;
            }
        }

        return null;
    }

    public float GetConditionValue(SpecialConditionType type, float defaultValue = 0f)
    {
        OrderCondition condition = GetCondition(type);
        return condition != null ? condition.value : defaultValue;
    }

    public string GetConditionString(SpecialConditionType type, string defaultValue = "")
    {
        OrderCondition condition = GetCondition(type);
        return condition != null && !string.IsNullOrEmpty(condition.stringValue)
            ? condition.stringValue
            : defaultValue;
    }

    public string GetConditionsText()
    {
        if (conditions == null || conditions.Length == 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        foreach (OrderCondition condition in conditions)
        {
            if (condition == null || condition.type == SpecialConditionType.None)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(",\n ");
            }

            builder.Append(condition.type);
            if (!string.IsNullOrEmpty(condition.stringValue))
            {
                builder.Append(": ");
                builder.Append(condition.stringValue);
            }
            else if (condition.value > 0f)
            {
                builder.Append(": ");
                builder.Append(condition.value);
                if (condition.type == SpecialConditionType.StrictTimer)
                {
                    builder.Append("s");
                }
            }
        }

        return builder.Length > 0 ? builder.ToString() : "None";
    }

    public static ActiveOrder FromDefinition(OrderDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        return new ActiveOrder
        {
            manager = definition.manager,
            genre = definition.genre,
            budget = definition.budget,
            isTutorialOrder = false,
            guaranteedReward = 0,
            paymentCap = 0,
            deadline = definition.deadline,
            milestones = CloneMilestones(definition.milestones),
            conditions = CloneConditions(definition.conditions)
        };
    }

    private static Milestone[] CloneMilestones(Milestone[] source)
    {
        if (source == null)
        {
            return Array.Empty<Milestone>();
        }

        Milestone[] cloned = new Milestone[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            Milestone milestone = source[i];
            cloned[i] = new Milestone
            {
                title = milestone.title,
                progress = 0f,
                devicetype = milestone.devicetype,
                isCompleted = false,
                minigamePrefab = milestone.minigamePrefab
            };
        }

        return cloned;
    }

    private static OrderCondition[] CloneConditions(OrderCondition[] source)
    {
        if (source == null)
        {
            return Array.Empty<OrderCondition>();
        }

        OrderCondition[] cloned = new OrderCondition[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            OrderCondition condition = source[i];
            if (condition == null)
            {
                continue;
            }

            cloned[i] = new OrderCondition
            {
                type = condition.type,
                value = condition.value,
                stringValue = condition.stringValue
            };
        }

        return cloned;
    }
}

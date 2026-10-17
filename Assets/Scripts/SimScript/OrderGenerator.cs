using System.Collections.Generic;
using UnityEngine;

public class OrderGenerator : MonoBehaviour
{
    public OrderTable orderTable;
    [SerializeField] private OrderDefinition tutorialOrder;
    [SerializeField] private int tutorialReward = 100;

    public ActiveOrder GenerateOrder(ManagerId manager, int playerLevel)
    {
        List<OrderDefinition> availableOrders = new List<OrderDefinition>();

        if (orderTable != null && orderTable.orders != null)
        {
            foreach (OrderDefinition order in orderTable.orders)
            {
                if (order != null && order.manager == manager && order.minLevel <= playerLevel)
                {
                    availableOrders.Add(order);
                }
            }
        }

        if (ShouldGenerateTutorialOrder(manager))
        {
            ActiveOrder trainingOrder = CreateTutorialOrder(availableOrders);
            if (trainingOrder != null)
            {
                return trainingOrder;
            }
        }

        if (availableOrders.Count == 0)
        {
            Debug.LogWarning("No available order found for manager " + manager + " at level " + playerLevel);
            return null;
        }

        OrderDefinition selected = availableOrders[Random.Range(0, availableOrders.Count)];
        return ActiveOrder.FromDefinition(selected);
    }

    private bool HasAdvancedConditions(OrderDefinition order)
    {
        if (order.conditions == null)
        {
            return false;
        }

        foreach (OrderCondition condition in order.conditions)
        {
            if (condition != null && condition.type != SpecialConditionType.None)
            {
                return true;
            }
        }

        return false;
    }

    private bool ShouldGenerateTutorialOrder(ManagerId manager)
    {
        return manager == ManagerId.Metel
            && PlayerPrefs.GetInt(TutorialController.TutorialCompleteKey, 0) == 0;
    }

    private ActiveOrder CreateTutorialOrder(List<OrderDefinition> fallbackOrders)
    {
        ActiveOrder order = tutorialOrder != null
            ? ActiveOrder.FromDefinition(tutorialOrder)
            : CreateTutorialOrderFromFallback(fallbackOrders);

        if (order == null)
        {
            return null;
        }

        order.isTutorialOrder = true;
        order.guaranteedReward = Mathf.Max(1, tutorialReward);
        order.budget = Mathf.Max(1, tutorialReward);
        order.paymentCap = 0;
        order.deadline = Mathf.Max(10, order.deadline);
        if (string.IsNullOrWhiteSpace(order.genre))
        {
            order.genre = "Training App";
        }

        return order;
    }

    private ActiveOrder CreateTutorialOrderFromFallback(List<OrderDefinition> fallbackOrders)
    {
        if (fallbackOrders == null || fallbackOrders.Count == 0)
        {
            return null;
        }

        ActiveOrder order = ActiveOrder.FromDefinition(fallbackOrders[0]);
        if (order == null)
        {
            return null;
        }

        order.genre = "Training App";
        return order;
    }
}

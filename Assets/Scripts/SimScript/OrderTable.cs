using UnityEngine;

[CreateAssetMenu(fileName = "OrderTable", menuName = "Orders/Order Table")]
public class OrderTable : ScriptableObject
{
    public OrderDefinition[] orders;
}

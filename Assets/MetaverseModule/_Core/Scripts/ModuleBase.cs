using UnityEngine;

public class ModuleBase : MonoBehaviour
{
    public Condition condition = Condition.None;

    [HideInInspector] public string itemName;
    [HideInInspector] public int itemAmount;
}
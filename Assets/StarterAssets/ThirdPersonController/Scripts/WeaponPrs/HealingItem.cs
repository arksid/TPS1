using UnityEngine;

public class HealingItem : Item
{
    [SerializeField] private int healAmount = 30; // È¸º¹·®
    public int HealAmount => healAmount;
}

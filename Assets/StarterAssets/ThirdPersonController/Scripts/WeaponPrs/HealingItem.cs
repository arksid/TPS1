using UnityEngine;

public class HealingItem : Item
{
    [SerializeField] private int healAmount = 30;
    public int HealAmount => healAmount;

    private void OnEnable()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.RegisterHealingItem(transform);
    }

    private void OnDisable()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.UnregisterTarget(transform);
    }
}

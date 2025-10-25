using UnityEngine;

public class StatModifierManager : MonoBehaviour
{
    public static StatModifierManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    // ✅ 공격 관련
    public void AddDamageMultiplier(float amount)
    {
        foreach (var weapon in Character.Instance.weaponSlots)
        {
            if (weapon != null)
                weapon.damage *= (1f + amount);
        }
    }

    public void AddFireRateMultiplier(float amount)
    {
        foreach (var weapon in Character.Instance.weaponSlots)
        {
            if (weapon != null)
                weapon.fireRate *= (1f - amount);
        }
    }

    public void AddCriticalChance(float amount)
    {
        Character.Instance.CriticalChance += amount;
    }

    // ✅ 방어 관련
    public void AddMaxShield(int amount)
    {
        Character.Instance.MaxShield += amount;
    }

    public void AddOnKillHeal(float amount)
    {
        Character.Instance.onKillHealAmount += amount;
    }

    public void AddMoveSpeed(float amount)
    {
        Character.Instance.moveSpeed += amount;
    }

    // ✅ 버프 효과
    public void TempDamageBuff(float amount, float duration)
    {
        StartCoroutine(BuffCoroutine(amount, duration));
    }

    private System.Collections.IEnumerator BuffCoroutine(float amount, float duration)
    {
        foreach (var weapon in Character.Instance.weaponSlots)
        {
            if (weapon != null)
                weapon.damage *= (1f + amount);
        }

        yield return new WaitForSeconds(duration);

        foreach (var weapon in Character.Instance.weaponSlots)
        {
            if (weapon != null)
                weapon.damage /= (1f + amount);
        }
    }

    public void TempMoveSpeedBuff(float amount, float duration)
    {
        StartCoroutine(SpeedBuffCoroutine(amount, duration));
    }

    private System.Collections.IEnumerator SpeedBuffCoroutine(float amount, float duration)
    {
        Character.Instance.moveSpeed += amount;
        yield return new WaitForSeconds(duration);
        Character.Instance.moveSpeed -= amount;
    }
}
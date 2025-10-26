using UnityEngine;

public class AugmentSystem : MonoBehaviour
{
    public static AugmentSystem Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void ApplyAugment(AugmentData augment)
    {
        switch (augment.type)
        {
            case AugmentType.Berserker:
                StatModifierManager.Instance.AddDamageMultiplier(augment.value); // 0.2f 등 CSV 값 사용
                break;

            case AugmentType.Slayer:
                // Slayer는 크확 %p 가산 (CSV에 0.15로 넣었다면 ApplyAugment에서 15로 변환하는 로직이 있으면 그걸 쓰세요)
                StatModifierManager.Instance.AddCriticalChance(augment.value <= 1f ? augment.value * 100f : augment.value);
                break;

            case AugmentType.Overdrive:
                StatModifierManager.Instance.AddFireRateMultiplier(augment.value);
                break;

            case AugmentType.IronSkin:
                // ❗ 이전: AddMaxShield(50) → 정식 메서드: AddShield(int)
                StatModifierManager.Instance.AddShield(Mathf.RoundToInt(augment.value));
                break;

            case AugmentType.Survivor:
                // ❗ 이전: AddOnKillHeal(10) → 정식 메서드: AddHealOnKill(float)
                StatModifierManager.Instance.AddHealOnKill(augment.value);
                break;

            case AugmentType.Retaliation:
                Character.Instance.enableRetaliation = true;
                break;

            case AugmentType.Predator:
                Character.Instance.enablePredator = true;
                break;

            case AugmentType.TriggerRush:
                Character.Instance.enableTriggerRush = true;
                break;

            case AugmentType.AdrenalSurge:
                Character.Instance.enableAdrenalSurge = true;
                break;

            case AugmentType.ChainReaction:
                Character.Instance.enableChainReaction = true;
                break;

            case AugmentType.Vengeance:
                Character.Instance.enableVengeance = true;
                break;

            case AugmentType.BulletFever:
                Character.Instance.enableBulletFever = true;
                break;

            case AugmentType.ColdRage:
                Character.Instance.enableColdRage = true;
                break;

            case AugmentType.SecondWind:
                Character.Instance.enableSecondWind = true;
                break;

            case AugmentType.UltCharger:
                Character.Instance.enableUltCharger = true;
                break;
        }

        Debug.Log($"[AugmentSystem] {augment.name} 적용 완료");
    }

    public void RemoveAugment(AugmentData augment)
    {
        switch (augment.type)
        {
            case AugmentType.Berserker:
                StatModifierManager.Instance.AddDamageMultiplier(-augment.value);
                break;

            case AugmentType.Slayer:
                StatModifierManager.Instance.AddCriticalChance(-(augment.value <= 1f ? augment.value * 100f : augment.value));
                break;

            case AugmentType.Overdrive:
                StatModifierManager.Instance.AddFireRateMultiplier(-augment.value);
                break;

            case AugmentType.IronSkin:
                // ❗ 이전: AddMaxShield(-50) → 정식 메서드: AddShield(int)
                StatModifierManager.Instance.AddShield(-Mathf.RoundToInt(augment.value));
                break;

            case AugmentType.Survivor:
                // ❗ 이전: AddOnKillHeal(-10) → 정식 메서드: AddHealOnKill(float)
                StatModifierManager.Instance.AddHealOnKill(-augment.value);
                break;

            case AugmentType.Retaliation:
                Character.Instance.enableRetaliation = false;
                break;

            case AugmentType.Predator:
                Character.Instance.enablePredator = false;
                break;

            case AugmentType.TriggerRush:
                Character.Instance.enableTriggerRush = false;
                break;

            case AugmentType.AdrenalSurge:
                Character.Instance.enableAdrenalSurge = false;
                break;

            case AugmentType.ChainReaction:
                Character.Instance.enableChainReaction = false;
                break;

            case AugmentType.Vengeance:
                Character.Instance.enableVengeance = false;
                break;

            case AugmentType.BulletFever:
                Character.Instance.enableBulletFever = false;
                break;

            case AugmentType.ColdRage:
                Character.Instance.enableColdRage = false;
                break;

            case AugmentType.SecondWind:
                Character.Instance.enableSecondWind = false;
                break;

            case AugmentType.UltCharger:
                Character.Instance.enableUltCharger = false;
                break;
        }

        Debug.Log($"[AugmentSystem] {augment.name} 제거 완료");
    }
}

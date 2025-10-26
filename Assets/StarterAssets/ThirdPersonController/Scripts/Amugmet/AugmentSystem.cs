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
            // ===== 즉시형(StatModifierManager 직접 가감) =====
            case AugmentType.Berserker:
                StatModifierManager.Instance.AddDamageMultiplier(augment.value); // 예: 0.2 → +20%
                break;

            case AugmentType.Slayer:
                StatModifierManager.Instance.AddCriticalChance(augment.value);   // 예: 15 → +15%p
                break;

            case AugmentType.Overdrive:
                StatModifierManager.Instance.AddFireRateMultiplier(augment.value); // 예: 0.2 → +20%
                break;

            case AugmentType.IronSkin:
                StatModifierManager.Instance.AddMaxShield(Mathf.RoundToInt(augment.value)); // 예: 50
                break;

            case AugmentType.Survivor:
                StatModifierManager.Instance.AddOnKillHeal(Mathf.RoundToInt(augment.value)); // 예: 10
                break;

            // ===== 조건부(값도 함께 세팅) =====
            case AugmentType.Retaliation:
                Character.Instance.enableRetaliation = true;
                // 필요 시 Retaliation 세부값도 만들었다면 여기서 세팅
                break;

            case AugmentType.Predator:
                Character.Instance.enablePredator = true;
                Character.Instance.predatorValue = augment.value;   // HP 50%↓ 때 데미지 +v
                break;

            case AugmentType.TriggerRush:
                Character.Instance.enableTriggerRush = true;
                Character.Instance.triggerRushValue = augment.value; // 처치 후 3초 이속 +v
                break;

            case AugmentType.AdrenalSurge:
                Character.Instance.enableAdrenalSurge = true;
                Character.Instance.adrenalSurgeValue = augment.value; // 명중시 공속 +v(스택/2초 유지)
                break;

            case AugmentType.ChainReaction:
                Character.Instance.enableChainReaction = true;
                // 필요 시 ChainReaction 세부값 세팅
                break;

            case AugmentType.Vengeance:
                Character.Instance.enableVengeance = true;
                Character.Instance.vengeanceValue = augment.value; // 피격 후 5초 데미지 +v
                break;

            case AugmentType.BulletFever:
                Character.Instance.enableBulletFever = true;
                // CSV에서 0.05 같은 비율이면 %p로 바꿔주기
                Character.Instance.bulletFeverValue = (augment.value <= 1f ? augment.value * 100f : augment.value);
                break;

            case AugmentType.ColdRage:
                Character.Instance.enableColdRage = true;
                // 마찬가지로 %p (최대 보너스치)
                Character.Instance.coldRageMaxBonus = (augment.value <= 1f ? augment.value * 100f : augment.value);
                break;

            case AugmentType.SecondWind:
                Character.Instance.enableSecondWind = true;
                Character.Instance.secondWindShield = Mathf.RoundToInt(augment.value); // 예: 50
                break;

            case AugmentType.UltCharger:
                Character.Instance.enableUltCharger = true;
                // Ult 명중충전량을 augment.value로 쓰고 싶다면 UltimateSkill쪽에서 참조하도록 확장
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
                StatModifierManager.Instance.AddCriticalChance(-augment.value);
                break;
            case AugmentType.Overdrive:
                StatModifierManager.Instance.AddFireRateMultiplier(-augment.value);
                break;
            case AugmentType.IronSkin:
                StatModifierManager.Instance.AddMaxShield(-Mathf.RoundToInt(augment.value));
                break;
            case AugmentType.Survivor:
                StatModifierManager.Instance.AddOnKillHeal(-Mathf.RoundToInt(augment.value));
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

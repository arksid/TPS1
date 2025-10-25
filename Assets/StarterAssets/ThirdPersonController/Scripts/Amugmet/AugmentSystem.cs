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
                StatModifierManager.Instance.AddDamageMultiplier(0.2f);
                break;

            case AugmentType.Slayer:
                StatModifierManager.Instance.AddCriticalChance(15f);
                break;

            case AugmentType.Overdrive:
                StatModifierManager.Instance.AddFireRateMultiplier(0.2f);
                break;

            case AugmentType.IronSkin:
                StatModifierManager.Instance.AddMaxShield(50);
                break;

            case AugmentType.Survivor:
                StatModifierManager.Instance.AddOnKillHeal(10);
                break;

            case AugmentType.Retaliation:
                // Character에서 피격 시 RestoreShield(10) 호출하도록 이벤트 연결 필요
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
                StatModifierManager.Instance.AddDamageMultiplier(-0.2f);
                break;

            case AugmentType.Slayer:
                StatModifierManager.Instance.AddCriticalChance(-15f);
                break;

            case AugmentType.Overdrive:
                StatModifierManager.Instance.AddFireRateMultiplier(-0.2f);
                break;

            case AugmentType.IronSkin:
                StatModifierManager.Instance.AddMaxShield(-50);
                break;

            case AugmentType.Survivor:
                StatModifierManager.Instance.AddOnKillHeal(-10);
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

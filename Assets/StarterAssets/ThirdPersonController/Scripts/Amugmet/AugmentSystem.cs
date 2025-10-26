using UnityEngine;

public class AugmentSystem : MonoBehaviour
{
    public static AugmentSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 씬 전환 유지가 필요하면 주석 해제:
        // DontDestroyOnLoad(gameObject);
    }

    public void ApplyAugment(AugmentData augment)
    {
        if (augment == null) return;

        var SM = StatModifierManager.Instance;
        var CH = Character.Instance;

        switch (augment.type)
        {
            // ===== 즉시형(배율/가산) =====
            case AugmentType.DamageBoost:
            case AugmentType.Berserker:
                SM?.AddDamageMultiplier(augment.value);             // ex) 0.2 → +20%
                break;

            case AugmentType.AttackSpeedBoost:
            case AugmentType.Overdrive:
                SM?.AddFireRateMultiplier(augment.value);
                break;

            case AugmentType.CriticalChanceUp:
            case AugmentType.Slayer:
                SM?.AddCriticalChance(augment.value);               // %p
                break;

            case AugmentType.MaxShieldUp:
            case AugmentType.IronSkin:
                SM?.AddMaxShield(Mathf.RoundToInt(augment.value));  // ex) 50
                break;

            case AugmentType.HealOnKill:
            case AugmentType.Survivor:
                SM?.AddOnKillHeal(Mathf.RoundToInt(augment.value)); // ex) 10
                break;

            case AugmentType.MoveSpeedBoost:
                SM?.AddMoveSpeedMultiplier(augment.value);
                break;

            case AugmentType.UltimateChargeBoost:
            case AugmentType.UltCharger:
                SM?.AddUltimateCharge(augment.value);
                if (CH != null) CH.enableUltCharger = true;
                break;

            case AugmentType.RecoilReduction:
            case AugmentType.RecoilTamer:
                SM?.AddRecoilReduction(augment.value);              // 0.2 → 반동 20% 감소
                break;

            case AugmentType.ExtendedMag:
                SM?.AddMagazineSizeBonus(Mathf.RoundToInt(augment.value)); // +N
                if (CH?.weapon != null)
                {
                    CH.weapon.ClampAmmoToMagazine();
                    CanvasManager.singleton?.UpdateAmmo(CH.weapon.ammo, CH.ammo?.amount ?? 0);
                }
                break;

            case AugmentType.StepAndGun:
                SM?.EnableStepAndGun(augment.value, 0.1f);
                break;

            case AugmentType.Penetrator:
                SM?.AddPenetrationBonus(Mathf.RoundToInt(augment.value)); // 관통 +N
                break;

            // ===== 토글/조건형(캐릭터 플래그) =====
            case AugmentType.Retaliation: if (CH != null) CH.enableRetaliation = true; break;

            case AugmentType.Predator:
                if (CH != null) { CH.enablePredator = true; CH.predatorValue = augment.value; }
                break;

            case AugmentType.TriggerRush:
                if (CH != null) { CH.enableTriggerRush = true; CH.triggerRushValue = augment.value; }
                break;

            case AugmentType.AdrenalSurge:
                if (CH != null) { CH.enableAdrenalSurge = true; CH.adrenalSurgeValue = augment.value; }
                break;

            case AugmentType.ChainReaction:
                if (CH != null) CH.enableChainReaction = true;
                break;

            case AugmentType.Vengeance:
                if (CH != null) { CH.enableVengeance = true; CH.vengeanceValue = augment.value; }
                break;

            case AugmentType.BulletFever:
                if (CH != null)
                {
                    CH.enableBulletFever = true;
                    CH.bulletFeverValue = (augment.value <= 1f ? augment.value * 100f : augment.value);
                }
                break;

            case AugmentType.ColdRage:
                if (CH != null)
                {
                    CH.enableColdRage = true;
                    CH.coldRageMaxBonus = (augment.value <= 1f ? augment.value * 100f : augment.value);
                }
                break;

            case AugmentType.SecondWind:
                if (CH != null)
                {
                    CH.enableSecondWind = true;
                    CH.secondWindShield = Mathf.RoundToInt(augment.value);
                }
                break;

            case AugmentType.QuickReload:
                if (CH != null) CH.enableQuickReload = true;
                break;

            case AugmentType.GearUp:
                if (CH != null) CH.enableGearUp = true;
                break;

            case AugmentType.Rend:
                if (CH != null) CH.enableRend = true;
                break;

            case AugmentType.Overheat:
                if (CH != null) CH.enableOverheat = true;
                break;

            default:
                Debug.Log($"[AugmentSystem] 적용 미정의: {augment.type}");
                break;
        }

        Debug.Log($"[AugmentSystem] {augment.augmentName} 적용 완료");
    }

    public void RemoveAugment(AugmentData augment)
    {
        if (augment == null) return;

        var SM = StatModifierManager.Instance;
        var CH = Character.Instance;

        switch (augment.type)
        {
            // ===== 즉시형 원복 =====
            case AugmentType.DamageBoost:
            case AugmentType.Berserker:
                SM?.AddDamageMultiplier(-augment.value);
                break;

            case AugmentType.AttackSpeedBoost:
            case AugmentType.Overdrive:
                SM?.AddFireRateMultiplier(-augment.value);
                break;

            case AugmentType.CriticalChanceUp:
            case AugmentType.Slayer:
                SM?.AddCriticalChance(-augment.value);
                break;

            case AugmentType.MaxShieldUp:
            case AugmentType.IronSkin:
                SM?.AddMaxShield(-Mathf.RoundToInt(augment.value));
                break;

            case AugmentType.HealOnKill:
            case AugmentType.Survivor:
                SM?.AddOnKillHeal(-Mathf.RoundToInt(augment.value));
                break;

            case AugmentType.MoveSpeedBoost:
                SM?.AddMoveSpeedMultiplier(-augment.value);
                break;

            case AugmentType.UltimateChargeBoost:
            case AugmentType.UltCharger:
                SM?.AddUltimateCharge(-augment.value);
                if (CH != null) CH.enableUltCharger = false;
                break;

            case AugmentType.RecoilReduction:
            case AugmentType.RecoilTamer:
                SM?.AddRecoilReduction(-augment.value);
                break;

            case AugmentType.ExtendedMag:
                SM?.AddMagazineSizeBonus(-Mathf.RoundToInt(augment.value));
                if (CH?.weapon != null)
                {
                    CH.weapon.ClampAmmoToMagazine();
                    CanvasManager.singleton?.UpdateAmmo(CH.weapon.ammo, CH.ammo?.amount ?? 0);
                }
                break;

            case AugmentType.StepAndGun:
                SM?.DisableStepAndGun();
                break;

            case AugmentType.Penetrator:
                SM?.AddPenetrationBonus(-Mathf.RoundToInt(augment.value));
                break;

            // ===== 토글 원복 =====
            case AugmentType.Retaliation: if (CH != null) CH.enableRetaliation = false; break;
            case AugmentType.Predator: if (CH != null) CH.enablePredator = false; break;
            case AugmentType.TriggerRush: if (CH != null) CH.enableTriggerRush = false; break;
            case AugmentType.AdrenalSurge: if (CH != null) CH.enableAdrenalSurge = false; break;
            case AugmentType.ChainReaction: if (CH != null) CH.enableChainReaction = false; break;
            case AugmentType.Vengeance: if (CH != null) CH.enableVengeance = false; break;
            case AugmentType.BulletFever: if (CH != null) CH.enableBulletFever = false; break;
            case AugmentType.ColdRage: if (CH != null) CH.enableColdRage = false; break;
            case AugmentType.SecondWind: if (CH != null) CH.enableSecondWind = false; break;

            case AugmentType.QuickReload:
                if (CH != null) CH.enableQuickReload = false;
                SM?.GrantQuickReload(1f, 0f); // 남은 가속 즉시 원복
                break;

            case AugmentType.GearUp: if (CH != null) CH.enableGearUp = false; break;
            case AugmentType.Rend: if (CH != null) CH.enableRend = false; break;
            case AugmentType.Overheat: if (CH != null) CH.enableOverheat = false; break;

            default:
                Debug.Log($"[AugmentSystem] 제거 미정의: {augment.type}");
                break;
        }
    }
}

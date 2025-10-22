using UnityEngine;
using System.Collections.Generic;

public class AugmentSystem : MonoBehaviour
{
    public static AugmentSystem Instance;

    private List<AugmentData> acquiredAugments = new List<AugmentData>();
    private Character player;
    private Weapon weapon;
    private UltimateSkill ultimateSkill;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        player = FindObjectOfType<Character>();
        weapon = FindObjectOfType<Weapon>();
        ultimateSkill = FindObjectOfType<UltimateSkill>();
    }

    private float GetRarityMultiplier(AugmentRarity rarity)
    {
        switch (rarity)
        {
            case AugmentRarity.Rare: return 1.5f;
            case AugmentRarity.Epic: return 2f;
            case AugmentRarity.Legendary: return 3f;
            default: return 1f;
        }
    }

    public void ApplyAugment(AugmentData data)
    {
        acquiredAugments.Add(data);

        float multiplier = GetRarityMultiplier(data.rarity);
        float finalValue = data.value * multiplier;

        switch (data.type)
        {
            case AugmentType.AttackSpeedBoost:
                weapon.fireRate *= (1f - finalValue);
                break;

            case AugmentType.DamageBoost:
                weapon.damage += finalValue;
                break;

            case AugmentType.HealOnKill:
                player.onKillHealAmount += finalValue;
                break;

            case AugmentType.ShieldBoost:
                player.AddShield(finalValue);
                break;

            case AugmentType.MoveSpeedBoost:
                player.moveSpeed *= (1f + finalValue);
                break;

            case AugmentType.SlowAura:
                player.EnableSlowAura(finalValue);
                break;

            case AugmentType.AutoReload:
                player.autoReloadOnKill = true;
                break;

            case AugmentType.UltimateChargeBoost:
                ultimateSkill.gaugePerHit *= finalValue;
                break;

            case AugmentType.RecoilReduction:
                weapon.ApplyRecoilMultiplier(1f - finalValue);
                break;

            case AugmentType.ExtraLoot:
                player.extraLootRate += finalValue;
                break;
            case AugmentType.MaxHealthUp:
                player.MaxHealth += (int)data.value;  // MaxHealth 증가
                player.Health = player.MaxHealth;     // 현재 체력도 최대치로 회복
                break;
        }
    }
}

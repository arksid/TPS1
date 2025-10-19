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

    public void ApplyAugment(AugmentData data)
    {
        acquiredAugments.Add(data);

        switch (data.type)
        {
            case AugmentType.AttackSpeedBoost:
                weapon.fireRate *= (1f - data.value);
                break;

            case AugmentType.DamageBoost:
                weapon.damage += data.value;
                break;

            case AugmentType.HealOnKill:
                player.onKillHealAmount += data.value;
                break;

            case AugmentType.ShieldBoost:
                player.AddShield(data.value);
                break;

            case AugmentType.MoveSpeedBoost:
                player.moveSpeed *= (1f + data.value);
                break;

            case AugmentType.SlowAura:
                player.EnableSlowAura(data.value);
                break;

            case AugmentType.AutoReload:
                player.autoReloadOnKill = true;
                break;

            case AugmentType.UltimateChargeBoost:
                ultimateSkill.gaugePerHit *= data.value;
                break;

            case AugmentType.RecoilReduction:
                weapon.ApplyRecoilMultiplier(1f - data.value);
                break;

            case AugmentType.ExtraLoot:
                player.extraLootRate += data.value;
                break;
        }
    }
}

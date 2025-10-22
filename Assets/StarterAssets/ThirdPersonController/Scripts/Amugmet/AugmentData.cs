using UnityEngine;

public enum AugmentRarity
{
    Normal,     // 일반
    Rare,       // 희귀
    Epic,       // 에픽
    Legendary   // 전설
}

public enum AugmentCategory
{
    Normal,     // 기본 특성 (공격력, 체력 증가 등)
    Special     // 특수 특성 (슬로우 오라, 자동장전 등)
}

[CreateAssetMenu(fileName = "NewAugment", menuName = "Augment System/Augment")]
public class AugmentData : ScriptableObject
{
    [Header("기본 정보")]
    public string augmentName;
    public string description;
    public Sprite icon;

    [Header("희귀도 및 분류")]
    public AugmentRarity rarity;
    public AugmentCategory category;    // 🟡 카테고리 추가

    [Header("효과 설정")]
    public AugmentType type;
    public float value;

    [Header("조건 설정")]
    public bool isStackable;
}

public enum AugmentType
{
    AttackSpeedBoost,
    DamageBoost,
    HealOnKill,
    ShieldBoost,
    MoveSpeedBoost,
    SlowAura,
    AutoReload,
    UltimateChargeBoost,
    RecoilReduction,
    ExtraLoot,
    MaxHealthUp        // 🆕 체력 증가 추가
}
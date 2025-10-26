using UnityEngine;

public enum AugmentRarity
{
    Common,     // ✅ CSV의 "Common" 값과 일치하도록 추가
    Normal,     // 기존 값 유지
    Rare,
    Epic,
    Legendary
}

public enum AugmentCategory
{
    Normal,     // 🟢 일반 특성
    Special,    // 🟡 특수 특성
    Offense,    // 공격 관련
    Utility,    // 유틸리티
    Movement,   // 이동 관련
    Defense     // 방어 관련
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
    MaxHealthUp,
    MaxShieldUp,          // 🛡 실드 최대치 증가
    CriticalChanceUp,   // 💥 치명타 확률 증가
     Berserker,
    Slayer,
    Overdrive,
    IronSkin,
    Survivor,
    Retaliation,
    Predator,
    TriggerRush,
    AdrenalSurge,
    ChainReaction,
    Vengeance,
    BulletFever,
    ColdRage,
    SecondWind,
    UltCharger,
    RecoilTamer,     // 반동 감소(퍼센트)
    ExtendedMag,
    StepAndGun,     // 이동 중 반동 감소 (recoil 감소 계수)
    QuickReload,    // 처치 후 5초 내 다음 리로드 속도 증가 (reloadSpeedMultiplier)
    Penetrator,     // 투사체 관통 가능 횟수 +N
    GearUp,         // 리로드 직후 3초간 데미지 증가
    Rend,           // 동일 적 1초내 3회 명중 → 그 적에게 5초간 추가피해
    Overheat,  // 장탄수(탄창 용량) +정수
}

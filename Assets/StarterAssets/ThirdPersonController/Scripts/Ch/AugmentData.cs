using UnityEngine;

[CreateAssetMenu(fileName = "NewAugment", menuName = "Augment System/Augment")]
public class AugmentData : ScriptableObject
{
    [Header("기본 정보")]
    public string augmentName;      // 증강 이름
    public string description;      // 설명
    public Sprite icon;             // UI 아이콘

    [Header("효과 설정")]
    public AugmentType type;        // 효과 종류
    public float value;             // 수치값 (예: 0.3 = 30%)

    [Header("조건 설정")]
    public bool isStackable;        // 중첩 가능 여부
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
    ExtraLoot
}

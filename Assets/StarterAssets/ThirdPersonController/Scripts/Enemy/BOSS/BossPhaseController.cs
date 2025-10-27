using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    public BossMonster boss;
    public BossWeaponMinigun minigun;
    public BossWeaponMissile missile;

    [Header("분노 임계치")]
    [Range(0.1f, 1f)] public float enragedHpRatio = 0.5f;
    private bool enraged = false;

    void Update()
    {
        if (!boss) return;
        float r = boss.HpRatio;

        if (!enraged && r <= enragedHpRatio)
        {
            enraged = true;
            minigun?.SetEnraged(true);
            missile?.SetEnraged(true);
            // 선택: 분노 시 전체 배수 살짝 상향
            boss.globalDamageMultiplier = 1.05f;
        }
    }
}

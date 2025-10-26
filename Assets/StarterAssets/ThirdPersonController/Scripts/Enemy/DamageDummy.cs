using UnityEngine;

/// <summary>
/// 간단 데미지 로그용 더미. 적 프리팹에 붙여두고,
/// 실제 데미지 계산 지점에서 ApplyDamage를 한 줄 호출하면 콘솔로 바로 확인 가능.
/// </summary>
public class DamageDummy : MonoBehaviour
{
    [Header("보이는 체력(디버그용)")]
    public float hp = 999999f;

    /// <param name="damage">최종 적용 데미지(배율/크리 계산 반영 후)</param>
    /// <param name="isCrit">치명타 여부</param>
    public void ApplyDamage(float damage, bool isCrit = false)
    {
        hp -= damage;
        Debug.Log($"[DamageDummy] {name} ← {damage:0.##} damage {(isCrit ? "(CRIT)" : "")} | HP: {hp:0.##}");
    }
}

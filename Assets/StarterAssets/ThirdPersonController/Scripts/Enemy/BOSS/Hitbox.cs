using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour, IHittable
{
    [Tooltip("이 히트박스가 연결된 DamageablePart")]
    public DamageablePart part;

    [Tooltip("부위별 데미지 배율 (예: 머리 2.0, 장갑 0.5)")]
    public float damageMultiplier = 1f;

    public void OnHit(int damage)
    {
        if (part == null) return;
        int finalDamage = Mathf.Max(0, Mathf.RoundToInt(damage * damageMultiplier));

        // 📝 참고 로그 (원본 데미지, 배율, 최종)
        Debug.Log($"[Hitbox] 부위='{part.gameObject.name}'  원본={damage}  배율={damageMultiplier}  최종={finalDamage}");

        part.ApplyDamage(finalDamage);
    }


    private void Reset()
    {
        // 자동으로 트리거 제안
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
}

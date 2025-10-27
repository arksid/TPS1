using UnityEngine;
using UnityEngine.Events;

public class BossMonster : MonoBehaviour
{
    [Header("Boss HP")]
    public int maxHP = 5000;
    [SerializeField] private int currentHP;

    [Header("Global Damage Rule")]
    [Tooltip("분노 등 상태에 따라 전체 배수를 조정하고 싶을 때 사용")]
    public float globalDamageMultiplier = 1f;

    [Header("Events")]
    public UnityEvent<int, int> onHpChanged;          // (current, max)
    public UnityEvent onBossDead;
    public UnityEvent<DamageablePart> onPartDamaged; // 부위가 맞을 때
    public UnityEvent<DamageablePart> onPartDestroyed;

    void Awake()
    {
        currentHP = maxHP;
        onHpChanged?.Invoke(currentHP, maxHP);
    }

    public float HpRatio => (maxHP <= 0) ? 0f : (float)currentHP / maxHP;

    /// <summary>부위에서 전달되는 데미지를 보스 전체에 적용</summary>
    public void ApplyDamage(int amount, DamageablePart fromPart)
    {
        if (currentHP <= 0) return;

        int applied = Mathf.Clamp(Mathf.RoundToInt(amount * globalDamageMultiplier), 0, currentHP);
        currentHP -= applied;

        Debug.Log($"[BossHit] fromPart='{(fromPart ? fromPart.name : "Unknown")}'  데미지={applied}  HP={currentHP}/{maxHP}");

        onHpChanged?.Invoke(currentHP, maxHP);
        onPartDamaged?.Invoke(fromPart);

        if (currentHP == 0)
        {
            onBossDead?.Invoke();
            // TODO: 사망 연출/드랍/씬 전환 등
        }
    }

    /// <summary>부위 파괴 알림 → 무기 비활성/상태 전환 등에 활용</summary>
    public void NotifyPartDestroyed(DamageablePart part)
    {
        onPartDestroyed?.Invoke(part);
        Debug.Log($"[BossPartBreak] part='{(part ? part.name : "Unknown")}'  BossHP={currentHP}/{maxHP}");
    }
}

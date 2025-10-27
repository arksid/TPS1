using UnityEngine;
using UnityEngine.Events;

public class DamageablePart : MonoBehaviour
{
    [Header("Part HP")]
    public int maxHP = 100;
    [SerializeField] private int currentHP;

    [Header("Link")]
    public BossMonster boss;   // 보스 전체 HP

    [Header("Damage Multipliers")]
    [Tooltip("부위가 멀쩡할 때: 이 부위를 맞추면 보스에게 함께 들어가는 배수(머리 2.0, 장갑 0.7 등)")]
    public float partToBossDamageMultiplier = 1f;

    [Tooltip("부위가 '파괴된 후' 이 자리를 맞추면 보스에게 전달될 배수(약점화). 예: 1.5")]
    public float destroyedForwardMultiplier = 1.2f;

    [Header("Destroyed Behavior")]
    [Tooltip("부위가 파괴된 뒤에도 그 자리를 맞추면 보스에게 데미지를 '계속' 전달할지?")]
    public bool forwardDamageAfterDestroyed = true;

    [Tooltip("파괴 시 콜라이더/히트박스를 꺼서 더 이상 맞지 않게 만들지? (약점을 유지하려면 false 권장)")]
    public bool disableColliderOnDestroyed = false;

    [Tooltip("파괴 시 이 모델로 교체(옵션)")]
    public GameObject destroyedSwap;

    [Header("Events / VFX hooks")]
    public UnityEvent onDamaged;
    public UnityEvent onDestroyed;

    void Awake() => currentHP = maxHP;

    /// <summary>
    /// Projectile → Hitbox → 여기로 최종 들어옴
    /// </summary>
    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;

        // 1) 아직 파괴 전이라면: 부위 HP를 먼저 깎고, 동시에 보스에게도 전달
        if (currentHP > 0)
        {
            int appliedToPart = Mathf.Clamp(amount, 0, currentHP);
            currentHP = Mathf.Max(0, currentHP - amount);

            onDamaged?.Invoke();
            Debug.Log($"[PartHit] 부위='{name}'  데미지={appliedToPart}  HP={currentHP}/{maxHP}");

            // 보스에 동시 전달
            if (boss != null && appliedToPart > 0)
            {
                int toBoss = Mathf.RoundToInt(appliedToPart * partToBossDamageMultiplier);
                boss.ApplyDamage(toBoss, this);
            }

            // 1-1) 지금 타격으로 부위가 막 파괴되었으면 파괴 처리
            if (currentHP == 0)
            {
                HandleDestroyed();
            }
            return;
        }

        // 2) 이미 파괴된 상태라면: 옵션에 따라 보스에게 '바로' 전달(약점화)
        if (forwardDamageAfterDestroyed && boss != null)
        {
            int toBoss = Mathf.RoundToInt(amount * destroyedForwardMultiplier);
            boss.ApplyDamage(toBoss, this);
            Debug.Log($"[PartHit-DESTROYED] 부위='{name}'  (직통)보스데미지={toBoss}  x{destroyedForwardMultiplier:0.00}");
        }
        // forwardDamageAfterDestroyed가 false면 아무 일도 안 함(말 그대로 '막힘')
    }

    private void HandleDestroyed()
    {
        onDestroyed?.Invoke();
        if (boss != null) boss.NotifyPartDestroyed(this);

        // 파괴 비주얼 스왑
        if (destroyedSwap != null)
        {
            destroyedSwap.SetActive(true);
            gameObject.SetActive(false); // 스왑 모델을 쓰는 경우, 이 파트 자체를 끈다
            return; // 이 경우엔 더 이상 맞을 수 없으니 주의!
        }

        // 콜라이더 유지/비활성 선택
        if (disableColliderOnDestroyed)
        {
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;
            // 약점을 유지하려면 false로 두는 게 좋다(계속 맞게).
        }

        // 스크립트는 남겨둔다(약점 구간 유지 위해)
        enabled = true;
    }

    public int GetCurrentHP() => currentHP;
    public bool IsDestroyed() => currentHP <= 0;

    void Reset()
    {
        if (boss == null) boss = GetComponentInParent<BossMonster>();
    }
}

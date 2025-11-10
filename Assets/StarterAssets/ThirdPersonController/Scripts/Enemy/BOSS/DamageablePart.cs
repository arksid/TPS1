using UnityEngine;
using UnityEngine.Events;

public class DamageablePart : MonoBehaviour
{
    // ---------- HP ----------
    [Header("HP")]
    public int maxHP = 50;
    int _curHP;
    bool _destroyed;

    // ---------- Boss Link ----------
    [Header("보스 연동")]
    public BossMonster ownerBoss;     // 보스 본체 Drag
    public string partId = "Core";    // 보스가 식별할 키
    // (AutoLinker 호환용)
    public BossMonster boss { get => ownerBoss; set => ownerBoss = value; }

    // ---------- 0HP 처리(무기 안 사라지게 기본값) ----------
    public enum ZeroHPBehavior
    {
        None,                 // 아무것도 안 함(무기/모양 유지)
        DisableHitbox,        // Hitbox만 꺼서 더 맞지 않게
        DisableBehaviours,    // 지정한 스크립트만 끔(기능 제한)
        DisableRenderers,     // 렌더러만 끔(모양만 숨김)
        DeactivateGameObject  // 통째로 비활성화(권장 X)
    }

    [Header("부서짐 처리(0HP)")]
    public ZeroHPBehavior zeroHPBehavior = ZeroHPBehavior.None;  // 기본: 유지
    [Tooltip("부서진 뒤에도 계속 보스에게 데미지를 전달할지")]
    public bool forwardDamageAfterBreak = true;                  // ★ 추가
    [Tooltip("DisableBehaviours일 때 끌 스크립트들")]
    public MonoBehaviour[] behavioursToDisable;
    [Tooltip("추가로 비활성화할 오브젝트(옵션)")]
    public GameObject[] extraObjectsToDeactivate;
    [Tooltip("부서지면 Hitbox를 꺼줄지(계속 맞게 하려면 끄세요=false)")]
    public bool disableHitboxesOnBreak = false;                  // ★ 기본 false 권장
    [Tooltip("레거시 호환: 켜져 있으면 무조건 전체 비활성화")]
    public bool destroyOnZero = false;                           // 무기 유지하려면 false

    // ---------- Events ----------
    [Header("이벤트")]
    public UnityEvent onHit;
    public UnityEvent onDestroyed;

    void Awake()
    {
        _curHP = maxHP;
    }

    public void ApplyDamage(int dmg)
    {
        // 부서진 이후에도 보스에 전달하고 싶으면, 리턴하지 않음
        if (!_destroyed)
        {
            _curHP = Mathf.Max(0, _curHP - dmg);
            onHit?.Invoke();

            // 매 타격 시 보스 HP에 반영 (오버플로우 고민 無: 항상 전체 전달)
            if (ownerBoss) ownerBoss.ApplyDamage(dmg, this);

            if (_curHP <= 0)
            {
                _destroyed = true;

                onDestroyed?.Invoke();
                if (ownerBoss) ownerBoss.NotifyPartDestroyed(this);

                HandleZeroHPBehavior();
            }
        }
        else
        {
            // 이미 부서진 상태
            if (forwardDamageAfterBreak)
            {
                // 계속 들어오는 타격은 그대로 보스에게 전달
                if (ownerBoss) ownerBoss.ApplyDamage(dmg, this);
            }
            // forwardDamageAfterBreak=false면 무시
        }
    }

    void HandleZeroHPBehavior()
    {
        if (destroyOnZero)
        {
            gameObject.SetActive(false);
            return;
        }

        switch (zeroHPBehavior)
        {
            case ZeroHPBehavior.None:
                // 아무것도 안 함 (무기/모양/로직 모두 유지)
                if (disableHitboxesOnBreak)
                {
                    var hitboxes0 = GetComponentsInChildren<Hitbox>(true);
                    foreach (var hb in hitboxes0) if (hb) hb.enabled = false;
                }
                break;

            case ZeroHPBehavior.DisableHitbox:
                var hitboxes = GetComponentsInChildren<Hitbox>(true);
                foreach (var hb in hitboxes) if (hb) hb.enabled = false;
                break;

            case ZeroHPBehavior.DisableBehaviours:
                if (behavioursToDisable != null)
                    foreach (var b in behavioursToDisable) if (b) b.enabled = false;
                if (disableHitboxesOnBreak)
                {
                    var hitboxes2 = GetComponentsInChildren<Hitbox>(true);
                    foreach (var hb in hitboxes2) if (hb) hb.enabled = false;
                }
                break;

            case ZeroHPBehavior.DisableRenderers:
                var rends = GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends) if (r) r.enabled = false;
                if (disableHitboxesOnBreak)
                {
                    var hitboxes3 = GetComponentsInChildren<Hitbox>(true);
                    foreach (var hb in hitboxes3) if (hb) hb.enabled = false;
                }
                break;

            case ZeroHPBehavior.DeactivateGameObject:
                gameObject.SetActive(false);
                break;
        }

        if (extraObjectsToDeactivate != null)
            foreach (var go in extraObjectsToDeactivate) if (go) go.SetActive(false);
    }

#if UNITY_EDITOR
    [ContextMenu("Test Damage 10")]
    void __DebugDamage() { ApplyDamage(10); }
#endif
}

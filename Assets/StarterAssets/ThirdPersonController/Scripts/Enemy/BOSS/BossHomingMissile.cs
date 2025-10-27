using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BossHomingMissile : MonoBehaviour
{
    [Header("Guidance")]
    public Transform target;                 // 기본: 플레이어
    public Transform owner;                  // 소유자(자기 충돌 무시)
    public float aimOffsetY = 1.2f;          // 머리/상단 조준
    public float turnRateDegPerSec = 140f;   // 선회 속도
    public float accel = 12f;
    public float maxSpeed = 22f;
    public float startSpeed = 8f;
    public float lifeTime = 12f;             // 발사체 수명(무기에서 보정 가능)

    [Header("Arming / Spawn Safe")]
    public float spawnForwardOffset = 0.2f;  // 스폰 위치 전방 보정
    public float armingDelay = 0.12f;        // 스폰 직후 충돌 무시

    [Header("Fuse / Explosion")]
    public float proximityFuse = 1.4f;       // 목표 근접 시 자동 기폭 거리
    public float explosionRadius = 3.5f;
    public float damage = 45f;
    public bool damageFalloff = true;        // 중심 세게, 바깥 약하게

    [Header("Collision")]
    public LayerMask hitMask = ~0;           // 데미지 줄 대상(플레이어 레이어 포함)
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("FX (optional)")]
    public GameObject explosionFx;
    public float explosionFxLife = 2f;
    public GameObject trailFx;

    [Header("Debug")]
    public bool enableLogging = false;

    // 무기에서 활성 수 관리를 위한 콜백
    public Action onDestroyed;

    private Rigidbody rb;
    private Collider col;
    private float currentSpeed;
    private float armedAt;
    private bool detonated;

    private bool IsArmed => Time.time >= armedAt;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        col.isTrigger = false;
    }

    void OnEnable()
    {
        detonated = false;
        currentSpeed = startSpeed;
        armedAt = Time.time + armingDelay;

        CancelInvoke();
        Invoke(nameof(SelfDestructTimeout), lifeTime);

        // 소유자 충돌 무시
        if (owner != null && col != null)
        {
            foreach (var oc in owner.GetComponentsInChildren<Collider>())
                if (oc && oc.enabled) Physics.IgnoreCollision(col, oc, true);
        }

        // 기본 타깃 자동
        if (target == null && Character.Instance != null)
            target = Character.Instance.transform;
    }

    public void Launch(Transform owner, Transform targetOverride = null)
    {
        this.owner = owner;
        if (targetOverride != null) target = targetOverride;

        // 스폰 위치 전방 보정
        transform.position += transform.forward * spawnForwardOffset;

        currentSpeed = startSpeed;
        rb.velocity = transform.forward * currentSpeed;
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 aimPoint = target.position + Vector3.up * aimOffsetY;
            Vector3 desiredDir = (aimPoint - transform.position).normalized;

            float maxDelta = turnRateDegPerSec * Time.fixedDeltaTime;
            Vector3 newDir = Vector3.RotateTowards(transform.forward, desiredDir, Mathf.Deg2Rad * maxDelta, 0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }

        currentSpeed = Mathf.Min(maxSpeed, currentSpeed + accel * Time.fixedDeltaTime);
        rb.velocity = transform.forward * currentSpeed;

        // 근접 신관
        if (IsArmed && target != null && !detonated)
        {
            float d = Vector3.Distance(transform.position, target.position);
            if (d <= proximityFuse)
            {
                Detonate(transform.position, -transform.forward);
                return;
            }
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (!IsArmed || detonated) return;

        // 소유자/자식 무시
        if (owner != null && (c.transform == owner || c.transform.IsChildOf(owner))) return;

        // 데미지 대상 레이어만 반응
        if (((1 << c.gameObject.layer) & hitMask) == 0) return;

        Vector3 p = (c.contacts.Length > 0) ? c.contacts[0].point : transform.position;
        Vector3 n = (c.contacts.Length > 0) ? c.contacts[0].normal : -transform.forward;
        Detonate(p, n);
    }

    private void Detonate(Vector3 pos, Vector3 normal)
    {
        if (detonated) return;
        detonated = true;

        // 폭발 FX
        if (explosionFx != null)
        {
            var fx = Instantiate(explosionFx, pos, Quaternion.LookRotation(normal));
            Destroy(fx, explosionFxLife);
        }

        // 반경 데미지(플레이어 기준)
        if (Character.Instance != null)
        {
            float dist = Vector3.Distance(pos, Character.Instance.transform.position);
            if (dist <= explosionRadius)
            {
                float final = damage;
                if (damageFalloff)
                {
                    float t = Mathf.Clamp01(dist / explosionRadius); // 0=중심,1=경계
                    final = Mathf.Lerp(damage, damage * 0.25f, t);
                }
                Character.Instance.ApplyDamage(gameObject, Character.Instance.transform, final);
            }
        }

        SelfDestruct();
    }

    private void SelfDestructTimeout()
    {
        if (enableLogging) Debug.Log("<Missile> Timeout");
        SelfDestruct();
    }

    private void SelfDestruct()
    {
        if (trailFx != null)
        {
            trailFx.transform.SetParent(null);
            Destroy(trailFx, 2.0f);
        }
        onDestroyed?.Invoke();
        Destroy(gameObject);
    }
}

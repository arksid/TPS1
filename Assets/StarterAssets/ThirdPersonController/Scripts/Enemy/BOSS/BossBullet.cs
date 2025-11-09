using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BossBullet : MonoBehaviour
{
    [Header("Ballistics")]
    public float speed = 30f;
    public float lifeTime = 12f;                 // 전체 수명(초)
    public float maxTravelDistance = 120f;       // 최대 비행 거리(미터)
    public bool useGravity = false;

    [Header("Damage")]
    public float damage = 12f;

    [Header("Aim")]
    [Tooltip("플레이어 머리/상단 조준 높이(m)")]
    public float aimOffsetY = 1.2f;

    [Header("Hit Settings")]
    [Tooltip("여기에 들어있는 레이어에게만 데미지/파괴 처리(그 외는 통과)")]
    public LayerMask hitMask = ~0;               // 인스펙터에서 Player/PlayerHitbox만 켜두세요
    public Transform owner;                      // 소유자(보스 루트 등)
    public bool acceptTrigger = false;           // 기본: 트리거 맞아도 무시
    public bool acceptCollision = true;          // 물리 충돌은 허용

    [Header("Anti-Tunnel (Sweep)")]
    public bool enableSweep = true;
    public float sweepRadius = 0.12f;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore; // 스윕에서 트리거 무시

    [Header("Spawn Safe")]
    [Tooltip("스폰 시 총구 앞으로 밀어낼 거리(m)")]
    public float spawnForwardOffset = 1.0f;      // 총구/메쉬와 즉시 충돌 방지
    [Tooltip("스폰 직후 이 시간 동안 충돌 무시(자기 몸과 즉시 충돌 방지)")]
    public float armingDelay = 0.12f;            // 무장 지연

    [Header("FX (optional)")]
    public GameObject hitEffect;
    public float hitEffectLife = 1.0f;

    [Header("Debug")]
    public bool enableLogging = false;

    private Rigidbody rb;
    private Collider col;
    private Vector3 prevPos;
    private Vector3 spawnPos;
    private bool hasHit = false;
    private float armedAt = 0f;                  // 이 시간이 지나야 충돌 허용
    private bool IsArmed => Time.time >= armedAt;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.useGravity = useGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = false;

        // 충돌형으로 두고, 트리거는 OnTrigger에서만 처리(기본 false)
        col.isTrigger = false;
    }

    void OnEnable()
    {
        hasHit = false;
        prevPos = transform.position;
        spawnPos = transform.position;
        armedAt = Time.time + armingDelay;

        CancelInvoke();
        Invoke(nameof(SelfDestructTimeout), lifeTime);
        if (enableLogging) Debug.Log("<BossBullet> Enable");
    }

    // ===== 발사(방향 지정) =====
    public void Fire(Vector3 direction, Transform owner)
    {
        this.owner = owner;
        SafeSpawnOffset();
        IgnoreOwnerCollision();

        transform.forward = direction.normalized;
        rb.velocity = transform.forward * speed;

        prevPos = transform.position;
        spawnPos = transform.position;

        if (enableLogging) Debug.Log($"<BossBullet> Fire dir={transform.forward}, vel={rb.velocity}, armedAt={armedAt:0.00}");
    }

    // ===== 발사(타깃 추정) =====
    public void FireAtTarget(Transform target, Transform owner)
    {
        this.owner = owner;
        SafeSpawnOffset();
        IgnoreOwnerCollision();

        Vector3 aimPoint = (target != null)
            ? target.position + Vector3.up * aimOffsetY
            : transform.position + transform.forward * 10f;

        Vector3 dir = (aimPoint - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(dir);
        rb.velocity = dir * speed;

        prevPos = transform.position;
        spawnPos = transform.position;

        if (enableLogging) Debug.Log($"<BossBullet> FireAtTarget aim={aimPoint}, vel={rb.velocity}, armedAt={armedAt:0.00}");
    }

    // ===== 거리 기반 수명 체크 =====
    void Update()
    {
        if (hasHit) return;
        float traveled = Vector3.Distance(spawnPos, transform.position);
        if (traveled >= maxTravelDistance)
        {
            if (enableLogging) Debug.Log("<BossBullet> SelfDestruct: MaxDistance");
            Destroy(gameObject);
        }
    }

    // ===== 스윕(터널링 방지) =====
    void FixedUpdate()
    {
        if (!IsArmed || !enableSweep || hasHit) { prevPos = transform.position; return; }

        Vector3 curPos = transform.position;
        Vector3 delta = curPos - prevPos;
        float dist = delta.magnitude;

        if (dist > 0.0001f)
        {
            RaycastHit hit;
            // ★ 변경: 스윕에서 hitMask만 검사(트리거 무시는 triggerInteraction로 제어)
            if (Physics.SphereCast(prevPos, sweepRadius, delta.normalized, out hit, dist, hitMask, triggerInteraction))
            {
                // 소유자면 무시
                if (owner != null && (hit.transform == owner || hit.transform.IsChildOf(owner)))
                {
                    prevPos = curPos;
                    return;
                }

                // 여기 도달했다면 이미 hitMask에 포함된 대상
                transform.position = hit.point;
                ProcessHit(hit.collider, hit.point, hit.normal);
                return;
            }
        }

        prevPos = curPos;
    }

    // ===== 물리 충돌 =====
    void OnCollisionEnter(Collision c)
    {
        if (!acceptCollision || hasHit) return;
        if (!IsArmed) return;
        if (owner != null && (c.transform == owner || c.transform.IsChildOf(owner))) return;

        // hitMask에 없는 레이어는 파괴/데미지 미처리(통과 느낌)
        if (((1 << c.gameObject.layer) & hitMask) == 0) return;

        var contact = c.contacts.Length > 0 ? c.contacts[0] : default;
        Vector3 p = contact.thisCollider ? contact.point : transform.position;
        Vector3 n = contact.thisCollider ? contact.normal : -transform.forward;
        ProcessHit(c.collider, p, n);
    }

    // ===== 트리거 충돌 =====
    void OnTriggerEnter(Collider other)
    {
        if (!acceptTrigger || hasHit) return;
        if (!IsArmed) return;
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner))) return;

        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        ProcessHit(other, transform.position, -transform.forward);
    }

    // ===== 공통 히트 처리 =====
    void ProcessHit(Collider hitCol, Vector3 point, Vector3 normal)
    {
        if (hasHit) return;
        hasHit = true;

        // 1) 플레이어 캐릭터에 직접 데미지(프로젝트 설계에 맞게)
        var player = hitCol.transform.root.GetComponent<Character>();
        if (player != null)
        {
            player.ApplyDamage(gameObject, hitCol.transform, damage);
            if (enableLogging) Debug.Log($"<BossBullet> Damage Player {damage} at {hitCol.name}");
        }
        else
        {
            // 2) IHittable도 지원(히트박스 등)
            var hittable = hitCol.GetComponentInParent<IHittable>();
            if (hittable != null)
            {
                hittable.OnHit(Mathf.RoundToInt(damage));
                if (enableLogging) Debug.Log($"<BossBullet> IHittable Hit {damage} at {hitCol.name}");
            }
        }

        // 이펙트
        if (hitEffect != null)
        {
            var fx = Instantiate(hitEffect, point, Quaternion.LookRotation(normal));
            Destroy(fx, hitEffectLife);
        }

        Destroy(gameObject);
    }

    private void SafeSpawnOffset()
    {
        // 총구 앞쪽으로 충분히 밀어서 스폰(자기/총구 충돌 방지)
        transform.position += transform.forward * spawnForwardOffset;
        if (transform.lossyScale == Vector3.zero) transform.localScale = Vector3.one;
    }

    private void IgnoreOwnerCollision()
    {
        if (!owner || !col) return;
        foreach (var oc in owner.GetComponentsInChildren<Collider>())
            if (oc && oc.enabled) Physics.IgnoreCollision(col, oc, true);
    }

    private void SelfDestructTimeout()
    {
        if (enableLogging) Debug.Log("<BossBullet> SelfDestruct: LifeTime");
        Destroy(gameObject);
    }
}

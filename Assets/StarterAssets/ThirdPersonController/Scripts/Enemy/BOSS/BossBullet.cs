using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BossBullet : MonoBehaviour
{
    [Header("Ballistics")]
    public float speed = 30f;
    public float lifeTime = 12f;                 // 넉넉하게
    public float maxTravelDistance = 120f;       // 거리 제한(충분히 크게)
    public bool useGravity = false;

    [Header("Damage")]
    public float damage = 12f;

    [Header("Aim")]
    [Tooltip("플레이어 머리/상단 조준 높이(m)")]
    public float aimOffsetY = 1.2f;

    [Header("Hit Settings")]
    [Tooltip("여기에 들어있는 레이어에게만 데미지/파괴 처리(그 외는 통과)")]
    public LayerMask hitMask = ~0;
    public Transform owner;
    public bool acceptTrigger = true;
    public bool acceptCollision = true;

    [Header("Anti-Tunnel (Sweep)")]
    public bool enableSweep = true;
    public float sweepRadius = 0.12f;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Spawn Safe")]
    [Tooltip("스폰 시 총구 앞으로 밀어낼 거리(m)")]
    public float spawnForwardOffset = 1.0f;      // 메쉬/총구와 겹침 방지
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

        // 충돌형으로 두고, 트리거는 OnTrigger에서 처리
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

    // ===== 발사 =====
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
            // 스윕은 '모든 레이어' 검사(~0), 이후 hitMask로 거르기
            if (Physics.SphereCast(prevPos, sweepRadius, delta.normalized, out hit, dist, ~0, triggerInteraction))
            {
                // 소유자면 무시
                if (owner != null && (hit.transform == owner || hit.transform.IsChildOf(owner)))
                {
                    prevPos = curPos;
                    return;
                }

                // 데미지 대상 레이어에만 반응
                if (((1 << hit.collider.gameObject.layer) & hitMask) != 0)
                {
                    transform.position = hit.point;
                    ProcessHit(hit.collider, hit.point, hit.normal);
                    return;
                }
            }
        }

        prevPos = curPos;
    }

    // ===== 충돌형 =====
    void OnCollisionEnter(Collision c)
    {
        if (!acceptCollision || hasHit) return;
        if (!IsArmed) return;
        if (owner != null && (c.transform == owner || c.transform.IsChildOf(owner))) return;

        // hitMask에 없는 레이어는 파괴도 데미지도 안 함(그냥 통과 느낌)
        if (((1 << c.gameObject.layer) & hitMask) == 0) return;

        var contact = c.contacts.Length > 0 ? c.contacts[0] : default;
        Vector3 p = contact.thisCollider ? contact.point : transform.position;
        Vector3 n = contact.thisCollider ? contact.normal : -transform.forward;
        ProcessHit(c.collider, p, n);
    }

    // ===== 트리거 =====
    void OnTriggerEnter(Collider other)
    {
        if (!acceptTrigger || hasHit) return;
        if (!IsArmed) return;
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner))) return;

        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        ProcessHit(other, transform.position, -transform.forward);
    }

    private void ProcessHit(Collider hitCol, Vector3 point, Vector3 normal)
    {
        if (hasHit) return;
        hasHit = true;

        // 플레이어 데미지
        var player = hitCol.transform.root.GetComponent<Character>();
        if (player != null)
        {
            player.ApplyDamage(gameObject, hitCol.transform, damage);
            if (enableLogging) Debug.Log($"<BossBullet> Damage Player {damage} at {hitCol.name}");
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

using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BossHomingMissile : MonoBehaviour, ISlowable
{
    [Header("Guidance")]
    public Transform target;                 // 기본: 플레이어
    public Transform owner;                  // 소유자(자기 충돌 무시)
    public float aimOffsetY = 1.2f;          // 머리/상단 조준
    public float turnRateDegPerSec = 140f;   // 선회 속도
    public float accel = 12f;
    public float maxSpeed = 22f;
    public float startSpeed = 8f;
    public float lifeTime = 12f;

    [Header("Spawn Safe")]
    public float spawnForwardOffset = 0.2f;  // 스폰 위치 보정
    public float armingDelay = 0.12f;        // 스폰 직후 충돌 무시

    [Header("Homing Delay")]
    [Tooltip("이 시간이 지난 뒤부터 유도 시작(지연 유도). 0이면 즉시 유도.")]
    public float homingDelay = 0f;

    [Header("Fuse / Explosion")]
    public float proximityFuse = 1.4f;       // 근접 신관
    public float explosionRadius = 3.5f;
    public float damage = 45f;
    public bool damageFalloff = true;

    [Header("Collision")]
    public LayerMask hitMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("FX (optional)")]
    public GameObject explosionFx;
    public float explosionFxLife = 2f;
    public GameObject trailFx;

    [Header("Debug")]
    public bool enableLogging = false;

    // ====== ULT 슬로우 ======
    [Header("ULT Slow")]
    [Range(0.05f, 1f)] public float localTimeScale = 1f; // 현재 적용 배수
    private float baseTurnRate, baseAccel, baseMaxSpeed;  // 원본 값 보관
    private float lastAppliedSlow = 1f;                   // 마지막 적용값(폴링 비교용)

    public Action onDestroyed;

    private Rigidbody rb;
    private Collider col;
    private float currentSpeed;
    private float armedAt;
    private float homingStartAt;
    private bool detonated;

    private bool IsArmed => Time.time >= armedAt;
    private bool CanHome => Time.time >= homingStartAt;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        col.isTrigger = false;

        // ULT 슬로우용 원본 값 저장
        baseTurnRate = turnRateDegPerSec;
        baseAccel = accel;
        baseMaxSpeed = maxSpeed;
    }

    void OnEnable()
    {
        detonated = false;
        currentSpeed = startSpeed;
        armedAt = Time.time + armingDelay;
        homingStartAt = Time.time + Mathf.Max(0f, homingDelay);

        CancelInvoke();
        Invoke(nameof(SelfDestructTimeout), lifeTime);

        if (owner != null && col != null)
        {
            foreach (var oc in owner.GetComponentsInChildren<Collider>())
                if (oc && oc.enabled) Physics.IgnoreCollision(col, oc, true);
        }

        if (target == null && Character.Instance != null)
            target = Character.Instance.transform;

        // 스폰 시점에 ULT 켜져 있으면 즉시 반영
        if (UltimateSkill.IsUltimateActive)
            SetLocalTimeScale(UltimateSkill.CurrentSlowFactor);
        else
            ResetLocalTimeScale();
    }

    public void Launch(Transform owner, Transform targetOverride = null)
    {
        this.owner = owner;
        if (targetOverride != null) target = targetOverride;

        transform.position += transform.forward * spawnForwardOffset;
        currentSpeed = startSpeed;
        rb.velocity = transform.forward * currentSpeed;

        // 발사 순간에도 한 번 더 보정
        if (UltimateSkill.IsUltimateActive)
            SetLocalTimeScale(UltimateSkill.CurrentSlowFactor);
    }

    void FixedUpdate()
    {
        // ★ ULT 상태 폴링(중간에 On/Off 되어도 비행 중 즉시 반영)
        float desiredSlow = UltimateSkill.IsUltimateActive ? UltimateSkill.CurrentSlowFactor : 1f;
        if (Mathf.Abs(desiredSlow - lastAppliedSlow) > 0.001f)
            SetLocalTimeScale(desiredSlow);

        // 유도 (지연 전에는 직진 유지)
        if (target != null && CanHome)
        {
            Vector3 aimPoint = target.position + Vector3.up * aimOffsetY;
            Vector3 desiredDir = (aimPoint - transform.position).normalized;

            float maxDelta = turnRateDegPerSec * Time.fixedDeltaTime; // ← 슬로우 반영된 선회속도
            Vector3 newDir = Vector3.RotateTowards(transform.forward, desiredDir, Mathf.Deg2Rad * maxDelta, 0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }

        // 가속(슬로우 반영된 accel, maxSpeed 사용)
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
        if (owner != null && (c.transform == owner || c.transform.IsChildOf(owner))) return;
        if (((1 << c.gameObject.layer) & hitMask) == 0) return;

        Vector3 p = (c.contacts.Length > 0) ? c.contacts[0].point : transform.position;
        Vector3 n = (c.contacts.Length > 0) ? c.contacts[0].normal : -transform.forward;
        Detonate(p, n);
    }

    private void Detonate(Vector3 pos, Vector3 normal)
    {
        if (detonated) return;
        detonated = true;

        if (explosionFx != null)
        {
            var fx = Instantiate(explosionFx, pos, Quaternion.LookRotation(normal));
            Destroy(fx, explosionFxLife);
        }

        if (Character.Instance != null)
        {
            float dist = Vector3.Distance(pos, Character.Instance.transform.position);
            if (dist <= explosionRadius)
            {
                float final = damage;
                if (damageFalloff)
                {
                    float t = Mathf.Clamp01(dist / explosionRadius);
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

    // ====== ISlowable 구현 ======
    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = Mathf.Clamp(scale, 0.05f, 1f);
        lastAppliedSlow = localTimeScale;

        // 원본 값을 기준으로 스케일 적용
        turnRateDegPerSec = baseTurnRate * localTimeScale;
        accel = baseAccel * localTimeScale;
        maxSpeed = baseMaxSpeed * localTimeScale;

        // 이미 높은 속도로 달리는 경우, 상한 재클램프
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
    }

    public void ResetLocalTimeScale()
    {
        SetLocalTimeScale(1f);
    }
}

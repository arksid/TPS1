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
    }

    public void Launch(Transform owner, Transform targetOverride = null)
    {
        this.owner = owner;
        if (targetOverride != null) target = targetOverride;

        transform.position += transform.forward * spawnForwardOffset;
        currentSpeed = startSpeed;
        rb.velocity = transform.forward * currentSpeed;
    }

    void FixedUpdate()
    {
        // 유도 (지연 전에는 직진 유지)
        if (target != null && CanHome)
        {
            Vector3 aimPoint = target.position + Vector3.up * aimOffsetY;
            Vector3 desiredDir = (aimPoint - transform.position).normalized;

            float maxDelta = turnRateDegPerSec * Time.fixedDeltaTime;
            Vector3 newDir = Vector3.RotateTowards(transform.forward, desiredDir, Mathf.Deg2Rad * maxDelta, 0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }

        // 가속
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
}

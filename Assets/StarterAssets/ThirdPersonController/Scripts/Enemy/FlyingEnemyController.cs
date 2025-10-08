using UnityEngine;
using System.Collections;

public class SmartFlyingEnemyController : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 6f;             // 비행 속도
    public float turnSpeed = 5f;             // 회전 속도
    public float hoverHeight = 5f;           // 플레이어 기준 비행 높이
    public float minDistance = 3f;           // 플레이어로부터 최소 거리
    public float maxDistance = 4f;           // 플레이어로부터 최대 거리
    public float moveInterval = 3f;          // 새로운 목표 갱신 주기 (초)
    public float randomOffset = 2f;          // 움직임의 랜덤성 정도

    [Header("공격 설정")]
    public float attackRange = 15f;          // 사격 거리
    public float attackCooldown = 2f;        // 사격 쿨타임
    public float projectileSpeed = 25f;
    public float projectileDamage = 15f;
    public Transform firePoint;

    [Header("체력 / 이펙트")]
    public int maxHealth = 100;
    private int currentHealth;
    public GameObject explosionEffect;

    private Transform player;
    private Rigidbody rb;
    private bool isDead = false;
    private bool canShoot = true;
    private bool moving = true;

    private Vector3 targetPos;               // 현재 목표 위치
    private float lastMoveTime;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 2f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        SetNewTargetPosition();
    }

    void FixedUpdate()
    {
        if (isDead || player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // 1️⃣ 플레이어 주위를 맴돌도록 이동
        if (Time.time - lastMoveTime > moveInterval)
            SetNewTargetPosition();

        MoveTowardTarget();

        // 2️⃣ 사격
        if (distToPlayer <= attackRange && canShoot)
            StartCoroutine(ShootRoutine());
    }

    // 🎯 목표 위치 갱신
    void SetNewTargetPosition()
    {
        lastMoveTime = Time.time;

        // 플레이어 기준 방향 랜덤 생성 (정수리 위 피하기)
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir.y = Mathf.Clamp(randomDir.y, -0.2f, 0.6f); // 너무 위나 아래로 가지 않도록 제한
        randomDir.Normalize();

        float randomDist = Random.Range(minDistance, maxDistance);
        Vector3 offset = randomDir * randomDist;

        targetPos = player.position + offset + Vector3.up * hoverHeight;
    }

    // ✈ 이동 처리
    void MoveTowardTarget()
    {
        Vector3 dir = (targetPos - transform.position).normalized;

        // 플레이어 정수리 바로 위 방향 금지
        Vector3 topDir = (player.position + Vector3.up * (hoverHeight + 1f) - transform.position).normalized;
        if (Vector3.Dot(dir, topDir) > 0.8f)
        {
            // 너무 위로 가려 하면 살짝 옆으로 회피
            dir = Quaternion.Euler(0, Random.Range(60f, 120f), 0) * dir;
        }

        // 부드럽게 회전
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

        // 이동
        rb.velocity = transform.forward * moveSpeed;
    }

    // 🔫 총알 발사
    IEnumerator ShootRoutine()
    {
        canShoot = false;

        if (firePoint != null && player != null)
        {
            Vector3 shootDir = (player.position + Vector3.up * 1.2f - firePoint.position).normalized;
            Quaternion rot = Quaternion.LookRotation(shootDir);

            GameObject bullet = PoolManager.Instance.Get("EnemyProjectile", firePoint.position, rot);
            EnemyProjectile proj = bullet.GetComponent<EnemyProjectile>();
            proj.Init(gameObject, shootDir, projectileSpeed, projectileDamage);
        }

        yield return new WaitForSeconds(attackCooldown);
        canShoot = true;
    }

    // 💥 피격 및 폭발 처리
    public void TakeDamage(float dmg)
    {
        currentHealth -= Mathf.RoundToInt(dmg);
        if (currentHealth <= 0 && !isDead)
            Explode();
    }

    void Explode()
    {
        if (isDead) return;
        isDead = true;

        if (explosionEffect)
        {
            GameObject fx = PoolManager.Instance.Get("ExplosionFX", transform.position, Quaternion.identity);
            StartCoroutine(ReturnFX(fx, 2f));
        }

        IEnumerator ReturnFX(GameObject fx, float delay)
        {
            yield return new WaitForSeconds(delay);
            PoolManager.Instance.Return(fx);
        }

        Invoke(nameof(ReturnToPool), 0.2f);
    }

    private void ReturnToPool()
    {
        isDead = false;
        currentHealth = maxHealth;
        PoolManager.Instance.Return(gameObject);
    }

    public void ResetEnemy()
    {
        isDead = false;
        currentHealth = maxHealth;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        gameObject.SetActive(true);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
#endif
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemyController : MonoBehaviour, IEnemyReward
{
    [Header("Target & Movement")]
    public Transform player;
    public float moveSpeed = 5f;
    public float stopDistance = 15f;  // 플레이어 반경 15m 이내로 접근하지 않음
    public float rotationSpeed = 5f;
    public float evadeAmplitude = 2f;
    public float evadeFrequency = 2f;

    [Header("Shooting")]
    public Transform firePoint;        // 총알이 나갈 위치(드론의 총구)
    public GameObject bulletPrefab;    // EnemyProjectile이 붙은 프리팹
    public float fireRate = 1.2f;      // 초당 발사 간격(예: 1.2초마다)
    public float bulletSpeed = 30f;    // 총알 속도
    public float bulletDamage = 15f;   // 총알 데미지
    public float fireRange = 35f;      // 사거리(가까울 때만 쏘게)
    public LayerMask lineOfSightMask;  // 시야 차단용(벽/지형 레이어)
    float _nextFireTime;

    [Header("Status")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    [Header("보상 설정")]
    [SerializeField] private int expReward = 10;
    [SerializeField] private float ultimateGaugeReward = 10f;
    public int ExpReward => expReward;

    [Header("Effect")]
    public GameObject deathEffect;

    private Rigidbody rb;
    private Vector3 initialEvadeOffset;

    public void GiveReward()
    {
        if (PlayerLevelSystem.Instance != null)
            PlayerLevelSystem.Instance.AddExp(expReward);

        var ult = FindObjectOfType<UltimateSkill>();
        if (ult != null)
            ult.AddGauge(ultimateGaugeReward);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;

        // 회피 이동 시작 지점
        initialEvadeOffset = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        );
    }

    private void Update()
    {
        if (isDead) return;

        if (player != null)
        {
            MoveAndEvade();
            TryAttack();
        }
    }

    private void MoveAndEvade()
    {
        Vector3 direction = (player.position - transform.position);
        float distance = direction.magnitude;

        // 플레이어를 바라보게 회전
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 일정 거리 이상일 때만 접근
        if (distance > stopDistance)
        {
            Vector3 evadeOffset = initialEvadeOffset * Mathf.Sin(Time.time * evadeFrequency) * evadeAmplitude;
            Vector3 moveDir = (direction.normalized + evadeOffset.normalized).normalized;
            rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
        }
    }

    void TryAttack()
    {
        if (player == null || bulletPrefab == null || firePoint == null) return;

        // 사거리 체크
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > fireRange) return;

        // 시야 막힘 체크(선택)
        Vector3 eye = firePoint.position;
        Vector3 toPlayer = (player.position + Vector3.up * 1.2f) - eye; // 상체 쪽 보정
        if (Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, fireRange, lineOfSightMask))
        {
            // 레이캐스트가 플레이어를 먼저 못 맞췄다면 막힌 것
            if (!hit.collider.CompareTag("Player")) return;
        }

        // 발사 타이밍
        if (Time.time < _nextFireTime) return;

        // 총구를 플레이어 방향으로 돌리기
        Vector3 dir = toPlayer.normalized;
        firePoint.rotation = Quaternion.LookRotation(dir);

        // 총알 생성 및 초기화 (★ EnemyProjectile 그대로 사용)
        GameObject go = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        var proj = go.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            // shooter=this.gameObject, 방향=firePoint.forward, 속도/데미지 전달
            proj.Init(this.gameObject, firePoint.forward, bulletSpeed, bulletDamage); // ← 핵심
        }
        else
        {
            Debug.LogWarning("[FlyingEnemy] bulletPrefab에 EnemyProjectile이 없습니다.");
        }

        _nextFireTime = Time.time + fireRate;
    }



    // FlyingEnemyController.cs
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // ✅ [추가] 히트 시 궁극기 게이지 올리기
        var ult = UltimateSkillCached.Instance;
        if (ult != null) ult.AddGauge(ult.GaugePerHit);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // EnemyController.cs (같은 네임스페이스/파일 끝에 추가)
    static class UltimateSkillCached
    {
        private static UltimateSkill _inst;
        public static UltimateSkill Instance
        {
            get
            {
                if (_inst == null) _inst = Object.FindObjectOfType<UltimateSkill>();
                return _inst;
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect != null)
        {
            GameObject fx = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        // ✅ 경험치 지급
        GiveReward();

        var dropSystem = GetComponent<EnemyDropSystem>();
        if (dropSystem != null)
            dropSystem.TryDropItemByWeight();

        Destroy(gameObject, 0.1f);
    }

    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }
}

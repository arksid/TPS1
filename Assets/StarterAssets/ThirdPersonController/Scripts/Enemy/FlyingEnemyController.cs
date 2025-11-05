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

    // ▼ 5점사 + 퍼짐 설정
    [Header("Burst Settings")]
    public int burstCount = 5;              // 몇 발씩?
    public float shotIntervalInBurst = 0.08f; // 점사 내 발사 간격(초)
    public float burstCooldown = 1.2f;      // 점사 후 쿨타임(초) = 다음 점사까지 기다림
    public float spreadDegrees = 4f;        // 퍼짐 각도(도)
    public float aimChestOffsetY = 1.2f;    // 플레이어 상체 보정 높이

    float _nextBurstTime = 0f;
    bool _isBursting = false;

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
            TryAttackBurst();  // ← 한 줄만 추가/교체
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

    void TryAttackBurst()
    {
        if (_isBursting) return;
        if (player == null || bulletPrefab == null || firePoint == null) return;

        // 사거리
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > fireRange) return;

        // 시야 막힘(선택): 레이가 플레이어를 먼저 못 맞추면 쏘지 않음
        Vector3 eye = firePoint.position;
        Vector3 toPlayer = (player.position + Vector3.up * aimChestOffsetY) - eye;
        if (Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, fireRange, lineOfSightMask))
        {
            if (!hit.collider.CompareTag("Player")) return;
        }

        // 점사 쿨타임
        if (Time.time < _nextBurstTime) return;

        StartCoroutine(BurstRoutine());
    }

    System.Collections.IEnumerator BurstRoutine()
    {
        _isBursting = true;

        for (int i = 0; i < burstCount; i++)
        {
            if (player == null) break;

            // 1) 기본 조준 방향(플레이어 상체)
            Vector3 baseDir = (player.position + Vector3.up * aimChestOffsetY - firePoint.position).normalized;

            // 2) 퍼짐 적용(명중률 낮추기)
            Vector3 shotDir = ApplySpread(baseDir, spreadDegrees);

            // 3) 총구 방향 정렬
            firePoint.rotation = Quaternion.LookRotation(shotDir);

            // 4) 발사체 생성 + 초기화(EnemyProjectile 사용)
            GameObject go = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            var proj = go.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.Init(this.gameObject, firePoint.forward, bulletSpeed, bulletDamage);
            }

            // 5) 점사 사이 간격
            if (i < burstCount - 1)
                yield return new WaitForSeconds(shotIntervalInBurst);
        }

        // 다음 점사까지 대기
        _nextBurstTime = Time.time + burstCooldown;

        _isBursting = false;
    }

    // 퍼짐(스프레드) 계산: 기준 방향을 살짝 흔들어주는 함수
    Vector3 ApplySpread(Vector3 dir, float degrees)
    {
        // dir을 바라보는 기준 회전
        Quaternion toTarget = Quaternion.LookRotation(dir);

        // 원 안에서 랜덤한 두 값(피치/요에 대응) -> -1~1 범위를 degrees 만큼 곱해 각도 오프셋 생성
        Vector2 r = Random.insideUnitCircle;        // r.x = yaw, r.y = pitch
        Quaternion spreadLocal = Quaternion.Euler(r.y * degrees, r.x * degrees, 0f);

        // (toTarget * spreadLocal) * forward = 퍼짐 방향
        Vector3 spreadDir = (toTarget * spreadLocal) * Vector3.forward;
        return spreadDir.normalized;
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
        MissionEvents.RaiseEnemyKilled(); // ★ 이 한 줄만 추가
        Destroy(gameObject);
    }

    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }
}

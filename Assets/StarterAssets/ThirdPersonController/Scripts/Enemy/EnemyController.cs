using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, ISlowable, IEnemyReward
{
    [Header("체력 설정")]
    public int maxHealth = 100;
    protected int currentHealth;

    // ✅ 추가됨
    protected float baseSpeed;
    private float localTimeScale = 1f;

    [Header("공격 관련 설정")]
    public Transform shootingPoint;
    public GameObject projectilePrefab;
    public float shootRange = 15f;
    public float shootCooldown = 1.5f;
    public float projectileSpeed = 25f;
    public float projectileDamage = 10f;
    protected float lastShootTime;

    [Header("보상 설정")]
    [SerializeField] private int expReward = 20;
    public int ExpReward => expReward;

    [Header("AI 관련")]
    public float rotationSpeed = 8f;
    protected NavMeshAgent agent;
    protected Transform playerTarget;
    protected Animator animator;

    [Header("폭발 이펙트")]
    public GameObject deathExplosionPrefab;
    public float explosionDestroyTime = 3f;

    [Header("우회 경로 탐색 설정")]
    public float flankSearchRadius = 8f;
    public int flankSearchSteps = 12;
    public float lineOfSightCheckHeight = 1.2f;
    [SerializeField] private LayerMask sightMask;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public void GiveReward()
    {
        if (PlayerLevelSystem.Instance != null)
            PlayerLevelSystem.Instance.AddExp(expReward);
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTarget = player.transform;

        lastShootTime = Time.time - shootCooldown;

        baseSpeed = agent != null ? agent.speed : 3.5f;

        // 🧠 궁극기 활성 중이면 슬로우 반영
        if (UltimateSkill.IsUltimateActive)
        {
            SetLocalTimeScale(UltimateSkill.CurrentSlowFactor);
        }

        // ✅ 시작 시에도 NavMesh 위로 스냅 (스폰 포인트 경계면 대비)
        if (agent != null)
        {
            EnsureOnNavMesh(agent, transform.position);
        }
    }

    // ✅ 궁극기에서 불리는 함수
    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = scale;

        if (agent != null)
            agent.speed = baseSpeed * localTimeScale;

        if (animator != null)
        {
            animator.speed = localTimeScale;
            float moveSpeed = agent != null ? agent.velocity.magnitude : 0f;
            animator.SetFloat("Speed", moveSpeed * localTimeScale);
        }
    }

    protected virtual void Update()
    {
        if (playerTarget == null || currentHealth <= 0) return;
        if (agent == null || !agent.isActiveAndEnabled) return;

        // ✅ 매 프레임, NavMesh 위 보장(안 되면 이번 프레임 스킵)
        if (!EnsureOnNavMesh(agent, transform.position))
            return;

        // 슬로우 반영
        agent.speed = baseSpeed * localTimeScale;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > shootRange)
        {
            agent.isStopped = false; // ✅ Resume 대체
            agent.SetDestination(playerTarget.position);
            if (animator != null) animator.SetBool("isMoving", true);
        }
        else
        {
            if (CanSeePlayer())
            {
                agent.isStopped = true;
                FaceTarget();

                if (animator != null) animator.SetBool("isMoving", false);

                if (Time.time - lastShootTime >= shootCooldown)
                {
                    Shoot();
                    lastShootTime = Time.time;
                }
            }
            else
            {
                agent.isStopped = false;
                FindFlankPositionAndMove();
                if (animator != null) animator.SetBool("isMoving", true);
            }
        }
    }

    // ✅ NavMesh 보장 함수 (못 찾으면 근처로 워프 시도)
    bool EnsureOnNavMesh(NavMeshAgent nav, Vector3 nearPos, float maxDist = 5f)
    {
        if (nav == null || !nav.isActiveAndEnabled) return false;
        if (nav.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(nearPos, out var hit, maxDist, NavMesh.AllAreas))
        {
            nav.Warp(hit.position);
            return nav.isOnNavMesh;
        }
        return false;
    }

    // 🧭 우회 경로 탐색
    private void FindFlankPositionAndMove()
    {
        if (playerTarget == null || agent == null) return;

        Vector3 dirToPlayer = (playerTarget.position - transform.position).normalized;
        Vector3 bestPoint = Vector3.zero;
        float bestDist = Mathf.Infinity;
        bool found = false;

        for (int i = 0; i < flankSearchSteps; i++)
        {
            float angle = (360f / flankSearchSteps) * i;
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 flankDir = rot * dirToPlayer;
            Vector3 candidatePos = playerTarget.position + flankDir * flankSearchRadius;

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                if (HasLineOfSight(navHit.position))
                {
                    float dist = Vector3.Distance(transform.position, navHit.position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestPoint = navHit.position;
                        found = true;
                    }
                }
            }
        }

        agent.SetDestination(found ? bestPoint : playerTarget.position);
    }

    // 🧭 플레이어 할당 (스포너에서 사용)
    public void SetPlayer(Transform player)
    {
        playerTarget = player;
    }

    public void OnBulletBlocked(Vector3 hitPos)
    {
        if (playerTarget == null || agent == null) return;

        // ✅ 먼저 NavMesh 위 보장
        if (!EnsureOnNavMesh(agent, transform.position)) return;

        if (CanSeePlayer())
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            return;
        }

        Vector3 dirToPlayer = (playerTarget.position - hitPos).normalized;
        Vector3 bestPoint = Vector3.zero;
        float bestDist = Mathf.Infinity;
        bool found = false;

        for (int i = 0; i < flankSearchSteps; i++)
        {
            float angle = (360f / flankSearchSteps) * i;
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 flankDir = rot * dirToPlayer;
            Vector3 candidatePos = hitPos + flankDir * flankSearchRadius;

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                if (HasLineOfSight(navHit.position))
                {
                    float dist = Vector3.Distance(transform.position, navHit.position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestPoint = navHit.position;
                        found = true;
                        if (CanSeePlayer()) break;
                    }
                }
            }
        }

        agent.isStopped = false;
        agent.SetDestination(found ? bestPoint : playerTarget.position);
    }

    // 👁️ 시야 체크
    private bool CanSeePlayer()
    {
        if (playerTarget == null || shootingPoint == null) return false;

        Vector3 start = shootingPoint.position + Vector3.up * 0.2f;
        Vector3 end = playerTarget.position + Vector3.up * 1.2f;
        Vector3 dir = (end - start).normalized;
        float dist = Vector3.Distance(start, end);

        Debug.DrawLine(start, end, Color.red, 0.1f);

        if (Physics.Raycast(start, dir, out RaycastHit hit, dist, sightMask))
            return hit.collider.CompareTag("Player");
        return false;
    }

    private bool HasLineOfSight(Vector3 from)
    {
        if (playerTarget == null) return false;

        Vector3 start = from + Vector3.up * lineOfSightCheckHeight;
        Vector3 end = playerTarget.position + Vector3.up * lineOfSightCheckHeight;
        Vector3 dir = (end - start).normalized;
        float dist = Vector3.Distance(start, end);

        if (Physics.Raycast(start, dir, out RaycastHit hit, dist, sightMask))
            return hit.collider.CompareTag("Player");
        return false;
    }

    private void FaceTarget()
    {
        Vector3 dir = (playerTarget.position - transform.position).normalized;
        dir.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
    }

    protected virtual void Shoot()
    {
        if (shootingPoint == null || projectilePrefab == null || playerTarget == null) return;

        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
        Vector3 shootDir = (targetPoint - shootingPoint.position).normalized;
        shootingPoint.rotation = Quaternion.LookRotation(shootDir);

        GameObject bullet = Instantiate(projectilePrefab, shootingPoint.position, shootingPoint.rotation);
        var proj = bullet.GetComponent<EnemyProjectile>();
        if (proj != null)
            proj.Init(gameObject, shootDir, projectileSpeed, projectileDamage);

        Collider bulletCol = bullet.GetComponent<Collider>();
        if (bulletCol != null)
        {
            Collider[] enemyCols = GetComponentsInChildren<Collider>();
            foreach (var c in enemyCols)
                if (c != null) Physics.IgnoreCollision(bulletCol, c, true);
        }

        Destroy(bullet, 5f);
    }

    // EnemyController.cs
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= Mathf.RoundToInt(damage);
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"[EnemyController] 데미지: {damage} / HP: {currentHealth} / {maxHealth}");

        if (CanvasManager.singleton != null)
            CanvasManager.singleton.ShowDamage(damage);

        // ✅ [추가] "맞췄을 때" 궁극기 게이지 올리기
        //   - 궁극기 중에는 AddGauge 내부에서 자동 차단됨
        var ult = UltimateSkillCached.Instance;                 // 아래 헬퍼 참조
        if (ult != null) ult.AddGauge(ult.GaugePerHit);

        if (currentHealth <= 0) Die();
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

    public void ResetLocalTimeScale()
    {
        localTimeScale = 1f;
        if (agent != null)
            agent.speed = baseSpeed;
    }

    protected virtual void Die()
    {
        if (agent != null) agent.enabled = false;
        if (animator != null) animator.enabled = false;

        if (deathExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, explosionDestroyTime);
        }

        GiveReward();

        var dropSystem = GetComponent<EnemyDropSystem>();
        if (dropSystem != null)
            dropSystem.TryDropItemByWeight();

        Destroy(gameObject);
        QuestEvents.EnemyDied(transform.position, gameObject);
        Character.Instance?.OnEnemyKilledHook();
        StatModifierManager.Instance?.OnEnemyKilled();
    }

    private void OnEnable()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.RegisterEnemy(transform);
    }

    private void OnDisable()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.UnregisterEnemy(transform);
    }
}

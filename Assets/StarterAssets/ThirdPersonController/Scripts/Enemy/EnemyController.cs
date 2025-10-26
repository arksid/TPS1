using UnityEngine;
using UnityEngine.AI;
public class EnemyController : MonoBehaviour, ISlowable, IEnemyReward
{
    [Header("체력 설정")]
    public int maxHealth = 100;
    protected int currentHealth;
    // ✅ 추가된 부분 시작
    protected float baseSpeed;              // 원래 속도 저장
    private float localTimeScale = 1f;     // 궁극기용 시간 배율
    // ✅ 추가된 부분 끝

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

        baseSpeed = agent.speed;

        // 🧠 궁극기가 발동 중이라면 즉시 슬로우 적용
        if (UltimateSkill.IsUltimateActive)
        {
            SetLocalTimeScale(UltimateSkill.CurrentSlowFactor);
        }
    }
    // ✅ 궁극기에서 불리는 함수만 추가
    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = scale;

        // NavMeshAgent 이동속도 조절
        if (agent != null)
        {
            agent.speed = baseSpeed * localTimeScale;
        }

        // 🧠 Animator도 슬로우
        if (animator != null)
        {
            // 1) 애니메이터 전체 재생속도 낮추기
            animator.speed = localTimeScale;

            // 2) Blend Tree에 사용하는 Speed 파라미터도 같이 낮추기
            float moveSpeed = agent.velocity.magnitude;
            animator.SetFloat("Speed", moveSpeed * localTimeScale);
        }
    }




    protected virtual void Update()
    {
        if (playerTarget == null || currentHealth <= 0) return;

        // 👇 슬로우 반영
        if (agent != null)
        {
            agent.speed = baseSpeed * localTimeScale;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > shootRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            if (animator != null)
                animator.SetBool("isMoving", true);
        }
        else
        {
            if (CanSeePlayer())
            {
                agent.isStopped = true;
                FaceTarget();

                if (animator != null)
                    animator.SetBool("isMoving", false);

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
                if (animator != null)
                    animator.SetBool("isMoving", true);
            }
        }
    }


    // 🧭 우회 경로 탐색 함수
    private void FindFlankPositionAndMove()
    {
        if (playerTarget == null) return;

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

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(candidatePos, out navHit, 2f, NavMesh.AllAreas))
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

        if (found)
        {
            agent.SetDestination(bestPoint);
            //Debug.Log($"[EnemyController] 우회 경로로 이동: {bestPoint}");
        }
        else
        {
            agent.SetDestination(playerTarget.position);
           // Debug.Log("[EnemyController] 우회 경로 없음 → 플레이어 직진");
        }
    }

    // 🧭 플레이어 할당 (스포너에서 사용)
    public void SetPlayer(Transform player)
    {
        playerTarget = player;
    }
    public void OnBulletBlocked(Vector3 hitPos)
    {
        if (playerTarget == null || agent == null) return;

        // 플레이어가 보이면 바로 플레이어 위치로 이동 시도
        if (CanSeePlayer())
        {
            if (agent != null && agent.isOnNavMesh && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTarget.position); // targetPosition → playerTarget.position
            }
            else
            {
                Debug.LogWarning("[EnemyController] NavMeshAgent가 NavMesh에 없어서 이동 명령을 무시했습니다.");
            }
            return;
        }

        // 플레이어가 안 보일 때는 우회 경로 탐색
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

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(candidatePos, out navHit, 2f, NavMesh.AllAreas))
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

        if (found)
        {
            if (agent != null && agent.isOnNavMesh && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(bestPoint);
            }
        }
        else
        {
            if (agent != null && agent.isOnNavMesh && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTarget.position);
            }
        }
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
    }


    // 👁️ 시야 체크 함수
    private bool CanSeePlayer()
    {
        if (playerTarget == null || shootingPoint == null) return false;

        Vector3 start = shootingPoint.position + Vector3.up * 0.2f;
        Vector3 end = playerTarget.position + Vector3.up * 1.2f;
        Vector3 dir = (end - start).normalized;
        float dist = Vector3.Distance(start, end);

        Debug.DrawLine(start, end, Color.red, 0.1f);

        if (Physics.Raycast(start, dir, out RaycastHit hit, dist, sightMask))
        {
            //Debug.Log($"[EnemyController] 시야 감지 대상: {hit.collider.name}");
            return hit.collider.CompareTag("Player");
        }
        else
        {
            return false;
        }
    }

    private bool HasLineOfSight(Vector3 from)
    {
        if (playerTarget == null) return false;

        Vector3 start = from + Vector3.up * lineOfSightCheckHeight;
        Vector3 end = playerTarget.position + Vector3.up * lineOfSightCheckHeight;
        Vector3 dir = (end - start).normalized;
        float dist = Vector3.Distance(start, end);

        if (Physics.Raycast(start, dir, out RaycastHit hit, dist, sightMask))
        {
            return hit.collider.CompareTag("Player");
        }
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

    public virtual void TakeDamage(float damage)
    {
        currentHealth -= Mathf.RoundToInt(damage);
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"[EnemyController] 데미지: {damage} / HP: {currentHealth} / {maxHealth}");

        if (CanvasManager.singleton != null)
            CanvasManager.singleton.ShowDamage(damage);

        if (currentHealth <= 0) Die();
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

        // ✅ 경험치 지급
        GiveReward();

        // 💥 아이템 드랍 처리
        var dropSystem = GetComponent<EnemyDropSystem>();
        if (dropSystem != null)
            dropSystem.TryDropItemByWeight();

        Destroy(gameObject);
        Character.Instance?.OnEnemyKilledHook();          // 트리거러시 등
        StatModifierManager.Instance?.OnEnemyKilled();    // 처치-힐 등

    }


    private void OnEnable()
    {
        // 🧭 적이 활성화될 때 레이더에 등록
        if (RadarManager.Instance != null)
        {
            RadarManager.Instance.RegisterEnemy(transform);
        }
    }

    private void OnDisable()
    {
        // 🧭 적이 비활성화될 때 레이더에서 제거
        if (RadarManager.Instance != null)
        {
            RadarManager.Instance.UnregisterEnemy(transform);
        }
    }

}

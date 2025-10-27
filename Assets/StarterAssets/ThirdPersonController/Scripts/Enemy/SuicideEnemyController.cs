using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SuicideEnemyController : MonoBehaviour, ISlowable, IEnemyReward
{
    [Header("기본 설정")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("AI 설정")]
    public float detectionRange = 20f;
    public float normalSpeed = 3.5f;
    public float chaseSpeed = 10f;
    public float chaseDuration = 2f;
    public float explosionRange = 2.5f;
    public float explosionDamage = 70f;
    public GameObject explosionEffect;

    [Header("보상 설정")]
    [SerializeField] private int expReward = 10;
    [SerializeField] private float ultimateGaugeReward = 10f;
    public int ExpReward => expReward;

    private NavMeshAgent agent;
    private Transform player;
    private bool isExploding = false;
    private bool isBoosting = false;

    // 궁극기용
    private float localTimeScale = 1f;
    private float baseNormalSpeed;
    private float baseChaseSpeed;

    private Animator animator;

    // ✅ 인터페이스 함수
    public void GiveReward()
    {
        if (PlayerLevelSystem.Instance != null)
            PlayerLevelSystem.Instance.AddExp(expReward);

        var ult = FindObjectOfType<UltimateSkill>();
        if (ult != null)
            ult.AddGauge(ultimateGaugeReward);
    }

    void Start()
    {
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        baseNormalSpeed = normalSpeed;
        baseChaseSpeed = chaseSpeed;

        // ✅ 스폰 직후, 근처 NavMesh로 샘플→Warp (경계/공중 대비)
        if (agent != null)
        {
            EnsureOnNavMesh(agent, transform.position, 8f);
            agent.speed = baseNormalSpeed * localTimeScale;
            agent.isStopped = false; // Resume() 대신
        }

        FindPlayer();

        if (UltimateSkill.IsUltimateActive)
            SetLocalTimeScale(UltimateSkill.CurrentSlowFactor);
    }

    void Update()
    {
        if (player == null) { FindPlayer(); return; }
        if (isExploding) return;
        if (agent == null || !agent.isActiveAndEnabled) return;

        // ✅ 매 프레임 NavMesh 위 보장 (안 되면 이번 프레임 스킵)
        if (!EnsureOnNavMesh(agent, transform.position, 8f))
            return;

        agent.speed = (isBoosting ? baseChaseSpeed : baseNormalSpeed) * localTimeScale;
        agent.isStopped = false; // Resume() 대신

        // ✅ 이제 안전하게 목적지 설정
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= detectionRange && !isBoosting)
            StartCoroutine(SpeedBoostRoutine());

        if (dist <= explosionRange)
            Explode();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetFloat("Speed", agent.velocity.magnitude / Mathf.Max(0.01f, baseNormalSpeed));
        }
    }
    /// <summary>
    /// NavMesh 위가 아니면 근처에서 샘플링 후 Warp로 올려놓는다.
    /// 성공 시 true, 실패 시 false.
    /// </summary>
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


    IEnumerator SpeedBoostRoutine()
    {
        isBoosting = true;
        float t = 0f;
        while (t < chaseDuration)
        {
            t += Time.deltaTime * localTimeScale;
            yield return null;
        }
        isBoosting = false;
    }

    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = Mathf.Max(0.01f, scale);

        if (agent != null)
            agent.speed = (isBoosting ? baseChaseSpeed : baseNormalSpeed) * localTimeScale;
    }

    public void ResetEnemy()
    {
        isExploding = false;
        currentHealth = maxHealth;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.speed = baseNormalSpeed * localTimeScale;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        gameObject.SetActive(true);
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    // SuicideEnemyController.cs
    public void TakeDamage(float dmg)
    {
        if (isExploding) return;

        currentHealth -= Mathf.RoundToInt(dmg);

        if (CanvasManager.singleton != null)
            CanvasManager.singleton.ShowDamage(dmg);

        Debug.Log($"[SuicideEnemy] 데미지 {dmg} 받음 / 남은 체력: {currentHealth}/{maxHealth}");

        // ✅ [추가] 히트 시 궁극기 게이지 올리기
        var ult = UltimateSkillCached.Instance;
        if (ult != null) ult.AddGauge(ult.GaugePerHit);

        if (currentHealth <= 0) Explode();
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


    void Explode()
    {
        if (isExploding) return;
        isExploding = true;

        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRange);
        foreach (Collider c in hits)
        {
            if (c.CompareTag("Player"))
            {
                Character ch = c.GetComponent<Character>() ?? c.GetComponentInParent<Character>();
                if (ch != null)
                    ch.ApplyDamage(null, transform, explosionDamage);
            }
        }

        var dropSystem = GetComponent<EnemyDropSystem>();
        if (dropSystem != null) dropSystem.TryDropItemByWeight();

        // ✅ 인터페이스 기반 보상 지급
        GiveReward();

        Destroy(gameObject, 0.1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isExploding) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Explode();
            return;
        }

        Projectile p = collision.gameObject.GetComponent<Projectile>();
        if (p != null)
        {
            Debug.Log($"[SuicideEnemy] 총알({p.name})에 맞음 - 데미지: {p.damage}");
            TakeDamage(p.damage);
            if (p.gameObject != null) Destroy(p.gameObject);
        }
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

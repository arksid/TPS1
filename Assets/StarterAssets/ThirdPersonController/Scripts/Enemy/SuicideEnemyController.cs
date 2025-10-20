using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SuicideEnemyController : MonoBehaviour, ISlowable
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

    private NavMeshAgent agent;
    private Transform player;
    private bool isExploding = false;
    private bool isBoosting = false;

    // 궁극기용
    private float localTimeScale = 1f;   // 궁극기 배율 (1=정상, 0.2=슬로우)
    private float baseNormalSpeed;
    private float baseChaseSpeed;

    // (선택) 애니메이터가 있다면 참고해서 속도 파라미터 갱신 가능
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 원본 속도 저장
        baseNormalSpeed = normalSpeed;
        baseChaseSpeed = chaseSpeed;

        // 시작 속도 적용
        if (agent != null)
        {
            agent.speed = baseNormalSpeed * localTimeScale;
            agent.isStopped = false;
        }

        FindPlayer();

        // 궁극기 도중에 스폰되면 즉시 슬로우 반영
        if (UltimateSkill.IsUltimateActive)
            SetLocalTimeScale(UltimateSkill.CurrentSlowFactor);
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }
        if (isExploding) return;

        // 🐢 매 프레임 현재 상태(부스트 여부)에 맞는 속도 유지
        if (agent != null)
            agent.speed = (isBoosting ? baseChaseSpeed : baseNormalSpeed) * localTimeScale;

        float dist = Vector3.Distance(transform.position, player.position);
        agent.SetDestination(player.position);

        if (dist <= detectionRange && !isBoosting)
            StartCoroutine(SpeedBoostRoutine());

        if (dist <= explosionRange)
            Explode();

        // (선택) 애니 동기화: 루트모션 쓰지 않는 걸 권장
        if (animator != null)
        {
            animator.applyRootMotion = false;
            // Speed 파라미터를 NavMeshAgent 실제 속도에 맞춰 동기화(움찔 방지)
            animator.SetFloat("Speed", agent.velocity.magnitude / Mathf.Max(0.01f, baseNormalSpeed));
        }
    }

    // 🔁 부스트 코루틴: 시간도 슬로우를 따르도록 직접 누적 방식 사용
    IEnumerator SpeedBoostRoutine()
    {
        isBoosting = true;

        float t = 0f;
        while (t < chaseDuration)
        {
            t += Time.deltaTime * localTimeScale; // 궁극기 중엔 더 천천히 흐름
            yield return null;
        }

        isBoosting = false;
    }

    // 🧠 궁극기에서 호출됨
    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = Mathf.Max(0.01f, scale); // 0 방지

        if (agent != null)
            agent.speed = (isBoosting ? baseChaseSpeed : baseNormalSpeed) * localTimeScale;

        if (animator != null)
        {
            animator.applyRootMotion = false; // NavMeshAgent와 충돌 방지
            // animator.speed는 1로 두고, 위 Update에서 Speed 파라미터만 동기화
        }
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

    public void TakeDamage(float dmg)
    {
        if (isExploding) return;

        currentHealth -= Mathf.RoundToInt(dmg);

        // 💥 데미지 UI 표시 추가
        if (CanvasManager.singleton != null)
        {
            CanvasManager.singleton.ShowDamage(dmg);
        }

        Debug.Log($"[SuicideEnemy] 데미지 {dmg} 받음 / 남은 체력: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Explode();
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

        // 💥 공통 드랍 시스템 호출
        var dropSystem = GetComponent<EnemyDropSystem>();
        if (dropSystem != null) dropSystem.DropWeapon();

        Destroy(gameObject, 0.1f);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (isExploding) return;

        // 🧨 플레이어 충돌 처리
        if (collision.gameObject.CompareTag("Player"))
        {
            Explode();
            return;
        }

        // 🧨 총알 충돌 처리
        Projectile p = collision.gameObject.GetComponent<Projectile>();
        if (p != null)
        {
            // 💥 디버그 로그 추가로 충돌 여부 확인
            Debug.Log($"[SuicideEnemy] 총알({p.name})에 맞음 - 데미지: {p.damage}");

            // 데미지 적용
            TakeDamage(p.damage);

            // 총알 파괴 (안전하게)
            if (p.gameObject != null)
            {
                Destroy(p.gameObject);
            }

            return;
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

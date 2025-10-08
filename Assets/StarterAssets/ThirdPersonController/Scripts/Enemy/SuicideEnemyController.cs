using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SuicideEnemyController : MonoBehaviour
{
    [Header("기본 설정")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("AI 설정")]
    public float detectionRange = 20f;     // 플레이어 인식 범위
    public float normalSpeed = 3.5f;       // 평상시 이동 속도
    public float chaseSpeed = 10f;         // 인식 시 속도
    public float chaseDuration = 2f;       // 🔥 빠르게 달리는 지속 시간
    public float explosionRange = 2.5f;    // 폭발 범위
    public float explosionDamage = 70f;
    public GameObject explosionEffect;


    private NavMeshAgent agent;
    private Transform player;
    private bool isExploding = false;
    private bool isBoosting = false; // 🔥 현재 속도 증가 중인지 체크

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;
        FindPlayer();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (isExploding) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 추적
        agent.SetDestination(player.position);

        // 🔹 인식 범위 안에 들어오면 일정 시간 동안만 속도 증가
        if (dist <= detectionRange && !isBoosting)
            StartCoroutine(SpeedBoostRoutine());

        // 폭발 거리 안이면 폭발
        if (dist <= explosionRange)
            Explode();
    }

    // 🔥 2초 동안만 빠르게 달리는 코루틴
    IEnumerator SpeedBoostRoutine()
    {
        isBoosting = true;
        agent.speed = chaseSpeed;

        yield return new WaitForSeconds(chaseDuration);

        agent.speed = normalSpeed;
        isBoosting = false;
    }

    public void ResetEnemy()
    {
        isExploding = false;
        currentHealth = maxHealth;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        gameObject.SetActive(true);
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    public void TakeDamage(float dmg)
    {
        if (isExploding) return;
        currentHealth -= Mathf.RoundToInt(dmg);
        if (currentHealth <= 0)
            Explode();
    }

    void Explode()
    {
        if (isExploding) return;
        isExploding = true;

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

        // 기존: Destroy(gameObject, 0.1f);
        Invoke(nameof(ReturnToPool), 0.1f);
    }

    private void ReturnToPool()
    {
        isExploding = false;
        currentHealth = maxHealth;
        PoolManager.Instance.Return(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isExploding) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Explode();
        }

        Projectile p = collision.gameObject.GetComponent<Projectile>();
        if (p != null)
        {
            TakeDamage(p.damage);
            Destroy(p.gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
#endif
}

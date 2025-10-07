using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class SuicideEnemyController : MonoBehaviour
{
    [Header("기본 설정")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("AI 설정")]
    public float detectionRange = 12f;
    public float rushRange = 6f;       // 돌진 개시 거리
    public float followSpeed = 3.5f;   // 평상시 추적 속도
    public float prepDelay = 1.0f;     // 삐빅 대기시간
    public float rushSpeed = 20f;      // 돌진 속도
    public float explosionDelay = 2.5f;// 돌진 후 폭발 타이머

    [Header("폭발 설정")]
    public float explosionRange = 3f;
    public float explosionDamage = 60f;
    public GameObject explosionEffect;

    [Header("사운드")]
    public AudioClip beepSound;
    public AudioClip explosionSound;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Transform player;
    private AudioSource audioSource;

    private bool isPreparing = false;
    private bool isRushing = false;
    private bool hasExploded = false;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        rb.isKinematic = true;
        rb.freezeRotation = true;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        agent.speed = followSpeed;
        agent.acceleration = 8f;
        agent.angularSpeed = 400f;
    }

    void Update()
    {
        if (player == null || hasExploded) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 평소 추적
        if (!isPreparing && !isRushing)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // 자폭 준비 개시
        if (dist <= rushRange && !isPreparing && !isRushing)
        {
            StartCoroutine(PrepareExplosion());
        }
    }

    IEnumerator PrepareExplosion()
    {
        isPreparing = true;
        agent.isStopped = true;

        // 삐빅 소리 경고
        float elapsed = 0f;
        while (elapsed < prepDelay)
        {
            elapsed += 0.3f;
            if (audioSource && beepSound)
                audioSource.PlayOneShot(beepSound);
            yield return new WaitForSeconds(0.3f);
        }

        StartCoroutine(RushAndExplode());
    }

    IEnumerator RushAndExplode()
    {
        isPreparing = false;
        isRushing = true;

        // NavMeshAgent 끄고 물리 이동으로 전환
        agent.enabled = false;
        rb.isKinematic = false;

        // 돌진 방향 계산 (그 순간 플레이어 위치 기준)
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        // 물리 힘으로 돌진
        rb.AddForce(dir * rushSpeed, ForceMode.VelocityChange);

        // 💣 타이머가 끝나면 무조건 폭발
        yield return new WaitForSeconds(explosionDelay);
        if (!hasExploded)
            Explode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // 총알 피격
        Projectile proj = collision.gameObject.GetComponent<Projectile>();
        if (proj != null)
        {
            TakeDamage(proj.damage);
            Destroy(collision.gameObject);
            return;
        }

        // 플레이어 충돌 시 바로 폭발
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.name == "Player")
        {
            Explode();
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // 이펙트
        if (explosionEffect)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // 폭발음
        if (audioSource && explosionSound)
            audioSource.PlayOneShot(explosionSound);

        // 폭발 데미지
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRange);
        foreach (Collider c in hits)
        {
            if (c.CompareTag("Player"))
            {
                Character ch = c.GetComponent<Character>();
                if (ch == null) ch = c.GetComponentInParent<Character>();
                if (ch != null)
                    ch.ApplyDamage(null, transform, explosionDamage);
            }
        }

        // 물리/충돌 제거
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;

        Destroy(gameObject, 0.1f);
    }

    public void TakeDamage(float dmg)
    {
        if (hasExploded) return;

        currentHealth -= Mathf.RoundToInt(dmg);
        if (currentHealth <= 0)
            Explode();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rushRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}

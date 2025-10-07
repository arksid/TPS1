using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class SuicideEnemyController : MonoBehaviour
{
    [Header("체력 설정")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("행동 설정")]
    public float detectionRange = 10f;   // 감지 범위
    public float rushRange = 5f;         // 돌진 시작 거리
    public float rushSpeed = 15f;        // 돌진 속도
    public float prepDelay = 0.8f;       // 자폭 준비 시간 (삐빅)
    public float followSpeed = 3.5f;     // 평상시 추적 속도

    [Header("폭발 설정")]
    public float explosionRange = 3f;    // 폭발 반경
    public float explosionDamage = 60f;  // 폭발 데미지
    public GameObject explosionEffect;   // 폭발 이펙트 프리팹

    [Header("사운드 설정")]
    public AudioClip beepSound;
    public AudioClip explosionSound;

    private NavMeshAgent agent;
    private Transform player;
    private Rigidbody rb;
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

        // NavMesh 설정
        agent.speed = followSpeed;
        agent.acceleration = 8f;
        agent.angularSpeed = 400f;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;
    }

    void Update()
    {
        if (player == null || hasExploded) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 일반 추적
        if (!isPreparing && !isRushing)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            // 부드럽게 회전
            Vector3 dir = (player.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 8f);
            }
        }

        // 일정 거리 안에 들어오면 자폭 준비
        if (dist <= rushRange && !isPreparing && !isRushing)
        {
            StartCoroutine(PrepareAndRush());
        }
    }

    private IEnumerator PrepareAndRush()
    {
        isPreparing = true;
        agent.isStopped = true;

        float elapsed = 0f;
        while (elapsed < prepDelay)
        {
            elapsed += 0.25f;
            if (audioSource && beepSound)
                audioSource.PlayOneShot(beepSound);
            yield return new WaitForSeconds(0.25f);
        }

        RushAtPlayer();
    }

    private void RushAtPlayer()
    {
        if (player == null || hasExploded) return;

        isPreparing = false;
        isRushing = true;

        // NavMeshAgent 비활성 → 물리 이동 전환
        agent.enabled = false;
        rb.isKinematic = false;

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;

        transform.rotation = Quaternion.LookRotation(dir);
        rb.velocity = dir * rushSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Explode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // ✅ 총알 태그로 맞았을 때 데미지 받음
        if (other.CompareTag("Projectile"))
        {
            Projectile proj = other.GetComponent<Projectile>();
            if (proj != null)
            {
                TakeDamage(proj.damage);
            }

            Destroy(other.gameObject);
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // 폭발 이펙트
        if (explosionEffect)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // 폭발음
        if (audioSource && explosionSound)
            audioSource.PlayOneShot(explosionSound);

        // 폭발 데미지 적용
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRange);
        foreach (Collider c in hits)
        {
            if (c.CompareTag("Player"))
            {
                Character ch = c.GetComponent<Character>();
                if (ch == null)
                    ch = c.GetComponentInParent<Character>();

                if (ch != null)
                    ch.ApplyDamage(null, transform, explosionDamage);
            }
        }

        // 정지 후 삭제
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 0.1f);
    }

    public void TakeDamage(float dmg)
    {
        if (hasExploded) return;

        currentHealth -= Mathf.RoundToInt(dmg);
        if (currentHealth <= 0)
        {
            Explode();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rushRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}

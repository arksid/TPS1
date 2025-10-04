using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("체력 설정")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("공격 관련 설정")]
    public Transform shootingPoint;
    public GameObject projectilePrefab;
    public float shootRange = 15f;       // 사거리
    public float shootCooldown = 1.5f;   // 쿨타임
    private float lastShootTime;

    [Header("AI 관련")]
    public float rotationSpeed = 5f;     // 회전 속도
    private NavMeshAgent agent;
    private Transform playerTarget;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // ✅ 플레이어 자동 탐색
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }
    }

    void Update()
    {
        if (playerTarget == null || currentHealth <= 0)
            return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // 🔹 플레이어가 사거리 밖이면 추적
        if (distance > shootRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);

            if (animator != null)
                animator.SetBool("isMoving", agent.velocity.magnitude > 0.1f);
        }
        // 🔹 사거리 안이면 정지 + 공격
        else
        {
            agent.isStopped = true;

            // 플레이어를 바라보게 회전
            Vector3 dir = (playerTarget.position - transform.position);
            dir.y = 0;
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationSpeed);

            if (animator != null)
                animator.SetBool("isMoving", false);

            // 공격 쿨타임 체크
            if (Time.time - lastShootTime >= shootCooldown)
            {
                Shoot();
                lastShootTime = Time.time;
            }
        }
    }

    private void Shoot()
    {
        if (shootingPoint == null || projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: ShootingPoint 또는 ProjectilePrefab이 비어있습니다!");
            return;
        }

        // ✅ 플레이어 방향으로 조준
        Vector3 shootDir = (playerTarget.position - shootingPoint.position).normalized;
        Quaternion rot = Quaternion.LookRotation(shootDir, Vector3.up);

        GameObject bullet = Instantiate(projectilePrefab, shootingPoint.position, rot);

        // ✅ 자기 자신과 충돌 무시 추가
        Collider myCol = GetComponent<Collider>();
        Collider bulletCol = bullet.GetComponent<Collider>();
        if (myCol != null && bulletCol != null)
            Physics.IgnoreCollision(bulletCol, myCol);

        // 발사력
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(shootDir * 25f, ForceMode.Impulse);

        Debug.Log($"{name} → 총알 발사!");
    }


    public void TakeDamage(float damage)
    {
        currentHealth -= Mathf.RoundToInt(damage);

        if (animator != null)
            animator.SetTrigger("Hit");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (animator != null)
            animator.SetTrigger("Die");

        if (agent != null)
            agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        Destroy(gameObject, 3f);
    }

    // ✅ 웨이브 스포너용 플레이어 설정
    public void SetPlayer(Transform player)
    {
        playerTarget = player;
    }
}

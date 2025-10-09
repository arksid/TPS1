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
    public float shootRange = 15f;
    public float shootCooldown = 1.5f;
    public float projectileSpeed = 25f;
    public float projectileDamage = 10f;
    private float lastShootTime;

    [Header("AI 관련")]
    public float rotationSpeed = 8f;
    private NavMeshAgent agent;
    private Transform playerTarget;
    private Animator animator;

    [Header("폭발 이펙트")]
    public GameObject deathExplosionPrefab;
    public float explosionDestroyTime = 3f;

    private void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;

        lastShootTime = Time.time - shootCooldown;
    }

    private void Update()
    {
        if (playerTarget == null || currentHealth <= 0) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > shootRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            if (animator != null)
                animator.SetBool("isMoving", agent.velocity.magnitude > 0.1f);
        }
        else
        {
            agent.isStopped = true;
            Vector3 dir = (playerTarget.position - transform.position);
            dir.y = 0;
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationSpeed);

            if (animator != null)
                animator.SetBool("isMoving", false);

            if (Time.time - lastShootTime >= shootCooldown)
            {
                Shoot();
                lastShootTime = Time.time;
            }
        }
    }

    private void Shoot()
    {
        if (shootingPoint == null || projectilePrefab == null) return;

        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
        Vector3 shootDir = (targetPoint - shootingPoint.position).normalized;
        shootingPoint.rotation = Quaternion.LookRotation(shootDir);

        GameObject bullet = Instantiate(projectilePrefab, shootingPoint.position, shootingPoint.rotation);

        var proj = bullet.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            proj.Init(gameObject, shootDir, projectileSpeed, projectileDamage);
        }

        Collider bulletCol = bullet.GetComponent<Collider>();
        if (bulletCol != null)
        {
            Collider[] enemyCols = GetComponentsInChildren<Collider>();
            foreach (var c in enemyCols)
            {
                if (c != null) Physics.IgnoreCollision(bulletCol, c, true);
            }
        }

        Destroy(bullet, 5f);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= Mathf.RoundToInt(damage);
        if (currentHealth < 0) currentHealth = 0;

        if (animator != null) animator.SetTrigger("Hit");

        // ✅ 디버그 로그로 데미지 및 HP 표시
        Debug.Log($"[EnemyController] 데미지: {damage} / HP: {currentHealth} / {maxHealth}");

        // ✅ HUD에도 표시 (CanvasManager 연결 시)
        if (CanvasManager.singleton != null)
            CanvasManager.singleton.ShowDamage(damage);

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        // ✅ 폭발 이펙트 생성
        if (deathExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, explosionDestroyTime); // 이펙트만 일정 시간 유지
        }

        // ✅ 즉시 본체 제거
        Destroy(gameObject);
    }



#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (shootingPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(shootingPoint.position, 0.1f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(shootingPoint.position, shootingPoint.forward * 2f);
        }
    }
#endif

    public void SetPlayer(Transform player) => playerTarget = player;
}

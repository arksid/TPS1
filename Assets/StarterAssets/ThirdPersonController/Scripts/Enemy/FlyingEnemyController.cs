using UnityEngine;
using System.Collections;

public class SmartFlyingEnemyController : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 6f;
    public float turnSpeed = 5f;
    public float hoverHeight = 5f;
    public float minDistance = 3f;
    public float maxDistance = 4f;
    public float moveInterval = 3f;
    public float randomOffset = 2f;

    [Header("공격 설정")]
    public float attackRange = 15f;
    public float attackCooldown = 2f;
    public float projectileSpeed = 25f;
    public float projectileDamage = 15f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("체력 / 이펙트")]
    public int maxHealth = 100;
    private int currentHealth;
    public GameObject explosionEffect;

    private Transform player;
    private Rigidbody rb;
    private bool isDead = false;
    private bool canShoot = true;

    private Vector3 targetPos;
    private float lastMoveTime;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 2f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        SetNewTargetPosition();
    }

    void FixedUpdate()
    {
        if (isDead || player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (Time.time - lastMoveTime > moveInterval)
            SetNewTargetPosition();

        MoveTowardTarget();

        if (distToPlayer <= attackRange && canShoot)
            StartCoroutine(ShootRoutine());
    }

    void SetNewTargetPosition()
    {
        lastMoveTime = Time.time;
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir.y = Mathf.Clamp(randomDir.y, -0.2f, 0.6f);
        randomDir.Normalize();

        float randomDist = Random.Range(minDistance, maxDistance);
        Vector3 offset = randomDir * randomDist;

        targetPos = player.position + offset + Vector3.up * hoverHeight;
    }

    void MoveTowardTarget()
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        Vector3 topDir = (player.position + Vector3.up * (hoverHeight + 1f) - transform.position).normalized;
        if (Vector3.Dot(dir, topDir) > 0.8f)
        {
            dir = Quaternion.Euler(0, Random.Range(60f, 120f), 0) * dir;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
        rb.velocity = transform.forward * moveSpeed;
    }

    IEnumerator ShootRoutine()
    {
        canShoot = false;

        if (firePoint != null && player != null && projectilePrefab != null)
        {
            Vector3 shootDir = (player.position + Vector3.up * 1.2f - firePoint.position).normalized;
            Quaternion rot = Quaternion.LookRotation(shootDir);
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, rot);
            Rigidbody rbBullet = bullet.GetComponent<Rigidbody>();

            if (rbBullet != null)
            {
                rbBullet.velocity = shootDir * projectileSpeed;
            }

            Destroy(bullet, 5f);
        }

        yield return new WaitForSeconds(attackCooldown);
        canShoot = true;
    }

    public void TakeDamage(float dmg)
    {
        currentHealth -= Mathf.RoundToInt(dmg);
        if (currentHealth <= 0 && !isDead)
            Explode();
    }

    void Explode()
    {
        if (isDead) return;
        isDead = true;

        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        Destroy(gameObject, 0.2f);
    }

    public void ResetEnemy()
    {
        isDead = false;
        currentHealth = maxHealth;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        gameObject.SetActive(true);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
#endif
}

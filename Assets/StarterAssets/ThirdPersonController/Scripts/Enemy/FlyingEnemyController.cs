using UnityEngine;
using System.Collections;

public class FlyingEnemyController : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 10f;
    public float turnSpeed = 5f;
    public float hoverHeight = 6f;
    public float minDistance = 4f;
    public float attackRange = 15f;

    [Header("회피 기동")]
    public float dodgeFrequency = 2f;
    public float dodgeStrength = 3f;

    [Header("공격 설정")]
    public float attackCooldown = 2f;
    public float projectileSpeed = 25f;
    public float projectileDamage = 15f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("체력")]
    public int maxHealth = 100;
    private int currentHealth;

    private Transform player;
    private Rigidbody rb;
    private bool isDead = false;
    private bool canShoot = true;
    private Vector3 dodgeDir;
    private float lastDodgeTime;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 2f;

        // 기본 플레이어 찾기
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
            else
                Debug.LogWarning("[FlyingEnemy] Player 태그를 찾을 수 없습니다!");
        }

        PickNewDodgeDirection();
    }

    void FixedUpdate()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // ✅ 플레이어를 향해 회전
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

        // ✅ 회피 방향 주기적으로 변경
        if (Time.time - lastDodgeTime > dodgeFrequency)
            PickNewDodgeDirection();

        // ✅ 이동
        if (distance > minDistance)
        {
            Vector3 moveDir = dirToPlayer + dodgeDir;
            moveDir.Normalize();
            moveDir.y = 0;
            rb.velocity = moveDir * moveSpeed;
        }
        else
        {
            rb.velocity = Vector3.zero;
        }

        // ✅ 공격
        if (distance <= attackRange && canShoot)
            StartCoroutine(ShootRoutine());
    }

    void PickNewDodgeDirection()
    {
        dodgeDir = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized * dodgeStrength * 0.1f;

        lastDodgeTime = Time.time;
    }

    IEnumerator ShootRoutine()
    {
        canShoot = false;
        if (firePoint != null && projectilePrefab != null)
        {
            Vector3 shootDir = (player.position + Vector3.up * 1.2f - firePoint.position).normalized;
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDir));
            Rigidbody rbBullet = bullet.GetComponent<Rigidbody>();

            if (rbBullet != null)
                rbBullet.velocity = shootDir * projectileSpeed;
        }
        yield return new WaitForSeconds(attackCooldown);
        canShoot = true;
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHealth -= Mathf.RoundToInt(dmg);
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        Destroy(gameObject, 0.1f);
    }

    // ✅ 스폰 시 외부에서 플레이어 지정 가능하게
    public void SetPlayer(Transform p)
    {
        player = p;
    }
}

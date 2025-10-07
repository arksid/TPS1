using UnityEngine;
using System.Collections;

public class FlyingEnemyController : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 6f;            // 기본 이동 속도
    public float turnSpeed = 4f;            // 회전 속도
    public float followHeight = 5f;         // 유지할 비행 높이
    public float detectionRange = 20f;      // 플레이어 탐지 거리
    public float attackRange = 7f;          // 돌진 거리
    public float rushSpeed = 15f;           // 돌진 속도

    [Header("체력 설정")]
    public int maxHealth = 80;
    private int currentHealth;

    [Header("이펙트 / 폭발")]
    public GameObject explosionEffect;

    private Transform player;
    private Rigidbody rb;
    private bool isAttacking = false;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

        // 중력 제거 (떠다니게)
        rb.useGravity = false;
        rb.drag = 2f;

        GameObject p = GameObject.Find("Player");
        if (p != null)
            player = p.transform;
    }

    void FixedUpdate()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 평소엔 플레이어를 따라다님
        if (!isAttacking)
        {
            FollowPlayer();

            // 일정 거리 안으로 들어오면 돌진
            if (dist <= attackRange)
                StartCoroutine(DiveAttack());
        }
    }

    void FollowPlayer()
    {
        // 목표 위치: 플레이어 위/주변 높이로 따라가기
        Vector3 targetPos = player.position + Vector3.up * followHeight;

        Vector3 dir = (targetPos - transform.position).normalized;

        // 회전 부드럽게
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

        // 앞으로 이동
        rb.velocity = transform.forward * moveSpeed;
    }

    IEnumerator DiveAttack()
    {
        isAttacking = true;

        // 돌진 방향 계산
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f; // 수평 돌진

        rb.velocity = dir * rushSpeed;

        yield return new WaitForSeconds(1.5f); // 돌진 후 다시 복귀
        isAttacking = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        // ✅ 총알 충돌 처리 (EnemyController 방식 통합)
        Projectile proj = collision.gameObject.GetComponent<Projectile>();
        if (proj != null)
        {
            TakeDamage(proj.damage);
            Destroy(collision.gameObject);
            return;
        }

        // ✅ 플레이어와 충돌 시 폭발
        if (collision.gameObject.name == "Player")
        {
            Explode();
        }
    }


    public void TakeDamage(float dmg)
    {
        currentHealth -= Mathf.RoundToInt(dmg);
        if (currentHealth <= 0 && !isDead)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (isDead) return;
        isDead = true;

        if (explosionEffect)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}

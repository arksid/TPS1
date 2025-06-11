using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public float fireRange = 10f;
    private float fireTimer = 0f;
    public float meleeDamage = 10f;
    public float meleeCooldown = 1f;
    private float lastMeleeTime = -999f;

    public Transform player;
    private NavMeshAgent agent;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3.5f; //  이동 속도 변수 추가

    private float currentHealth;

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    void Start()
    {
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed; // 개별 속도 적용
        }
    }

    void Update()
    {
        if (player != null && agent != null)
        {
            agent.SetDestination(player.position);

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= fireRange)
            {
                fireTimer += Time.deltaTime;
                if (fireTimer >= fireRate)
                {
                    FireAtPlayer();
                    fireTimer = 0f;
                }
            }
        }
    }
    void FireAtPlayer()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            Vector3 targetPoint = player.position + Vector3.up * 1.5f; //  상체 보정
            Vector3 direction = (targetPoint - firePoint.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Instantiate(projectilePrefab, firePoint.position, lookRotation);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastMeleeTime >= meleeCooldown)
            {
                Character player = collision.transform.GetComponentInParent<Character>();
                if (player != null)
                {
                    player.ApplyDamage(null, collision.transform, meleeDamage);
                    lastMeleeTime = Time.time;
                }
            }
        }
    }
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}

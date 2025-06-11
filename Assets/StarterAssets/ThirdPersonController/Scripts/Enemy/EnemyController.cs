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
        }
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
    void FireAtPlayer()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            Vector3 direction = (player.position - firePoint.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Instantiate(projectilePrefab, firePoint.position, lookRotation);
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

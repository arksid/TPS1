using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SemiBossController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Stats")]
    public float maxHealth = 300f;
    public float fireRate = 2f;
    public float fireRange = 10f;
    public float meleeRange = 2f;
    public float meleeDamage = 30f;

    private float currentHealth;
    private float attackTimer;
    private NavMeshAgent agent;

    private void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (player == null || agent == null) return;

        agent.SetDestination(player.position);

        float distance = Vector3.Distance(transform.position, player.position);
        attackTimer += Time.deltaTime;

        if (distance <= meleeRange && attackTimer >= 2f)
        {
            MeleeAttack();
        }
        else if (distance <= fireRange && attackTimer >= fireRate)
        {
            RangedAttack();
        }

        if (currentHealth < maxHealth * 0.5f)
        {
            TrySpecialAttack();
        }
    }

    private void MeleeAttack()
    {
        attackTimer = 0f;
        Character target = player.GetComponent<Character>();
        if (target != null)
        {
            target.ApplyDamage(null, player, meleeDamage);
        }
    }

    private void RangedAttack()
    {
        attackTimer = 0f;
        if (projectilePrefab == null || firePoint == null) return;

        Vector3 direction = (player.position + Vector3.up * 1.5f - firePoint.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rotation);

        // Ignore self-collision
        Collider[] bossCols = GetComponentsInChildren<Collider>();
        Collider[] projCols = projectile.GetComponents<Collider>();
        foreach (var projCol in projCols)
        {
            foreach (var bossCol in bossCols)
            {
                Physics.IgnoreCollision(projCol, bossCol);
            }
        }

        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.shooter = gameObject;
        }
    }

    private void TrySpecialAttack()
    {
        // Optional: implement special attack (e.g., AoE)
    }

    public void SetPlayer(Transform t) => player = t;

    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("세미보스 사망!");
        Destroy(gameObject);
    }
}


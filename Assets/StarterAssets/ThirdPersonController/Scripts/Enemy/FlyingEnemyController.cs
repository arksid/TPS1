using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FlyingEnemyController : MonoBehaviour
{
    [Header("Target & Movement")]
    public Transform player;
    public float moveSpeed = 5f;
    public float stopDistance = 15f;  // 플레이어 반경 15m 이내로 접근하지 않음
    public float rotationSpeed = 5f;
    public float evadeAmplitude = 2f;
    public float evadeFrequency = 2f;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1.5f;
    private float nextFireTime;

    [Header("Status")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    [Header("Experience / Ultimate")]
    public int expReward = 10;
    public float ultimateGaugeReward = 10f;

    [Header("Effect")]
    public GameObject deathEffect;

    private Rigidbody rb;
    private Vector3 initialEvadeOffset;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;

        // 회피 이동 시작 지점
        initialEvadeOffset = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        );
    }

    private void Update()
    {
        if (isDead) return;

        if (player != null)
        {
            MoveAndEvade();
            Attack();
        }
    }

    private void MoveAndEvade()
    {
        Vector3 direction = (player.position - transform.position);
        float distance = direction.magnitude;

        // 플레이어를 바라보게 회전
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 일정 거리 이상일 때만 접근
        if (distance > stopDistance)
        {
            Vector3 evadeOffset = initialEvadeOffset * Mathf.Sin(Time.time * evadeFrequency) * evadeAmplitude;
            Vector3 moveDir = (direction.normalized + evadeOffset.normalized).normalized;
            rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
        }
    }

    private void Attack()
    {
        if (Time.time >= nextFireTime)
        {
            if (bulletPrefab != null && firePoint != null)
            {
                Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            }
            nextFireTime = Time.time + fireRate;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 이펙트
        if (deathEffect != null)
        {
            GameObject fx = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        // 경험치 및 궁극기 게이지
        if (PlayerLevelSystem.Instance != null)
            PlayerLevelSystem.Instance.AddExp(expReward);

        var ult = FindObjectOfType<UltimateSkill>();
        if (ult != null)
            ult.AddGauge(ultimateGaugeReward);

        // ✅ 공통 드랍 시스템 호출
        var dropSystem = GetComponent<EnemyDropSystem>();
        if (dropSystem != null) dropSystem.TryDropItemByWeight();

        Destroy(gameObject, 0.1f);
    }

    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }
}

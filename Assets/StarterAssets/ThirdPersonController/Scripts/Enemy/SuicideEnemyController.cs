using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SuicideEnemyController : MonoBehaviour
{
    [Header("기본 설정")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("AI 설정")]
    public float detectionRange = 20f;
    public float normalSpeed = 3.5f;
    public float chaseSpeed = 10f;
    public float chaseDuration = 2f;
    public float explosionRange = 2.5f;
    public float explosionDamage = 70f;
    public GameObject explosionEffect;

    private NavMeshAgent agent;
    private Transform player;
    private bool isExploding = false;
    private bool isBoosting = false;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;
        FindPlayer();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (isExploding) return;

        float dist = Vector3.Distance(transform.position, player.position);
        agent.SetDestination(player.position);

        if (dist <= detectionRange && !isBoosting)
            StartCoroutine(SpeedBoostRoutine());

        if (dist <= explosionRange)
            Explode();
    }

    IEnumerator SpeedBoostRoutine()
    {
        isBoosting = true;
        agent.speed = chaseSpeed;
        yield return new WaitForSeconds(chaseDuration);
        agent.speed = normalSpeed;
        isBoosting = false;
    }

    public void ResetEnemy()
    {
        isExploding = false;
        currentHealth = maxHealth;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        gameObject.SetActive(true);
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    public void TakeDamage(float dmg)
    {
        if (isExploding) return;
        currentHealth -= Mathf.RoundToInt(dmg);
        if (currentHealth <= 0) Explode();
    }

    void Explode()
    {
        if (isExploding) return;
        isExploding = true;

        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRange);
        foreach (Collider c in hits)
        {
            if (c.CompareTag("Player"))
            {
                Character ch = c.GetComponent<Character>() ?? c.GetComponentInParent<Character>();
                if (ch != null)
                    ch.ApplyDamage(null, transform, explosionDamage);
            }
        }

        Destroy(gameObject, 0.1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isExploding) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Explode();
        }

        Projectile p = collision.gameObject.GetComponent<Projectile>();
        if (p != null)
        {
            TakeDamage(p.damage);
            Destroy(p.gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
#endif
}

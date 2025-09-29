using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    private NavMeshAgent agent;
    private Transform playerTarget;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (playerTarget != null && agent != null)
        {
            agent.SetDestination(playerTarget.position);
        }
    }

    public void SetPlayer(Transform player)
    {
        playerTarget = player;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= Mathf.RoundToInt(damage); // 내부 계산은 int로
        if (currentHealth <= 0)
        {
            Die();
        }
    }


    private void Die()
    {
        if (RadarManager.Instance != null)
        {
            RadarManager.Instance.UnregisterEnemy(transform);
        }
        Destroy(gameObject);
    }
}

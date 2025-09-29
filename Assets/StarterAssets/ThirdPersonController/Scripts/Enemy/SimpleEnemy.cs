using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        // ❌ 여기서 RegisterEnemy 호출하지 않음
    }

    public void ApplyDamage(float damage)
    {
        currentHealth -= Mathf.RoundToInt(damage);
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            ApplyDamage(projectile.damage);
            Destroy(other.gameObject);
        }
    }

    private void OnDestroy()
    {
        // ✅ 죽을 때만 레이더에서 제거
        if (RadarManager.Instance != null)
            RadarManager.Instance.UnregisterEnemy(transform);
    }
}

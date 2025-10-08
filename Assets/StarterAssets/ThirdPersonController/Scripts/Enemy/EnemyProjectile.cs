using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 10f;
    public float lifeTime = 5f;
    public GameObject shooter;

    private Rigidbody rb;
    private float spawnTime;
    private Collider bulletCol;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        bulletCol = GetComponent<Collider>();
        if (bulletCol == null)
            bulletCol = gameObject.AddComponent<SphereCollider>();

        bulletCol.isTrigger = true; // ✅ 트리거 모드로 강제 설정
    }

    public void Init(GameObject shooter, Vector3 direction, float speed, float damage)
    {
        this.shooter = shooter;
        this.speed = speed;
        this.damage = damage;
        spawnTime = Time.time;

        rb.velocity = direction.normalized * speed;

        // 🔹 발사자와 충돌 무시 (한 번만 처리)
        if (shooter != null && bulletCol != null)
        {
            Collider[] cols = shooter.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
                Physics.IgnoreCollision(bulletCol, c, true);
        }
    }

    void Update()
    {
        if (Time.time - spawnTime > lifeTime)
            PoolManager.Instance.Return(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == shooter) return;

        Character ch = other.GetComponentInParent<Character>();
        if (ch != null)
        {
            ch.ApplyDamage(null, other.transform, damage);
            PoolManager.Instance.Return(gameObject);
            return;
        }

        if (other.CompareTag("Player"))
            PoolManager.Instance.Return(gameObject);
    }
}

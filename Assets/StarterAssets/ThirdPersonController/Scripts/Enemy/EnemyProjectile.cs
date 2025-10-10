using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("기본 설정")]
    public float speed = 20f;
    public float damage = 10f;
    public float lifeTime = 5f;

    private GameObject shooter;
    private Rigidbody rb;
    private Collider bulletCol;
    private float spawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        bulletCol = GetComponent<Collider>();
        if (bulletCol == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.1f;
            bulletCol = sphere;
        }

        // ✅ 트리거 OFF
        bulletCol.isTrigger = false;
    }

    public void Init(GameObject shooter, Vector3 direction, float speed, float damage)
    {
        this.shooter = shooter;
        this.speed = speed;
        this.damage = damage;
        spawnTime = Time.time;

        rb.velocity = direction.normalized * speed;

        // 발사자 충돌 무시
        if (shooter != null && bulletCol != null)
        {
            Collider[] cols = shooter.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                if (c != null) Physics.IgnoreCollision(bulletCol, c, true);
            }
        }
    }

    void Update()
    {
        if (Time.time - spawnTime > lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 자기 자신 무시
        if (collision.gameObject == shooter) return;
        if (shooter != null && collision.transform.IsChildOf(shooter.transform)) return;

        // 플레이어 맞음
        Character ch = collision.collider.GetComponentInParent<Character>();
        if (ch != null)
        {
            ch.ApplyDamage(null, collision.transform, damage);
            Destroy(gameObject);
            return;
        }

        // 벽이나 장애물에 부딪혔을 때
        if (!collision.collider.isTrigger)
        {
            // ✅ 발사자(적)에게 알림
            if (shooter != null)
            {
                var controller = shooter.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.OnBulletBlocked(transform.position);
                }
            }

            Destroy(gameObject);
        }
    }
}

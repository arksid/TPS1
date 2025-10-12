using UnityEngine;

public class EnemyProjectile : MonoBehaviour, ISlowable
{
    [Header("기본 설정")]
    public float speed = 20f;
    public float damage = 10f;
    public float lifeTime = 5f;

    private GameObject shooter;
    private Rigidbody rb;
    private Collider bulletCol;
    private float spawnTime;

    // 🐢 궁극기 슬로우용
    private float baseSpeed;
    private float localTimeScale = 1f;

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

        bulletCol.isTrigger = false;

        // ✅ 기본 속도 저장
        baseSpeed = speed;
    }

    public void Init(GameObject shooter, Vector3 direction, float speed, float damage)
    {
        this.shooter = shooter;
        this.speed = speed;
        this.damage = damage;
        this.baseSpeed = speed;
        spawnTime = Time.time;

        rb.velocity = direction.normalized * speed;

        // ✅ 궁극기 발동 중일 때 슬로우 적용
        if (UltimateSkill.IsUltimateActive)
        {
            SetLocalTimeScale(UltimateSkill.CurrentSlowFactor);
        }

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
        // 🐢 궁극기 배율 반영
        rb.velocity = rb.velocity.normalized * (baseSpeed * localTimeScale);

        if (Time.time - spawnTime > lifeTime)
        {
            Destroy(gameObject);
        }
    }

    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = scale;
    }

    private void OnCollisionEnter(Collision collision)
    {
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

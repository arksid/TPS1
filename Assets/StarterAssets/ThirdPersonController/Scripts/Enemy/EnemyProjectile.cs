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
        // Rigidbody 설정
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Collider 설정
        bulletCol = GetComponent<Collider>();
        if (bulletCol == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.1f; // 너무 큰 충돌 방지
            bulletCol = sphere;
        }
        bulletCol.isTrigger = true;
    }

    /// <summary>
    /// 총알 초기화
    /// </summary>
    public void Init(GameObject shooter, Vector3 direction, float speed, float damage)
    {
        this.shooter = shooter;
        this.speed = speed;
        this.damage = damage;
        spawnTime = Time.time;

        rb.velocity = direction.normalized * speed;

        // ✅ 발사자와 모든 충돌 무시
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
        // 수명 만료 시 제거
        if (Time.time - spawnTime > lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ✅ 자기 자신(발사자) 무시
        if (other.gameObject == shooter) return;

        // ✅ 적이 발사했는데 적 본체랑 부딪히는 경우 방지
        if (shooter != null && other.transform.IsChildOf(shooter.transform)) return;

        // 플레이어나 캐릭터에 데미지 적용
        Character ch = other.GetComponentInParent<Character>();
        if (ch != null)
        {
            ch.ApplyDamage(null, other.transform, damage);
            Destroy(gameObject);
            return;
        }

        // 벽, 지형 등에 부딪혔을 때도 삭제
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}

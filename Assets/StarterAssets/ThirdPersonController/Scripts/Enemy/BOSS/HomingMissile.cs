using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HomingMissile : MonoBehaviour, ISlowable
{
    public Transform target;
    public float speed = 30f;
    public float rotateSpeed = 180f;
    public float lifeTime = 8f;
    public int damage = 25;
    public float explosionRadius = 4f;
    public LayerMask damageMask;
    public GameObject explosionVfx;
    public string teamTag = "EnemyProjectile";

    private Rigidbody rb;
    private float localTimeScale = 1f;
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!string.IsNullOrEmpty(teamTag))
            gameObject.tag = teamTag;
    }

    void Update()
    {
        timer += Time.deltaTime * localTimeScale;
        if (timer >= lifeTime)
        {
            Explode();
            return;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (target == null)
        {
            rb.velocity = transform.forward * speed;
            return;
        }

        // 타깃을 향해 회전
        Vector3 aim = (target.position + Vector3.up * 1.2f) - transform.position;
        aim.Normalize();

        Vector3 newDir = Vector3.RotateTowards(
            transform.forward,
            aim,
            Mathf.Deg2Rad * rotateSpeed * Time.fixedDeltaTime * localTimeScale,
            0f
        );

        rb.MoveRotation(Quaternion.LookRotation(newDir));
        rb.velocity = transform.forward * speed;
    }

    void OnTriggerEnter(Collider other)
    {
        // 간단한 충돌 조건: 플레이어/기본 레이어에 닿으면 폭발
        if (other.CompareTag("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            Explode();
        }
    }

    void Explode()
    {
        if (explosionVfx) Instantiate(explosionVfx, transform.position, Quaternion.identity);

        // 범위 데미지 - IHittable 일괄 처리
        Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius, damageMask);
        foreach (var c in cols)
        {
            if (c.TryGetComponent<IHittable>(out var h))
                h.OnHit(damage);
        }

        Destroy(gameObject);
    }

    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = Mathf.Clamp(scale, 0.05f, 5f);
    }
}

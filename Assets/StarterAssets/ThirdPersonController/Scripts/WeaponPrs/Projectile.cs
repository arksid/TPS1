using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, ISlowable
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private GameObject hitParticlePrefab;

    public float damage => _damage;

    private float _damage = 1f;
    private bool _initialized = false;
    private Character _shooter = null;
    private Rigidbody _rigidbody = null;
    private Collider _collider = null;

    [Tooltip("누가 쐈는지(플레이어/적) 판별용")]
    public GameObject shooter;

    private float localTimeScale = 1f;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _rigidbody = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _collider = GetComponent<Collider>() ?? gameObject.AddComponent<SphereCollider>();
        _collider.isTrigger = false;
        gameObject.tag = gameObject.tag; // 태그는 프리팹에서 설정
    }

    public void Initialize(Character shooterChar, Vector3 target, float damage)
    {
        Initialize();
        _shooter = shooterChar;
        _damage = damage;

        transform.LookAt(target);
        _rigidbody.velocity = transform.forward.normalized * _speed;

        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        // 로컬 타임스케일 반영(궁극기 슬로우)
        transform.position += transform.forward * _speed * Time.deltaTime * localTimeScale;
    }

    public void SetLocalTimeScale(float scale) => localTimeScale = scale;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == shooter) return;

        // 1) 궁극기 게이지 먼저
        if (shooter != null)
        {
            var ult = shooter.GetComponent<UltimateSkill>();
            if (ult != null) ult.AddGauge(ult.GaugePerHit);
        }

        // 2) 피격 파티클
        if (hitParticlePrefab != null)
        {
            var contact = collision.contacts[0];
            var particle = Instantiate(hitParticlePrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(particle, 1f);
        }

        // 3) 데미지 처리(적에게)
        var enemy = collision.transform.GetComponentInParent<EnemyController>();
        if (enemy != null && shooter != enemy.gameObject)
        {
            enemy.TakeDamage(_damage);
        }

        Destroy(gameObject);
    }
}

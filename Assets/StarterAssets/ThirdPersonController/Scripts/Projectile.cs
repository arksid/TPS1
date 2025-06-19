using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _speed = 20f;

    [Header("Prefabs")]
    [SerializeField] private Transform _defaultImpact = null;

    private float _damage = 1f;
    public float damage { get { return _damage; } }
    private bool _intitialized = false;
    private Character _shooter = null;
    private Rigidbody _rigidbody = null;
    private Collider _collider = null;
    public GameObject shooter; // 누가 발사했는지
    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_intitialized) { return; }
        _intitialized = true;

        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }
        _rigidbody.useGravity = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<SphereCollider>();
        }
        _collider.isTrigger = false;
        _collider.tag = "Projectile";
    }
    public void Initialize(Character shooter, Vector3 target, float damage)
    {
        Initialize();
        _shooter = shooter;
        _damage = damage;
        transform.LookAt(target);
        _rigidbody.velocity = transform.forward.normalized * _speed;
        Destroy(gameObject, 5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == shooter)
            return; // 자기 자신 무시

        // 세미보스 데미지 처리
        SemiBossController boss = collision.transform.GetComponentInParent<SemiBossController>();
        if (boss != null && shooter != boss.gameObject)
        {
            boss.TakeDamage(damage);
        }

        // 일반 적
        EnemyController enemy = collision.transform.GetComponentInParent<EnemyController>();
        if (enemy != null && shooter != enemy.gameObject)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}


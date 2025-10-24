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

        // 치명타 판정
        float finalDamage = damage;
        if (shooterChar != null && shooterChar.RollCritical())
        {
            finalDamage *= shooterChar.CriticalMultiplier;
            Debug.Log("💥 치명타 발동! " + finalDamage);
        }

        _damage = finalDamage;
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

        // ✨ 1. 피격 파티클
        if (hitParticlePrefab != null)
        {
            var contact = collision.contacts[0];
            var particle = Instantiate(hitParticlePrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(particle, 1f);
        }

        // ✨ 2. 적에 맞았는지 확인 (Enemy 태그)
        if (collision.transform.root.CompareTag("Enemy"))
        {
            // 🧠 적 컨트롤러 찾기 (보스, 자폭병 등 상위에서 찾음)
            var enemy = collision.transform.root.GetComponent<EnemyController>();
            var suicide = collision.transform.root.GetComponent<SuicideEnemyController>();
            var flying = collision.transform.GetComponentInParent<FlyingEnemyController>();
            if (flying != null)
            {
                flying.TakeDamage(_damage);
            }

            if (enemy != null || suicide != null)
            {
                // ✅ 3. 궁극기 게이지 증가
                if (shooter != null)
                {
                    var ult = shooter.GetComponent<UltimateSkill>();
                    if (ult != null)
                        ult.AddGauge(ult.GaugePerHit);
                }

                // ✅ 4. 데미지 처리
                if (enemy != null) enemy.TakeDamage(_damage);
                if (suicide != null) suicide.TakeDamage(_damage);

                // ✅ 5. 히트마커 표시
                if (HitmarkerManager.instance != null)
                    HitmarkerManager.instance.ShowHitmarker();
            }
        }

        // 총알 파괴
        Destroy(gameObject);
    }

}

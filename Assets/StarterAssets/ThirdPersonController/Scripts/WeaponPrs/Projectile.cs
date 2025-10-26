using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, ISlowable
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private GameObject hitParticlePrefab;
    public int remainingPenetrations = 0; // 관통 가능 횟수

    public float damage => _damage;

    private float _damage = 1f;
    private bool _initialized = false;
    private Character _shooter = null;
    private Rigidbody _rigidbody = null;
    private Collider _collider = null;

    [Tooltip("누가 쐈는지(플레이어/적) 판별용")]
    public GameObject shooter;

    private float localTimeScale = 1f;
    // projectile.remainingPenetrations = StatModifierManager.Instance?.ProjectilePenetrationBonus ?? 0;

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

        // 피격 파티클
        if (hitParticlePrefab != null)
        {
            var contact = collision.contacts[0];
            var particle = Instantiate(hitParticlePrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(particle, 1f);
        }

        bool hitEnemy = collision.transform.root.CompareTag("Enemy");

        // === 데미지 처리 + 렌드 가중치 ===
        if (hitEnemy)
        {
            var enemy = collision.transform.root.GetComponent<EnemyController>();
            var suicide = collision.transform.root.GetComponent<SuicideEnemyController>();
            var flying = collision.transform.GetComponentInParent<FlyingEnemyController>();
            if (flying != null) flying.TakeDamage(_damage);

            float finalDamage = _damage;

            // 🔥 Shooter가 갖고 있는 'Rend' 보너스가 있으면 대상 한정 가중치 적용
            if (enemy != null && _shooter != null)
            {
                float rendBonus = _shooter.GetRendBonusForEnemy(enemy); // 0~0.2 등
                if (rendBonus > 0f) finalDamage *= (1f + rendBonus);
            }

            if (enemy != null) enemy.TakeDamage(finalDamage);
            if (suicide != null) suicide.TakeDamage(finalDamage);

            // 히트마커/궁극충전/명중훅
            if (HitmarkerManager.instance != null) HitmarkerManager.instance.ShowHitmarker();
            _shooter?.OnPlayerHitEnemyHook(enemy != null ? enemy : null);
            StatModifierManager.Instance?.OnPlayerHitEnemy();
        }

        // === 관통 분기 ===
        bool shouldDestroy = true;

        if (hitEnemy && remainingPenetrations > 0)
        {
            remainingPenetrations--;

            // 같은 콜라이더 재충돌 방지(잠깐 무시)
            if (_collider != null && collision.collider != null)
            {
                Physics.IgnoreCollision(_collider, collision.collider, true);
                StartCoroutine(ReenableCollision(collision.collider, 0.06f));
            }

            shouldDestroy = false; // 계속 날아감
        }

        if (shouldDestroy)
            Destroy(gameObject);
    }

    private IEnumerator ReenableCollision(Collider other, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_collider != null && other != null)
            Physics.IgnoreCollision(_collider, other, false);
    }


}

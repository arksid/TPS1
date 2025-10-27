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

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _rigidbody = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _collider = GetComponent<Collider>() ?? gameObject.AddComponent<SphereCollider>();
        _collider.isTrigger = false; // 기본은 충돌형, 부위는 주로 트리거에서 처리
        // 태그/레이어는 프리팹에서 설정 유지
    }

    public void Initialize(Character shooterChar, Vector3 target, float damage)
    {
        Initialize();
        _shooter = shooterChar;

        // (선택) 치명타 적용 예시: 기존 구조 유지
        float finalDamage = damage;
        if (shooterChar != null && shooterChar.RollCritical())
        {
            finalDamage *= shooterChar.CriticalMultiplier;
            Debug.Log($"💥 치명타! {finalDamage}");
        }

        _damage = finalDamage;
        transform.LookAt(target);
        _rigidbody.velocity = transform.forward.normalized * _speed;

        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        // 궁극기 슬로우 등 로컬 타임스케일 반영
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
        // ✅ 1) IHittable(= Beacon 등) 먼저 시도
        if (TryDealHittable(collision.collider))
        {
            // 관통이면 계속, 아니면 삭제
            if (remainingPenetrations > 0)
            {
                remainingPenetrations--;
                if (_collider != null && collision.collider != null)
                {
                    Physics.IgnoreCollision(_collider, collision.collider, true);
                    StartCoroutine(ReenableCollision(collision.collider, 0.06f));
                }
                return;
            }
            Destroy(gameObject);
            return;
        }
        // =========================
        // 보스: 부위 라우팅 (DamageablePart만 사용)
        // =========================
        var part = collision.collider.GetComponentInParent<DamageablePart>();
        if (part != null)
        {
            part.ApplyDamage(Mathf.RoundToInt(_damage));

            if (HitmarkerManager.instance != null) HitmarkerManager.instance.ShowHitmarker();
            _shooter?.OnPlayerHitEnemyHook(null);
            StatModifierManager.Instance?.OnPlayerHitEnemy();

            // 관통 유지
            if (remainingPenetrations > 0)
            {
                remainingPenetrations--;
                if (_collider != null && collision.collider != null)
                {
                    Physics.IgnoreCollision(_collider, collision.collider, true);
                    StartCoroutine(ReenableCollision(collision.collider, 0.06f));
                }
                return; // 계속 날아감
            }

            Destroy(gameObject);
            return;
        }
        // =========================
        // 보스(부위) 처리 끝 — 이하 기존 적 처리 유지
        // =========================

        bool hitEnemy = collision.transform.root.CompareTag("Enemy");

        // === 기존 적 데미지 처리 (구조 유지)
        if (hitEnemy)
        {
            var enemy = collision.transform.root.GetComponent<EnemyController>();
            var suicide = collision.transform.root.GetComponent<SuicideEnemyController>();
            var flying = collision.transform.GetComponentInParent<FlyingEnemyController>();
            if (flying != null) flying.TakeDamage(_damage);

            float finalDamage = _damage;

            // (선택) 렌드 보너스 등 기존 보정
            if (enemy != null && _shooter != null)
            {
                float rendBonus = _shooter.GetRendBonusForEnemy(enemy); // 0~0.2 등
                if (rendBonus > 0f) finalDamage *= (1f + rendBonus);
            }

            if (enemy != null) enemy.TakeDamage(finalDamage);
            if (suicide != null) suicide.TakeDamage(finalDamage);

            if (HitmarkerManager.instance != null) HitmarkerManager.instance.ShowHitmarker();
            _shooter?.OnPlayerHitEnemyHook(enemy != null ? enemy : null);
            StatModifierManager.Instance?.OnPlayerHitEnemy();
        }

        // === 기존 관통 로직 유지
        bool shouldDestroy = true;
        if (hitEnemy && remainingPenetrations > 0)
        {
            remainingPenetrations--;

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

    // ▶ Trigger 히트박스(보스 부위)에 대응
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == shooter) return;

        if (hitParticlePrefab != null)
        {
            var particle = Instantiate(hitParticlePrefab, transform.position, transform.rotation);
            Destroy(particle, 1f);
        }

        // 부위
        var part = other.GetComponentInParent<DamageablePart>();
        if (part != null)
        {
            part.ApplyDamage(Mathf.RoundToInt(_damage));

            if (HitmarkerManager.instance != null) HitmarkerManager.instance.ShowHitmarker();
            _shooter?.OnPlayerHitEnemyHook(null);
            StatModifierManager.Instance?.OnPlayerHitEnemy();

            if (remainingPenetrations > 0) { remainingPenetrations--; return; }
            Destroy(gameObject);
            return;
        }

        // ✅ IHittable(= Beacon 등) 처리 추가
        if (TryDealHittable(other))
        {
            if (remainingPenetrations > 0) { remainingPenetrations--; return; }
            Destroy(gameObject);
            return;
        }
        // (다른 트리거와 충돌 시 특별 처리 필요하면 여기에 추가)
        // Destroy(gameObject);
    }
    // ✅ IHittable 찾고 즉시 데미지 주는 유틸 (Beacon 포함)
    bool TryDealHittable(Component hitComp)
    {
        if (hitComp == null) return false;

        // 1) 바로 붙어있나?
        if (hitComp.TryGetComponent<IHittable>(out var h1))
        {
            h1.OnHit(Mathf.RoundToInt(_damage));
            if (HitmarkerManager.instance) HitmarkerManager.instance.ShowHitmarker();
            _shooter?.OnPlayerHitEnemyHook(null);
            StatModifierManager.Instance?.OnPlayerHitEnemy();
            return true;
        }

        // 2) 부모에서 찾기(콜라이더가 자식일 수 있음)
        var h2 = hitComp.GetComponentInParent<IHittable>();
        if (h2 != null)
        {
            h2.OnHit(Mathf.RoundToInt(_damage));
            if (HitmarkerManager.instance) HitmarkerManager.instance.ShowHitmarker();
            _shooter?.OnPlayerHitEnemyHook(null);
            StatModifierManager.Instance?.OnPlayerHitEnemy();
            return true;
        }
        return false;
    }

    private IEnumerator ReenableCollision(Collider other, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_collider != null && other != null)
            Physics.IgnoreCollision(_collider, other, false);
    }
}

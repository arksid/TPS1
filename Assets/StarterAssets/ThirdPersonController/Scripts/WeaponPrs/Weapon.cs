using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Weapon : Item
{
    public enum FireMode { SemiAuto, Burst, FullAuto }
    public enum Handle { OneHanded = 1, TwoHanded = 2 }
    public enum WeaponCategory { Primary, Secondary, Special }

    [Header("General Settings")]
    [SerializeField] private Handle _type = Handle.TwoHanded;
    [SerializeField] private FireMode _fireMode = FireMode.SemiAuto;
    [SerializeField] private WeaponCategory _category = WeaponCategory.Primary;
    [SerializeField] private string _ammoID = "";
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireRate = 0.2f; // 발사 간격(초)
    [SerializeField] private int _clipSize = 30;
    [SerializeField] private int _burstCount = 3;
    [SerializeField] private float _burstInterval = 0.1f;

    [Header("Kickback")]
    [SerializeField] private float _handKick = 5f;
    [SerializeField] private float _bodyKick = 5f;

    [Header("Hand IK")]
    [SerializeField] private Vector3 _leftHandPosition = Vector3.zero;
    [SerializeField] private Vector3 _leftHandRotation = Vector3.zero;
    [SerializeField] private Vector3 _rightHandPosition = Vector3.zero;
    [SerializeField] private Vector3 _rightHandRotation = Vector3.zero;

    [Header("References")]
    [SerializeField] private Transform _muzzle = null;
    [SerializeField] private ParticleSystem _flash = null;

    [Header("Projectile")]
    [SerializeField] private Projectile _projectile = null;

    [Header("Recoil Preset")]
    [SerializeField] private WeaponRecoilPreset recoilPreset;

    [Header("Reload Settings")]
    [SerializeField] private float _reloadDuration = 2.0f;
    public float reloadDuration => _reloadDuration;

    [Header("Eject Prefabs")]
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private Transform casingEjectPoint;
    [SerializeField] private GameObject magazinePrefab;
    [SerializeField] private Transform magazineDropPoint;

    [Header("Magazine Mesh")]
    [SerializeField] private GameObject magazineMesh;

    // 런타임 반동 값
    private float verticalRecoil;
    private float horizontalRecoil;
    public static float recoilX = 0f;
    public static float recoilY = 0f;
    public static float recoveryX = 8f;
    public static float recoveryY = 6f;

    // Runtime
    private int _ammo = 0;
    private float _fireTimer = 0;
    private bool _isFiring = false;
    public static bool IsPaused = false; // UI 중 사격 잠금용

    // ===== 프로퍼티 =====
    public Handle type => _type;
    public FireMode fireMode => _fireMode;
    public WeaponCategory category => _category;
    public string ammoID => _ammoID;
    public int clipSize => _clipSize;
    public int ammo { get => _ammo; set => _ammo = value; }
    public float handKick => _handKick;
    public float bodyKick => _bodyKick;
    public Transform muzzle => _muzzle;
    public float fireRate { get => _fireRate; set => _fireRate = value; }

    public Vector3 leftHandPosition => _leftHandPosition;
    public Vector3 leftHandRotation => _leftHandRotation;
    public Vector3 rightHandPosition => _rightHandPosition;
    public Vector3 rightHandRotation => _rightHandRotation;

    private void Awake()
    {
        _fireTimer = Time.realtimeSinceStartup;

        if (recoilPreset != null)
        {
            verticalRecoil = recoilPreset.verticalRecoil;
            horizontalRecoil = recoilPreset.horizontalRecoil;
            recoveryX = recoilPreset.recoveryX;
            recoveryY = recoilPreset.recoveryY;
        }
    }

    // ===== 발사 관련 =====
    public void StartFiring(Character character, Func<Vector3> getTarget, MonoBehaviour caller)
        => StartFiring(character, getTarget, caller, null, null, null);

    public void StartFiring(
    Character character,
    Func<Vector3> getTarget,
    MonoBehaviour caller,
    Func<bool> isAimingProvider,
    Func<float> moveMagnitudeProvider,
    Func<bool> isSprintingProvider)
    {
        if (IsPaused) return;  // ✅ 증강 UI 열려있을 땐 사격 차단

        if (_isFiring) return;
        _isFiring = true;

        switch (_fireMode)
        {
            case FireMode.SemiAuto:
                {
                    var target = ComputeTarget(getTarget);
                    TryShoot(character, target);
                    _isFiring = false;
                    break;
                }
            case FireMode.Burst:
                caller.StartCoroutine(FireBurst(character, () => ComputeTarget(getTarget)));
                break;
            case FireMode.FullAuto:
                caller.StartCoroutine(FireContinuously(character, () => ComputeTarget(getTarget)));
                break;
        }
    }


    public void StopFiring() => _isFiring = false;

    private bool TryShoot(Character character, Vector3 target)
    {
        // ✅ 캐릭터가 null이거나 이미 삭제됐다면 발사 중단
        if (character == null || character.Equals(null))
        {
            StopFiring();
            return false;
        }
        // ✅ 교체 후: 증강 연사배율(크면 더 빨라지도록 "나누기") + 궁극기 배율 반영
        float rateMul = (StatModifierManager.Instance != null) ? StatModifierManager.Instance.FireRateMultiplier : 1f;
        float effectiveInterval = (_fireRate / Mathf.Max(rateMul, 0.01f)) *
                                  (UltimateSkill.IsUltimateActive ? UltimateSkill.CurrentFireRateMultiplier : 1f);

        float passedTime = Time.realtimeSinceStartup - _fireTimer;
        bool canShootByAmmo = UltimateSkill.IsUltimateActive || _ammo > 0;

        if (canShootByAmmo && passedTime >= effectiveInterval)
        {
            if (!UltimateSkill.IsUltimateActive) _ammo--;

            _fireTimer = Time.realtimeSinceStartup;
            Vector3 spawnPos = _muzzle != null ? _muzzle.position : transform.position;

            var p = UnityEngine.Object.Instantiate(_projectile, spawnPos, Quaternion.identity);

            // 🔁 교체 전
            // float shotDamage = _damage * (UltimateSkill.IsUltimateActive ? UltimateSkill.CurrentDamageMultiplier : 1f);

            // ✅ 교체 후: 증강 데미지배율 * 궁극기 배율
            float dmgMul = (StatModifierManager.Instance != null) ? StatModifierManager.Instance.DamageMultiplier : 1f;
            float shotDamage = _damage * dmgMul *
                               (UltimateSkill.IsUltimateActive ? UltimateSkill.CurrentDamageMultiplier : 1f);


            // ✅ 다시 한 번 안전하게 확인
            if (character != null && !character.Equals(null))
            {
                p.Initialize(character, target, shotDamage);
                p.shooter = character.gameObject;
            }

            _flash?.Play();

            recoilY += verticalRecoil;
            recoilX += UnityEngine.Random.Range(-horizontalRecoil, horizontalRecoil);

            EjectCasing();

            if (CanvasManager.singleton != null)
                CanvasManager.singleton.UpdateAmmo(_ammo, character?.ammo?.amount ?? 0);

            return true;
        }
        return false;
    }


    private IEnumerator FireBurst(Character character, Func<Vector3> getTarget)
    {
        for (int i = 0; i < _burstCount; i++)
        {
            if (!_isFiring) break;
            if (!TryShoot(character, getTarget())) break;

            // 매 탄 사이 간격도 궁극기 배율 고려(원하면)
            // 🔁 교체 전
            // float interval = UltimateSkill.IsUltimateActive ? _burstInterval * UltimateSkill.CurrentFireRateMultiplier : _burstInterval;

            // ✅ 교체 후
            float rateMul = (StatModifierManager.Instance != null) ? StatModifierManager.Instance.FireRateMultiplier : 1f;
            float interval = (_burstInterval / Mathf.Max(rateMul, 0.01f)) *
                             (UltimateSkill.IsUltimateActive ? UltimateSkill.CurrentFireRateMultiplier : 1f);

            yield return new WaitForSeconds(interval);
        }
        _isFiring = false;
    }


    private IEnumerator FireContinuously(Character character, Func<Vector3> getTarget)
    {
        while (_isFiring)
        {
            if (IsPaused) yield break;
            if (character == null || character.Equals(null)) yield break; // ✅ 캐릭터 사망 시 종료

            TryShoot(character, getTarget());
            float rateMul = (StatModifierManager.Instance != null) ? StatModifierManager.Instance.FireRateMultiplier : 1f;
            float wait = (_fireRate / Mathf.Max(rateMul, 0.01f)) *
                         (UltimateSkill.IsUltimateActive ? UltimateSkill.CurrentFireRateMultiplier : 1f);
            yield return new WaitForSeconds(wait);
        }
    }


    public float VisualSpreadDeg(bool aiming, float moveMagnitude, bool sprinting) => 0f;

    private Vector3 ComputeTarget(Func<Vector3> getTarget)
    {
        if (getTarget != null) return getTarget();
        return _muzzle != null ? _muzzle.position + _muzzle.forward * 1000f
                               : transform.position + transform.forward * 1000f;
    }

    private void EjectCasing()
    {
        if (casingPrefab != null && casingEjectPoint != null)
        {
            var casing = UnityEngine.Object.Instantiate(casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);
            var rb = casing.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(casingEjectPoint.right * 1.5f + casingEjectPoint.up * 0.5f, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
            UnityEngine.Object.Destroy(casing, 5f);
        }
    }

    public void DropMagazine()
    {
        if (magazinePrefab != null && magazineDropPoint != null)
        {
            var mag = UnityEngine.Object.Instantiate(magazinePrefab, magazineDropPoint.position, magazineDropPoint.rotation);
            var rb = mag.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
            UnityEngine.Object.Destroy(mag, 10f);
        }
    }

    public void DropToGround()
    {
#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            Debug.LogError("Prefab Asset 자체를 드랍하려고 했습니다. 반드시 인스턴스를 사용하세요.");
            return;
        }
#endif
        transform.SetParent(null);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length == 0) colliders = new Collider[] { gameObject.AddComponent<BoxCollider>() };

        if (colliders.Length > 0)
        {
            colliders[0].enabled = true;
            colliders[0].isTrigger = true; // 줍기 트리거
        }
        for (int i = 1; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
            colliders[i].isTrigger = false; // 물리 충돌
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        for (int i = 1; i < colliders.Length; i++)
        {
            if (colliders[i].sharedMaterial == null)
            {
                PhysicMaterial pm = new PhysicMaterial
                {
                    bounciness = 0.4f,
                    frictionCombine = PhysicMaterialCombine.Multiply,
                    bounceCombine = PhysicMaterialCombine.Maximum
                };
                colliders[i].sharedMaterial = pm;
            }
        }

        Vector3 dropDir = (transform.forward + Vector3.up * 0.5f).normalized;
        rb.AddForce(dropDir * 3f, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.insideUnitSphere * 3f, ForceMode.Impulse);
    }

    public void EquipWeapon(Weapon weapon, Transform weaponHolder, RigManager rigManager)
    {
        weapon.transform.SetParent(weaponHolder);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;

        rigManager.SetLeftHandGrioData(weapon.leftHandPosition, weapon.leftHandRotation);
    }
    // ✅ 데미지 접근자
    public float damage
    {
        get => _damage;
        set => _damage = value;
    }
    // ✅ 반동 감소용 함수
    public void ApplyRecoilMultiplier(float multiplier)
    {
        recoilX *= multiplier;
        recoilY *= multiplier;
    }
    public void HideMagazineMesh() { if (magazineMesh != null) magazineMesh.SetActive(false); }
    public void ShowMagazineMesh() { if (magazineMesh != null) magazineMesh.SetActive(true); }
}

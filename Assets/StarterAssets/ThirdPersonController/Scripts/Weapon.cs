// Weapon.cs
using System;
using System.Collections;
using UnityEngine;

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
    [SerializeField] private float _fireRate = 0.2f;
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
    [SerializeField] private Transform _muzzle = null;   // 총구 Transform
    [SerializeField] private ParticleSystem _flash = null;

    [Header("Projectile")]
    [SerializeField] private Projectile _projectile = null;

    [Header("Spread (Preset)")]
    [SerializeField] private WeaponSpreadPreset _spreadPreset;

    [Header("Spread (Local Fallback)")]
    [SerializeField] private float _hipFireSpread = 3f;
    [SerializeField] private float _aimSpread = 1f;
    [SerializeField] private float _moveSpread = 2f;
    [SerializeField] private float _sprintSpread = 6f;

    [Header("Bloom (Local Fallback)")]
    [SerializeField] private float _bloomPerShot = 0.3f;
    [SerializeField] private float _bloomDecayPerSec = 2f;
    [SerializeField] private float _maxBloom = 5f;

    // Runtime
    private float _currentBloom = 0f;
    private int _ammo = 0;
    private float _fireTimer = 0;
    private bool _isFiring = false;

    public Handle type => _type;
    public FireMode fireMode => _fireMode;
    public WeaponCategory category => _category;
    public string ammoID => _ammoID;
    public int clipSize => _clipSize;
    public float handKick => _handKick;
    public float bodyKick => _bodyKick;
    public Vector3 leftHandPosition => _leftHandPosition;
    public Vector3 leftHandRotation => _leftHandRotation;
    public Vector3 rightHandPosition => _rightHandPosition;
    public Vector3 rightHandRotation => _rightHandRotation;
    public int ammo { get => _ammo; set => _ammo = value; }

    // ★ 총구 프로퍼티 공개
    public Transform muzzle => _muzzle;

    private void Awake()
    {
        _fireTimer = Time.realtimeSinceStartup;
    }

    // 기존 시그니처(호환성)
    public void StartFiring(Character character, Func<Vector3> getTarget, MonoBehaviour caller)
        => StartFiring(character, getTarget, caller, null, null, null);

    // 상태(조준/이동/스프린트) 전달 시그니처
    public void StartFiring(
        Character character,
        Func<Vector3> getTarget,
        MonoBehaviour caller,
        Func<bool> isAimingProvider,
        Func<float> moveMagnitudeProvider,
        Func<bool> isSprintingProvider)
    {
        if (_isFiring) return;
        _isFiring = true;

        switch (_fireMode)
        {
            case FireMode.SemiAuto:
                {
                    var targetWithSpread = ComputeTargetWithSpread(getTarget, isAimingProvider, moveMagnitudeProvider, isSprintingProvider);
                    TryShoot(character, targetWithSpread);
                    _isFiring = false;
                    break;
                }
            case FireMode.Burst:
                caller.StartCoroutine(FireBurst(character, () =>
                    ComputeTargetWithSpread(getTarget, isAimingProvider, moveMagnitudeProvider, isSprintingProvider)));
                break;
            case FireMode.FullAuto:
                caller.StartCoroutine(FireContinuously(character, () =>
                    ComputeTargetWithSpread(getTarget, isAimingProvider, moveMagnitudeProvider, isSprintingProvider)));
                break;
        }
    }

    public void StopFiring() => _isFiring = false;

    private bool TryShoot(Character character, Vector3 target)
    {
        float passedTime = Time.realtimeSinceStartup - _fireTimer;
        if (_ammo > 0 && passedTime >= _fireRate)
        {
            _ammo--;
            _fireTimer = Time.realtimeSinceStartup;

            var spawnPos = _muzzle != null ? _muzzle.position : transform.position;
            var p = UnityEngine.Object.Instantiate(_projectile, spawnPos, Quaternion.identity);
            p.Initialize(character, target, _damage);
            _flash?.Play();

            var cfg = GetSpreadConfig();
            _currentBloom = Mathf.Min(_currentBloom + cfg.bloomPerShot, cfg.maxBloom);
            return true;
        }
        return false;
    }

    private IEnumerator FireBurst(Character character, Func<Vector3> getTarget)
    {
        for (int i = 0; i < _burstCount; i++)
        {
            if (!_isFiring || !TryShoot(character, getTarget())) break;
            yield return new WaitForSeconds(_burstInterval);
        }
        _isFiring = false;
    }

    private IEnumerator FireContinuously(Character character, Func<Vector3> getTarget)
    {
        while (_isFiring)
        {
            TryShoot(character, getTarget());
            yield return new WaitForSeconds(_fireRate);
        }
    }

    // UI용 현재 퍼짐(도)
    public float VisualSpreadDeg(bool aiming, float moveMagnitude, bool sprinting)
    {
        var cfg = GetSpreadConfig();

        float baseSpread = (aiming ? cfg.aim : cfg.hip)
                         + Mathf.Clamp01(moveMagnitude) * cfg.move
                         + (sprinting ? cfg.sprint : 0f);

        float sinceLastShot = Time.realtimeSinceStartup - _fireTimer;
        float decayedBloom = Mathf.Max(0f, _currentBloom - cfg.bloomDecayPerSec * sinceLastShot);

        return Mathf.Min(baseSpread + decayedBloom, baseSpread + cfg.maxBloom);
    }

    // 발사용 타겟 계산(퍼짐/블룸 적용)
    private Vector3 ComputeTargetWithSpread(
        Func<Vector3> getTarget,
        Func<bool> isAimingProvider,
        Func<float> moveMagnitudeProvider,
        Func<bool> isSprintingProvider)
    {
        var cfg = GetSpreadConfig();

        Vector3 baseTarget =
            getTarget != null ? getTarget()
            : (_muzzle != null ? _muzzle.position + transform.forward * 1000f
                               : transform.position + transform.forward * 1000f);

        Vector3 muzzlePos = _muzzle != null ? _muzzle.position : transform.position;
        Vector3 baseDir = (baseTarget - muzzlePos).normalized;

        bool aiming = isAimingProvider != null && isAimingProvider.Invoke();
        float moveMag = moveMagnitudeProvider != null ? Mathf.Clamp01(moveMagnitudeProvider.Invoke()) : 0f;
        bool sprinting = isSprintingProvider != null && isSprintingProvider.Invoke();

        float baseSpreadDeg = aiming ? cfg.aim : cfg.hip;
        baseSpreadDeg += moveMag * cfg.move;
        if (sprinting) baseSpreadDeg += cfg.sprint;

        float sinceLastShot = Time.realtimeSinceStartup - _fireTimer;
        _currentBloom = Mathf.Max(0f, _currentBloom - cfg.bloomDecayPerSec * sinceLastShot);

        float totalSpreadDeg = Mathf.Min(baseSpreadDeg + _currentBloom, baseSpreadDeg + cfg.maxBloom);

        Vector3 forward = _muzzle != null ? _muzzle.forward : baseDir;
        Vector3 right = _muzzle != null ? _muzzle.right : Vector3.right;
        Vector3 up = _muzzle != null ? _muzzle.up : Vector3.up;

        Vector3 spreadDir = ApplySpread(forward, totalSpreadDeg, right, up);
        return muzzlePos + spreadDir * 1000f;
    }

    private Vector3 ApplySpread(Vector3 forward, float degrees, Vector3 localRight, Vector3 localUp)
    {
        if (degrees <= 0.001f) return forward.normalized;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * degrees;
        Quaternion yaw = Quaternion.AngleAxis(offset.x, localUp);
        Quaternion pitch = Quaternion.AngleAxis(-offset.y, localRight);

        return (yaw * pitch * forward).normalized;
    }

    // 프리셋/로컬 선택
    protected (float hip, float aim, float move, float sprint, float bloomPerShot, float bloomDecayPerSec, float maxBloom) GetSpreadConfig()
    {
        if (_spreadPreset != null)
        {
            return (
                _spreadPreset.hipFireSpread,
                _spreadPreset.aimSpread,
                _spreadPreset.moveSpread,
                _spreadPreset.sprintSpread,
                _spreadPreset.bloomPerShot,
                _spreadPreset.bloomDecayPerSec,
                _spreadPreset.maxBloom
            );
        }
        return (
            _hipFireSpread,
            _aimSpread,
            _moveSpread,
            _sprintSpread,
            _bloomPerShot,
            _bloomDecayPerSec,
            _maxBloom
        );
    }
}

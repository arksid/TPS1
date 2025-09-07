// Weapon.cs
using System;
using System.Collections;
using UnityEngine;

public class Weapon : Item
{
    public enum FireMode { SemiAuto, Burst, FullAuto }
    public enum Handle { OneHanded = 1, TwoHanded = 2 }
    public enum WeaponCategory { Primary, Secondary, Special }

    [Header("General")]
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

    [Header("IK Targets")]
    public Transform leftHandTarget;   // 왼손 그립 기준(위치/회전)
    public Transform leftElbowHint;    // (선택) 팔꿈치 힌트 — 비워도 동작
    public bool keepGripInAir = true;  // 점프/비조준에서도 그립 유지

    [Header("Muzzle / VFX / Projectile")]
    [SerializeField] private Transform _muzzle = null;
    [SerializeField] private ParticleSystem _flash = null;
    [SerializeField] private Projectile _projectile = null;

    // 호환용(필요시 채우기)
    public Vector3 rightHandPosition => Vector3.zero;
    public Vector3 rightHandRotation => Vector3.zero;

    public Handle type => _type;
    public FireMode fireMode => _fireMode;
    public WeaponCategory category => _category;
    public string ammoID => _ammoID;
    public int clipSize => _clipSize;
    public float handKick => _handKick;
    public float bodyKick => _bodyKick;

    private int _ammo = 0; public int ammo { get => _ammo; set => _ammo = value; }
    private float _fireTimer = 0;
    private bool _isFiring = false;

    private void Awake() => _fireTimer = Time.realtimeSinceStartup;

    public bool StartFiring(Character character, Func<Vector3> getTarget, MonoBehaviour caller)
    {
        if (_isFiring) return false;
        _isFiring = true;

        switch (_fireMode)
        {
            case FireMode.SemiAuto:
                bool success = TryShoot(character, getTarget());
                _isFiring = false;
                return success;

            case FireMode.Burst:
                caller.StartCoroutine(FireBurst(character, getTarget));
                break;

            case FireMode.FullAuto:
                caller.StartCoroutine(FireContinuously(character, getTarget));
                break;
        }
        return true;
    }

    public void StopFiring() => _isFiring = false;

    private bool TryShoot(Character character, Vector3 target)
    {
        float passed = Time.realtimeSinceStartup - _fireTimer;
        if (_ammo > 0 && passed >= _fireRate)
        {
            _ammo--;
            _fireTimer = Time.realtimeSinceStartup;

            if (_projectile != null && _muzzle != null)
            {
                var p = Instantiate(_projectile, _muzzle.position, Quaternion.identity);
                p.Initialize(character, target, _damage);
            }
            _flash?.Play();
            character.GetComponent<RigManager>()?.ApplyWeaponKick(_handKick, _bodyKick);
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
}

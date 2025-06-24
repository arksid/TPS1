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
    [SerializeField] private Transform _muzzle = null;
    [SerializeField] private ParticleSystem _flash = null;

    [Header("Projectile")]
    [SerializeField] private Projectile _projectile = null;

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

    private void Awake()
    {
        _fireTimer = Time.realtimeSinceStartup;
    }

    public void StartFiring(Character character, Func<Vector3> getTarget, MonoBehaviour caller)
    {
        if (_isFiring) return;
        _isFiring = true;

        switch (_fireMode)
        {
            case FireMode.SemiAuto:
                TryShoot(character, getTarget());
                _isFiring = false;
                break;
            case FireMode.Burst:
                caller.StartCoroutine(FireBurst(character, getTarget));
                break;
            case FireMode.FullAuto:
                caller.StartCoroutine(FireContinuously(character, getTarget));
                break;
        }
    }

    public void StopFiring()
    {
        _isFiring = false;
    }

    private bool TryShoot(Character character, Vector3 target)
    {
        float passedTime = Time.realtimeSinceStartup - _fireTimer;
        if (_ammo > 0 && passedTime >= _fireRate)
        {
            _ammo--;
            _fireTimer = Time.realtimeSinceStartup;
            var p = UnityEngine.Object.Instantiate(_projectile, _muzzle.position, Quaternion.identity);
            p.Initialize(character, target, _damage);
            _flash?.Play();
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

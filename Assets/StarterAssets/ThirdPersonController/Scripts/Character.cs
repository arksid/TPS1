// Character.cs
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Startup")]
    // 0: Primary1, 1: Primary2, 2: Secondary(3번 키), 3: Special
    [SerializeField] private int startSlotIndex = 2;

    [Header("Components / Refs")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;
    [SerializeField] private Transform _weaponHolder = null;

    [Header("Health")]
    [SerializeField] private int _health;
    [SerializeField] public int MaxHealth = 100;

    private readonly List<Item> _items = new List<Item>();
    private Weapon _weapon = null; public Weapon weapon => _weapon;
    private Ammo _ammo = null; public Ammo ammo => _ammo;

    private Animator _animator = null;
    private RigManager _rigManager = null;
    private Weapon _weaponToEquip = null;

    public List<Item> weaponItems => _items;

    private bool _reloading = false; public bool reloading => _reloading;
    private bool _switchingWeapon = false; public bool switchingWeapon => _switchingWeapon;
    public bool isInvincible = false;

    private static readonly int EquipTrigger = Animator.StringToHash("Equip");
    private static readonly int HolsterTrigger = Animator.StringToHash("Holster");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");

    private void Awake()
    {
        _rigManager = GetComponent<RigManager>();
        _animator = GetComponent<Animator>();
        // 필요 시 초기 인벤토리 세팅 호출
        // Initialized(...);
    }

    public int Health
    {
        get => _health;
        set
        {
            _health = Mathf.Clamp(value, 0, MaxHealth);
            CanvasManager.singleton?.UpdateHealth(_health, MaxHealth);
        }
    }

    public void Initialized(Dictionary<string, int> items)
    {
        if (items == null || PrefabManager.singleton == null) return;

        int firstWeaponIndex = -1;

        foreach (var itemData in items)
        {
            Item prefab = PrefabManager.singleton.GetItemPrefab(itemData.Key);
            if (prefab == null || itemData.Value <= 0) continue;

            for (int i = 0; i < itemData.Value; i++)
            {
                bool isAmmoHandled = false;
                Item item = Instantiate(prefab, transform);

                if (item is Weapon w)
                {
                    if (_weaponHolder != null)
                    {
                        item.transform.SetParent(_weaponHolder);
                        item.transform.localPosition = w.rightHandPosition;
                        item.transform.localEulerAngles = w.rightHandRotation;
                    }
                    if (firstWeaponIndex < 0) firstWeaponIndex = _items.Count;
                }
                else if (item is Ammo a)
                {
                    a.amount = itemData.Value;
                    isAmmoHandled = true;
                }

                item.gameObject.SetActive(false);
                _items.Add(item);

                if (isAmmoHandled) break;
            }
        }

        // 시작 무기: 지정 슬롯 우선 → 없으면 첫 무기
        if (_weapon == null)
        {
            Weapon preferred = GetWeaponBySlotIndex(startSlotIndex);
            if (preferred != null)
            {
                _weaponToEquip = preferred;
                EquipImmediately();
            }
            else if (firstWeaponIndex >= 0)
            {
                _weaponToEquip = (Weapon)_items[firstWeaponIndex];
                EquipImmediately();
            }
        }
    }

    /// <summary>슬롯 인덱스로 무기 가져오기(0/1=Primary1/2, 2=Secondary, 3=Special)</summary>
    public Weapon GetWeaponBySlotIndex(int slotIndex)
    {
        Weapon.WeaponCategory categoryToSearch;
        int categoryIndex;

        switch (slotIndex)
        {
            case 0:
            case 1:
                categoryToSearch = Weapon.WeaponCategory.Primary;
                categoryIndex = slotIndex;
                break;
            case 2:
                categoryToSearch = Weapon.WeaponCategory.Secondary;
                categoryIndex = 0;
                break;
            case 3:
                categoryToSearch = Weapon.WeaponCategory.Special;
                categoryIndex = 0;
                break;
            default:
                return null;
        }

        int found = 0;
        foreach (var item in _items)
        {
            if (item is Weapon w && w.category == categoryToSearch)
            {
                if (found == categoryIndex) return w;
                found++;
            }
        }
        return null;
    }

    public void EquipWeapon(Weapon newWeapon)
    {
        if (_switchingWeapon || newWeapon == null || _weapon == newWeapon) return;

        _weaponToEquip = newWeapon;

        if (_weapon != null)
        {
            HolsterWeapon();
        }
        else
        {
            _switchingWeapon = true;
            _animator?.SetTrigger(EquipTrigger);
        }
    }

    /// <summary>OnEquip(애니메이션 이벤트)에서 호출하거나, 즉시 장착 용도로 직접 호출</summary>
    public void EquipImmediately()
    {
        if (_weaponToEquip == null) return;

        _weapon = _weaponToEquip;
        _weaponToEquip = null;

        CanvasManager.singleton?.UpdateWeapon(_weapon.id);
        CanvasManager.singleton?.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);

        if (_weaponHolder != null && _weapon.transform.parent != _weaponHolder)
        {
            _weapon.transform.SetParent(_weaponHolder);
            _weapon.transform.localPosition = _weapon.rightHandPosition;
            _weapon.transform.localEulerAngles = _weapon.rightHandRotation;
        }

        // 왼손 그립: Transform 하나로 위치/회전 모두 사용(기존 API 시그니처 유지)
        if (_weapon.leftHandTarget != null)
        {
            _rigManager?.SetLeftHandGrioData(_weapon.leftHandTarget, _weapon.leftHandTarget);
        }
        else
        {
            Debug.LogWarning($"[IK] {_weapon.id} 의 leftHandTarget 미설정");
        }

        // 왼팔 IK 타깃/힌트(힌트 null 가능 → RigManager가 자동 힌트 생성)
        _rigManager?.SetLeftArmTargets(_weapon.leftHandTarget, _weapon.leftElbowHint);

        _weapon.gameObject.SetActive(true);

        // 탄약 할당
        _ammo = null;
        foreach (var item in _items)
        {
            if (item is Ammo a && _weapon.ammoID == a.id) { _ammo = a; break; }
        }
    }

    public void OnEquip() => EquipImmediately();

    public void HolsterWeapon()
    {
        if (_switchingWeapon) return;

        if (_weapon != null)
        {
            _switchingWeapon = true;
            _animator?.SetTrigger(HolsterTrigger);
        }
    }

    public void OnHolster()
    {
        if (_weapon != null)
        {
            _weapon.gameObject.SetActive(false);
            _weapon = null;
            _ammo = null;
        }
        if (_weaponToEquip != null) EquipImmediately();
    }

    public void ApplyDamage(Character shooter, Transform hit, float damage)
    {
        if (isInvincible) return;

        Health -= (int)damage;

        if (_health <= 0)
        {
            GetComponent<RagdollController>()?.ActivateRagdoll();

            foreach (var script in scriptsToDisableOnDeath)
                if (script != null) script.enabled = false;

            Destroy(this);
        }
    }

    public void Reload()
    {
        if (_weapon != null && !_reloading && _weapon.ammo < _weapon.clipSize && _ammo != null && _ammo.amount > 0)
        {
            _animator?.SetTrigger(ReloadTrigger);
            _reloading = true;
        }
    }

    public void ReloadFinished()
    {
        if (_weapon != null && _weapon.ammo < _weapon.clipSize && _ammo != null && _ammo.amount > 0)
        {
            int amount = Mathf.Min(_weapon.clipSize - _weapon.ammo, _ammo.amount);
            _ammo.amount -= amount;
            _weapon.ammo += amount;
        }
        _reloading = false;
        CanvasManager.singleton?.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);
    }

    public void HolsterFinished() => _switchingWeapon = false;
    public void EquipFinished() => _switchingWeapon = false;
}

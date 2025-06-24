// Character.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Character : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;
    [SerializeField] private Transform _weaponHolder = null;
    [SerializeField] private int _health;
    [SerializeField] public int MaxHealth = 100;

    private Weapon _weapon = null; public Weapon weapon { get { return _weapon; } }
    private Ammo _ammo = null; public Ammo ammo { get { return _ammo; } }
    private List<Item> _items = new List<Item>();
    private Animator _animator = null;
    private RigManager _rigManager = null;
    private Weapon _weaponToEquip = null;
    public List<Item> weaponItems => _items;
    private bool _reloading = false; public bool reloading { get { return _reloading; } }
    private bool _switchingWeapon = false; public bool switchingWeapon { get { return _switchingWeapon; } }
    public bool isInvincible = false;

    private void Awake()
    {
        _rigManager = GetComponent<RigManager>();
        _animator = GetComponent<Animator>();
        Initialized(new Dictionary<string, int> { { "HK416", 1 }, { "K-2", 1 }, { "KG-9", 1 }, { "9mm", 1000 } });
    }

    public int Health
    {
        get => _health;
        set
        {
            _health = Mathf.Clamp(value, 0, MaxHealth);
            CanvasManager.singleton.UpdateHealth(_health, MaxHealth);
        }
    }

    public void Initialized(Dictionary<string, int> items)
    {
        if (items != null && PrefabManager.singleton != null)
        {
            int firstWeaponIndex = -1;
            foreach (var itemData in items)
            {
                Item prefab = PrefabManager.singleton.GetItemPrefab(itemData.Key);
                if (prefab != null && itemData.Value > 0)
                {
                    for (int i = 0; i < itemData.Value; i++)
                    {
                        bool done = false;
                        Item item = Instantiate(prefab, transform);

                        if (item is Weapon w)
                        {
                            item.transform.SetParent(_weaponHolder);
                            item.transform.localPosition = w.rightHandPosition;
                            item.transform.localEulerAngles = w.rightHandRotation;
                            if (firstWeaponIndex < 0)
                            {
                                firstWeaponIndex = _items.Count;
                            }
                        }
                        else if (item is Ammo a)
                        {
                            a.amount = itemData.Value;
                            done = true;
                        }
                        item.gameObject.SetActive(false);
                        _items.Add(item);
                        if (done) break;
                    }
                }
            }
            if (firstWeaponIndex >= 0 && _weapon == null)
            {
                _weaponToEquip = (Weapon)_items[firstWeaponIndex];
                OnEquip();
            }
        }
    }

    public Weapon GetWeaponBySlotIndex(int slotIndex)
    {
        Weapon.WeaponCategory categoryToSearch;
        int categoryIndex = 0;

        switch (slotIndex)
        {
            case 0:
            case 1:
                categoryToSearch = Weapon.WeaponCategory.Primary;
                categoryIndex = slotIndex; // 0 또는 1
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
            if (item is Weapon weapon && weapon.category == categoryToSearch)
            {
                if (found == categoryIndex)
                    return weapon;
                found++;
            }
        }
        return null;
    }

    public void EquipWeapon(Weapon weapon)
    {
        if (_switchingWeapon || weapon == null || _weapon == weapon)
            return;

        _weaponToEquip = weapon;

        if (_weapon != null)
        {
            Holsterweapon();
        }
        else
        {
            _switchingWeapon = true;
            _animator.SetTrigger("Equip");
        }
    }

    public void _EquipWeapon()
    {
        if (_weaponToEquip != null)
        {
            _weapon = _weaponToEquip;
            _weaponToEquip = null;

            CanvasManager.singleton.UpdateWeapon(_weapon.id);
            CanvasManager.singleton.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);
            if (_weapon.transform.parent != _weaponHolder)
            {
                _weapon.transform.SetParent(_weaponHolder);
                _weapon.transform.localPosition = _weapon.rightHandPosition;
                _weapon.transform.localEulerAngles = _weapon.rightHandRotation;
            }

            if (_weapon.leftHandPosition != null && _weapon.leftHandRotation != null)
            {
                _rigManager.SetLeftHandGrioData(_weapon.leftHandPosition, _weapon.leftHandRotation);
            }
            else
            {
                Debug.LogWarning($"[무기 장착 오류] 무기 {_weapon.id}의 왼손 위치 또는 회전이 설정되지 않았습니다.");
            }
            _weapon.gameObject.SetActive(true);
            _ammo = null;
            foreach (var item in _items)
            {
                if (item is Ammo a && _weapon.ammoID == a.id)
                {
                    _ammo = a;
                    break;
                }
            }
        }
    }

    public void OnEquip() => _EquipWeapon();

    private void _Holsterweapon()
    {
        if (_weapon != null)
        {
            _weapon.gameObject.SetActive(false);
            _weapon = null;
            _ammo = null;
        }
    }

    public void Holsterweapon()
    {
        if (_switchingWeapon) return;

        if (_weapon != null)
        {
            _switchingWeapon = true;
            _animator.SetTrigger("Holster");
        }
    }

    public void OnHolster()
    {
        _Holsterweapon();
        if (_weaponToEquip != null)
        {
            OnEquip();
        }
    }

    public void ApplyDamage(Character shooter, Transform hit, float damage)
    {
        if (isInvincible) return;

        Health -= (int)damage;

        if (_health <= 0)
        {
            GetComponent<RagdollController>()?.ActivateRagdoll();

            foreach (var script in scriptsToDisableOnDeath)
            {
                if (script != null) script.enabled = false;
            }

            Destroy(this);
        }
    }

    public void Reload()
    {
        if (_weapon != null && !_reloading && _weapon.ammo < _weapon.clipSize && _ammo != null && _ammo.amount > 0)
        {
            _animator.SetTrigger("Reload");
            _reloading = true;
        }
    }

    public void ReloadFinished()
    {
        if (_weapon != null && _weapon.ammo < _weapon.clipSize && _ammo != null && _ammo.amount > 0)
        {
            int amount = _weapon.clipSize - _weapon.ammo;
            if (_ammo.amount < amount)
            {
                amount = _ammo.amount;
            }
            _ammo.amount -= amount;
            _weapon.ammo += amount;
        }
        _reloading = false;
        CanvasManager.singleton.UpdateAmmo(_weapon.ammo, _ammo.amount);
    }

    public void HolsterFinished() => _switchingWeapon = false;
    public void EquipFinished() => _switchingWeapon = false;
}

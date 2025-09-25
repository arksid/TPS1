using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;
    [SerializeField] private Transform _weaponHolder = null;
    [SerializeField] private int _health;
    [SerializeField] public int MaxHealth = 100;

    private Weapon _weapon = null; public Weapon weapon => _weapon;
    private Ammo _ammo = null; public Ammo ammo => _ammo;
    private readonly List<Item> _items = new List<Item>();

    private Animator _animator = null;
    private RigManager _rigManager = null;
    private Weapon _weaponToEquip = null;

    public List<Item> weaponItems => _items;

    private bool _reloading = false; public bool reloading => _reloading;
    private bool _switchingWeapon = false; public bool switchingWeapon => _switchingWeapon;
    public bool isInvincible = false;

    // 🔹 Animator hash 캐싱
    private static readonly int EquipTrigger = Animator.StringToHash("Equip");
    private static readonly int HolsterTrigger = Animator.StringToHash("Holster");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");

    private void Awake()
    {
        _rigManager = GetComponent<RigManager>();
        _animator = GetComponent<Animator>();

        // 예시 초기화 (프로토타입용)
        Initialized(new Dictionary<string, int>
        {
            { "HK416", 1 }, { "K-2", 1 }, { "KG-9", 1 }, { "9mm", 1000 }
        });
    }

    public int Health
    {
        get => _health;
        set
        {
            int oldHealth = _health;
            _health = Mathf.Clamp(value, 0, MaxHealth);

            Debug.Log($"[Character] Health changed: {oldHealth} → {_health} / {MaxHealth}");

            if (CanvasManager.singleton != null)
                CanvasManager.singleton.UpdateHealth(_health, MaxHealth);
        }
    }

    public void Initialized(Dictionary<string, int> items)
    {
        if (items == null || PrefabManager.singleton == null) return;

        int firstWeaponIndex = -1;
        foreach (var itemData in items)
        {
            Item prefab = PrefabManager.singleton.GetItemPrefab(itemData.Key);
            if (prefab != null && itemData.Value > 0)
            {
                for (int i = 0; i < itemData.Value; i++)
                {
                    bool isAmmoHandled = false;
                    Item item = Instantiate(prefab, transform);

                    if (item is Weapon w)
                    {
                        item.transform.SetParent(_weaponHolder);
                        item.transform.localPosition = w.rightHandPosition;
                        item.transform.localEulerAngles = w.rightHandRotation;

                        if (firstWeaponIndex < 0)
                            firstWeaponIndex = _items.Count;
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
        }

        if (firstWeaponIndex >= 0 && _weapon == null)
        {
            _weaponToEquip = (Weapon)_items[firstWeaponIndex];
            EquipImmediately();
        }
    }

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
            if (item is Weapon weapon && weapon.category == categoryToSearch)
            {
                if (found == categoryIndex)
                    return weapon;
                found++;
            }
        }
        return null;
    }

    public void EquipWeapon(Weapon newWeapon)
    {
        if (_switchingWeapon || newWeapon == null || _weapon == newWeapon)
            return;

        _weaponToEquip = newWeapon;

        if (_weapon != null)
        {
            HolsterWeapon();
        }
        else
        {
            _switchingWeapon = true;
            _animator.SetTrigger(EquipTrigger);
        }
    }

    public void EquipImmediately()
    {
        if (_weaponToEquip == null) return;

        _weapon = _weaponToEquip;
        _weaponToEquip = null;

        if (CanvasManager.singleton != null)
        {
            CanvasManager.singleton.UpdateWeapon(_weapon.id);
            CanvasManager.singleton.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);
        }

        if (_weapon.transform.parent != _weaponHolder)
        {
            _weapon.transform.SetParent(_weaponHolder);
            _weapon.transform.localPosition = _weapon.rightHandPosition;
            _weapon.transform.localEulerAngles = _weapon.rightHandRotation;
        }

        if (_weapon.leftHandPosition != null && _weapon.leftHandRotation != null)
        {
            _rigManager?.SetLeftHandGrioData(_weapon.leftHandPosition, _weapon.leftHandRotation);
        }
        else
        {
            Debug.LogWarning($"[무기 장착 오류] {_weapon.id} 의 왼손 위치/회전이 설정되지 않음");
        }

        _weapon.gameObject.SetActive(true);

        // 탄약 찾기
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

    private void HolsterWeaponInternal()
    {
        if (_weapon != null)
        {
            _weapon.gameObject.SetActive(false);
            _weapon = null;
            _ammo = null;
        }
    }

    public void HolsterWeapon()
    {
        if (_switchingWeapon) return;

        if (_weapon != null)
        {
            _switchingWeapon = true;
            _animator.SetTrigger(HolsterTrigger);
        }
    }

    // === 애니메이션 이벤트에서 호출됨 ===
    public void OnEquip() => EquipImmediately();

    public void OnHolster()
    {
        HolsterWeaponInternal();
        if (_weaponToEquip != null)
        {
            EquipImmediately();
        }
    }

    public void ApplyDamage(Character shooter, Transform hit, float damage)
    {
        if (isInvincible) return;

        Debug.Log($"[Character] Taking damage: {damage} from {shooter?.name ?? "unknown"}");
        Health -= (int)damage;

        if (_health <= 0)
        {
            Debug.Log("[Character] Character died.");

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
            _animator.SetTrigger(ReloadTrigger);
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

        if (CanvasManager.singleton != null)
            CanvasManager.singleton.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);
    }

    public void HolsterFinished() => _switchingWeapon = false;
    public void EquipFinished() => _switchingWeapon = false;
}

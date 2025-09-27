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

    // Animator hash 캐싱
    private static readonly int EquipTrigger = Animator.StringToHash("Equip");
    private static readonly int HolsterTrigger = Animator.StringToHash("Holster");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");

    private void Awake()
    {
        _rigManager = GetComponent<RigManager>();
        _animator = GetComponent<Animator>();

        // 테스트용 초기 아이템
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

            if (CanvasManager.singleton != null)
                CanvasManager.singleton.UpdateHealth(_health, MaxHealth);
        }
    }

    // ===== 아이템 초기화 =====
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

    // ===== 무기 슬롯 검색 =====
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

    // ===== 무기 장착 =====
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

    public void HolsterWeapon()
    {
        if (_switchingWeapon) return;

        if (_weapon != null)
        {
            _switchingWeapon = true;
            _animator.SetTrigger(HolsterTrigger);
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

    // ===== 데미지 처리 =====
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

    // ===== 재장전 시작 =====
    public void Reload()
    {
        if (_weapon != null && !_reloading && _weapon.ammo < _weapon.clipSize && _ammo != null && _ammo.amount > 0)
        {
            _animator.SetTrigger(ReloadTrigger);
            _reloading = true;

            float reloadDuration = _weapon.reloadDuration; // 무기별 재장전 시간
            CanvasManager.singleton?.StartReloadUI(reloadDuration);

            StartCoroutine(CoReloadFinish(reloadDuration));
        }
    }

    private IEnumerator CoReloadFinish(float duration)
    {
        yield return new WaitForSeconds(duration);
        ReloadFinished();
    }

    // ===== Animator 진행률 기반 대기 =====
    private IEnumerator CoReloadFinish()
    {
        yield return null; // 한 프레임 대기 (상태 진입 보장)

        while (true)
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Reload") && info.normalizedTime >= 1f)
            {
                break;
            }
            yield return null;
        }

        ReloadFinished();
    }

    // ===== 재장전 종료 =====
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
        {
            CanvasManager.singleton.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);
            CanvasManager.singleton.StopReloadUI();
        }
    }

    public void HolsterFinished() => _switchingWeapon = false;
    public void EquipFinished() => _switchingWeapon = false;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;
    [SerializeField] private Transform _weaponHolder = null;
    [SerializeField] private int _health;
    [SerializeField] public int MaxHealth = 100;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private RigManager rigManager;

    private Weapon _weapon = null; public Weapon weapon => _weapon;
    private Ammo _ammo = null; public Ammo ammo => _ammo;
    private readonly List<Item> _items = new List<Item>();

    private Animator _animator = null;
    private RigManager _rigManager = null;
    private Weapon _weaponToEquip = null;

    public List<Item> weaponItems => _items;
    public Weapon[] weaponSlots = new Weapon[3]; // 1,2,3번 슬롯
    public Transform weaponParent;
    private int currentSlot = 0;
    private bool _reloading = false; public bool reloading => _reloading;
    private bool _switchingWeapon = false; public bool switchingWeapon => _switchingWeapon;
    public bool isInvincible = false;

    // ✅ 트리거 방식 상호작용: 콜라이더 안 '가까운 무기' 참조
    private InteractableWeapon _nearbyWeapon;

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
             { "9mm", 1000 }
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
    public void EquipWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;
        currentSlot = slotIndex;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
                weaponSlots[i].gameObject.SetActive(i == currentSlot);
        }

        // ✅ 실제 현재 무기 참조 업데이트 (사격/조준/UI가 _weapon을 참조하므로 필수)
        _weapon = weaponSlots[currentSlot];

        // ✅ UI 갱신(있을 경우)
        if (CanvasManager.singleton != null && _weapon != null)
        {
            CanvasManager.singleton.UpdateWeapon(_weapon.id);

            // 보유 탄약(_ammo) 갱신: 같은 ammoID 찾아 연결
            _ammo = null;
            foreach (var item in _items)
            {
                if (item is Ammo a && _weapon.ammoID == a.id)
                {
                    _ammo = a;
                    break;
                }
            }
            CanvasManager.singleton.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);
        }

        // ✅ 왼손 IK 갱신(있을 경우)
        if (_weapon != null && _weapon.leftHandPosition != null && _weapon.leftHandRotation != null)
        {
            _rigManager?.SetLeftHandGrioData(_weapon.leftHandPosition, _weapon.leftHandRotation);
        }
    }

    public int GetCurrentSlotIndex()
    {
        return currentSlot;
    }

    public void SwapWeapon(Weapon newWeaponPrefab)
    {
        int slot = GetCurrentSlotIndex();

        // 기존 무기 드랍
        if (weaponSlots[slot] != null)
        {
            weaponSlots[slot].DropToGround();
            Destroy(weaponSlots[slot].gameObject);
        }

        // 새 무기 생성 & 장착
        var newWeapon = Instantiate(newWeaponPrefab, weaponParent);
        weaponSlots[slot] = newWeapon;
        EquipWeapon(slot);
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
    public void EquipWeapon(Weapon weapon)
    {
        // Weapon.cs 쪽 메서드를 직접 쓰는 대신 여기서 래핑
        weapon.transform.SetParent(weaponHolder);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;

        if (rigManager != null)
            rigManager.SetLeftHandGrioData(weapon.leftHandPosition, weapon.leftHandRotation);
    }

    public void ReplaceWeaponInSlot(int slotIndex, Weapon pickedWeapon)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;

        // 기존 무기 드랍
        if (weaponSlots[slotIndex] != null)
        {
            weaponSlots[slotIndex].DropToGround();
            weaponSlots[slotIndex] = null;
        }

        if (pickedWeapon != null)
        {
            // ✅ 반드시 인스턴스화 보장
            Weapon newWeapon = Instantiate(pickedWeapon, weaponParent);

            // 무기 Transform 세팅
            newWeapon.transform.localPosition = newWeapon.rightHandPosition;
            newWeapon.transform.localEulerAngles = newWeapon.rightHandRotation;

            // Rigidbody/Collider 비활성화 (손에 들고 있을 때)
            var rb = newWeapon.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            foreach (var col in newWeapon.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // 슬롯에 등록
            weaponSlots[slotIndex] = newWeapon;
            EquipWeapon(slotIndex);
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
            float reloadDuration = _weapon.reloadDuration;

            // 무기 기본 탄창 숨기기 + 탄창 드롭
            _weapon.HideMagazineMesh();
            _weapon.DropMagazine();

            // UI 시작
            if (CanvasManager.singleton != null)
                CanvasManager.singleton.StartReloadUI(reloadDuration);

            _animator.SetTrigger(ReloadTrigger);
            _reloading = true;
        }
    }

    // ===== 재장전 완료 =====
    public void ReloadFinished()
    {
        if (_weapon != null && _weapon.ammo < _weapon.clipSize && _ammo != null && _ammo.amount > 0)
        {
            int amount = Mathf.Min(_weapon.clipSize - _weapon.ammo, _ammo.amount);
            _ammo.amount -= amount;
            _weapon.ammo += amount;
        }

        // 무기 기본 탄창 다시 켜기
        _weapon?.ShowMagazineMesh();

        _reloading = false;

        if (CanvasManager.singleton != null)
        {
            CanvasManager.singleton.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);
            CanvasManager.singleton.StopReloadUI(); // 재장전 UI 종료
        }
    }

    public void HolsterFinished() => _switchingWeapon = false;
    public void EquipFinished() => _switchingWeapon = false;

    // ====== (Trigger 방식) 가까운 무기 등록/해제 & E키 액션 ======
    public void SetNearbyWeapon(InteractableWeapon weapon) => _nearbyWeapon = weapon;

    public void ClearNearbyWeapon(InteractableWeapon weapon)
    {
        if (_nearbyWeapon == weapon) _nearbyWeapon = null;
    }

    public void TryInteract()
    {
        // 가까운 무기가 있으면 상호작용 수행
        _nearbyWeapon?.Interact(this);
    }
}

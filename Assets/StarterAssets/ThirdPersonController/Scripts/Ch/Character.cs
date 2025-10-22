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

    [SerializeField] private Weapon slot1WeaponPrefab;   // 1번 무기 (HK416)
    [SerializeField] private Weapon slot2WeaponPrefab;   // 2번 무기 (EVO-3)
    [SerializeField] private Weapon slot3WeaponPrefab;   // 3번 무기 (K-9)

    [Header("Augment System")]
    public float onKillHealAmount = 0f;     // 처치 시 회복량
    public float moveSpeed = 5f;            // 이동속도 증강용
    public bool autoReloadOnKill = false;   // 처치 시 자동장전 여부
    public float extraLootRate = 0f;        // 드랍률 증가

    [Header("Shield System")]
    public float maxShield = 0f;
    public float currentShield = 0f;

    // Animator hash 캐싱
    private static readonly int EquipTrigger = Animator.StringToHash("Equip");
    private static readonly int HolsterTrigger = Animator.StringToHash("Holster");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");

    private void Awake()
    {
        _rigManager = GetComponent<RigManager>();
        _animator = GetComponent<Animator>();

        // 테스트용 초기 탄약
        Initialized(new Dictionary<string, int>
    {
        { "9mm", 1000 }
    });

        // 무기 프리팹을 슬롯에 직접 할당
        if (slot1WeaponPrefab != null)
            weaponSlots[0] = Instantiate(slot1WeaponPrefab, weaponHolder).GetComponent<Weapon>();

        if (slot2WeaponPrefab != null)
            weaponSlots[1] = Instantiate(slot2WeaponPrefab, weaponHolder).GetComponent<Weapon>();

        if (slot3WeaponPrefab != null)
            weaponSlots[2] = Instantiate(slot3WeaponPrefab, weaponHolder).GetComponent<Weapon>();

        // 시작 시 1번 무기 장착
        EquipWeapon(0);
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
    // ===== 무기 슬롯 검색 =====
    public Weapon GetWeaponBySlotIndex(int slotIndex)
    {
        // 슬롯 배열 중심으로 무기 검색
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length)
            return null;

        // 슬롯에 무기가 있으면 바로 반환
        if (weaponSlots[slotIndex] != null)
            return weaponSlots[slotIndex];

        // 예전 구조(아이템 리스트 기반) 유지하고 싶을 때만 백업 검색
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

    public void ReplaceWeaponInSlot(int slotIndex, Weapon pickedWeaponPrefab)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;

        // 기존 무기 정리
        if (weaponSlots[slotIndex] != null)
        {
            Weapon oldWeapon = weaponSlots[slotIndex];

            if (oldWeapon != null)
            {
                oldWeapon.gameObject.SetActive(false);
                weaponSlots[slotIndex] = null;

                // ✅ 에디터 오류 방지를 위한 딜레이 삭제
                Destroy(oldWeapon.gameObject, 0.05f);
            }
        }

        if (pickedWeaponPrefab != null)
        {
            // ✅ 새 무기 생성
            Weapon newWeapon = Instantiate(pickedWeaponPrefab, weaponParent);
            newWeapon.transform.localPosition = newWeapon.rightHandPosition;
            newWeapon.transform.localEulerAngles = newWeapon.rightHandRotation;

            // ✅ 물리 비활성화
            var rb = newWeapon.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            foreach (var col in newWeapon.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // ✅ 슬롯 갱신 및 장착
            weaponSlots[slotIndex] = newWeapon;
            EquipWeapon(slotIndex);

            _switchingWeapon = false;
            Debug.Log($"✅ 슬롯 {slotIndex + 1}번 무기 교체 완료: {newWeapon.name}");
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
    public bool UseHealingItem()
    {
        for (int i = 0; i < weaponItems.Count; i++)
        {
            if (weaponItems[i] is HealingItem healItem)
            {
                weaponItems.RemoveAt(i);
                Health += healItem.HealAmount;
                Debug.Log($"🩹 {healItem.HealAmount} 만큼 HP 회복!");
                if (CanvasManager.singleton != null)
                {
                    CanvasManager.singleton.UpdateHealth(Health, MaxHealth);
                    CanvasManager.singleton.UpdateHealingItemCount(GetHealingItemCount());
                }
                return true;
            }
        }

        Debug.Log("❌ 회복 아이템이 없습니다!");
        return false;
    }
    public int GetHealingItemCount()
    {
        int count = 0;
        foreach (var item in weaponItems)
        {
            if (item is HealingItem) count++;
        }
        return count;
    }

    // ====== (Trigger 방식) 가까운 무기 등록/해제 & E키 액션 ======
    public void SetNearbyWeapon(InteractableWeapon weapon) => _nearbyWeapon = weapon;

    public void ClearNearbyWeapon(InteractableWeapon weapon)
    {
        if (_nearbyWeapon == weapon) _nearbyWeapon = null;
    }

    // ====== 가까운 무기 상호작용 (빈 슬롯 또는 교체 포함) ======
    // ====== 가까운 무기 상호작용 (빈 슬롯 또는 교체 포함) ======
    // ====== 가까운 무기 상호작용 (빈 슬롯 또는 교체 포함) ======
    public void TryInteract()
    {
        if (_nearbyWeapon == null) return;

        Weapon pickedWeaponPrefab = _nearbyWeapon.GetComponent<Weapon>();
        if (pickedWeaponPrefab == null) return;

        int emptySlot = FindEmptySlot();

        // ✅ 1️⃣ 빈 슬롯이 있으면 바로 장착
        if (emptySlot != -1)
        {
            ReplaceWeaponInSlot(emptySlot, pickedWeaponPrefab);
            EquipWeapon(emptySlot);
            Debug.Log($"✅ {emptySlot + 1}번 슬롯에 {pickedWeaponPrefab.name} 장착 완료!");
        }
        // ✅ 2️⃣ 슬롯이 전부 찼다면 — 현재 들고 있는 무기를 드랍 후 교체
        else
        {
            int currentSlot = GetCurrentSlotIndex();
            Weapon oldWeapon = weaponSlots[currentSlot];

            if (oldWeapon != null)
            {
                DropWeaponToGround(oldWeapon, currentSlot);
            }

            ReplaceWeaponInSlot(currentSlot, pickedWeaponPrefab);
            EquipWeapon(currentSlot);
            Debug.Log($"♻ {currentSlot + 1}번 무기를 {pickedWeaponPrefab.name}으로 교체했습니다!");
        }

        // ✅ 줍은 무기 제거 (딜레이로 안전하게)
        Destroy(_nearbyWeapon.gameObject, 0.05f);
        _nearbyWeapon = null;
    }



    // ====== 비어있는 슬롯 찾기 ======
    private int FindEmptySlot()
    {
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] == null)
                return i;
        }
        return -1; // 모든 슬롯이 차 있음
    }
    private void DropWeaponToGround(Weapon weapon, int slotIndex)
    {
        if (weapon == null) return;

        // 부모 해제 (손에서 분리)
        weapon.transform.SetParent(null);

        // Rigidbody가 없으면 추가
        var rb = weapon.GetComponent<Rigidbody>();
        if (rb == null) rb = weapon.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.mass = 1f;
        rb.drag = 0.2f;
        rb.angularDrag = 0.05f;

        // 콜라이더 다시 켜기
        foreach (var col in weapon.GetComponentsInChildren<Collider>())
            col.enabled = true;

        // 💥 '몸에서 뱉는' 느낌: 앞으로 + 위로 강한 반동 적용
        Vector3 dropDirection = (transform.forward + Vector3.up * 0.5f).normalized;
        float dropForce = 6f;      // 튀어나가는 세기
        float torqueForce = 8f;    // 회전 세기

        rb.AddForce(dropDirection * dropForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);

        // ❌ 자동 삭제 제거 (이 줄을 없애면 무기는 사라지지 않음)
        // Destroy(weapon.gameObject, 5f);

        // 슬롯 비우기
        weaponSlots[slotIndex] = null;

        Debug.Log($"🟠 {slotIndex + 1}번 무기 {weapon.name}을(를) 몸에서 뱉듯이 버렸습니다. (지속 존재)");
    }

    // ✅ 실드 증가 함수
    public void AddShield(float amount)
    {
        maxShield += amount;
        currentShield = maxShield;
        // HUD 업데이트가 있다면 여기서 호출 가능
    }
    // ✅ 슬로우 오라 기능
    public bool slowAuraActive = false;
    public float slowAuraValue = 0f;

    public void EnableSlowAura(float value)
    {
        slowAuraActive = true;
        slowAuraValue = value;
        // 이 부분에 OverlapSphere 등으로 적 로컬 타임스케일을 줄이는 로직 추가 가능
    }
    public void LevelUp()
    {
        // 레벨업 처리 로직 (경험치 초기화 등)
        AugmentUIManager.Instance.ShowAugmentOptions();
    }
}

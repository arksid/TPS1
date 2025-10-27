using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public static Character Instance { get; private set; }


    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;
    [SerializeField] private Transform _weaponHolder = null;
    [SerializeField] private int _maxHealth = 200;   // ✅ 초기 체력 200
    [SerializeField] private int _health;

    [SerializeField] private Transform weaponHolder;
    [SerializeField] private RigManager rigManager;

    [SerializeField] private RagdollController ragdollController;
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

    // 🛡 실드 관련 필드
    [SerializeField] private int _maxShield = 100;
    [SerializeField] private int _shield = 0;
    [SerializeField] private float shieldRegenDelay = 5f; // 데미지 안 받은 시간
    [SerializeField] private float shieldRegenRate = 0.5f; // 초당 회복 속도
    private float lastDamageTime;

    // ===== [조건부 특성용 상태 & 값] =====
    [Header("Augment (Conditional) Flags & Values")]
    public bool enablePredator; public float predatorValue;      // HP 50% 이하일 때 공격력 +v
    public bool enableVengeance; public float vengeanceValue = 0.2f;   // 피격 후 5초 공격력 +v
    public bool enableTriggerRush; public float triggerRushValue = 0.3f; // 처치 후 3초 이속 +v
    public bool enableAdrenalSurge; public float adrenalSurgeValue = 0.1f; // 연속 명중 공속 스택
    public bool enableBulletFever; public float bulletFeverValue = 5f;    // 연속 사격 크확 +v%p
    public bool enableSecondWind; public int secondWindShield = 50;     // HP 20% 이하 즉시 실드 회복(쿨)
    public bool enableColdRage; public float coldRageMaxBonus = 30f;   // HP ↓일수록 크확 +X%p(최대)

    private bool _predatorActive;
    private float _coldRageApplied;       // 현재까지 크확 보정치(가감형)
    private bool _secondWindOnCooldown;

    private int _hitStreak;               // 연속 명중 카운트(AdrenalSurge, BulletFever 공용)
    private Coroutine _vengeanceCo, _rushCo, _adrenalDecayCo, _bulletDecayCo;

    // ===== 시간형 증강 '적용된 값/스택' 추적 =====
    private bool _vengeanceActive = false;
    private float _vengeanceApplied = 0f;

    private float _rushApplied = 0f;

    private int _adrenalStacks = 0;
    private float _adrenalApplied = 0f;

    private int _bulletStacks = 0;
    private float _bulletApplied = 0f;


    public bool enableRetaliation;   // (추후 확장: 피탄 시 반격 등)
    public bool enableChainReaction; // (추후 확장: 처치 시 폭발 연쇄 등)
    public bool enableUltCharger;    // (추후 확장: 명중 시 궁극 충전 보조 등)

    // --- 이번에 쓰는 토글 플래그들(새로 추가) ---
    public bool enableQuickReload = false;  // 처치 후 리로드가 빨라지는 버프 사용 여부
    public bool enableGearUp = false;       // 리로드 직후 데미지 버프 사용 여부
    public bool enableOverheat = false;     // 연속 명중 시 데미지 버프 사용 여부
    public bool enableRend = false;         // 동일 적 히트 추적 버프 사용 여부
                                            // ===== 시간형 증강 '적용된 값/스택' 추적 (이번 6종에서 추가 사용) =====
    private Coroutine _gearUpCo;
    private float _gearUpApplied = 0f;

    private Coroutine _overheatCo;
    private int _overheatHitStreak = 0;
    private float _overheatApplied = 0f;

    // Rend: 적별 히트 스택/버프
    private class RendInfo { public int hits; public float lastHitTime; public float expireTime; }
    private Dictionary<int, RendInfo> _rendMap = new Dictionary<int, RendInfo>(); // enemyID -> info

    // Animator hash 캐싱
    private static readonly int EquipTrigger = Animator.StringToHash("Equip");
    private static readonly int HolsterTrigger = Animator.StringToHash("Holster");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");

    [SerializeField] private float criticalChance = 0f;  // 퍼센트 (0~100)
    [SerializeField] private float criticalMultiplier = 2f;
    // Character.cs (필드 영역 어딘가)
    [SerializeField] private Outline outline;  // QuickOutline 컴포넌트
    [SerializeField] private bool outlineOffOnStart = true; // 시작 시 아웃라인 끄기 옵션
    public float CurrentSpeed { get; private set; }
    public int MaxHealth
    {
        get => _maxHealth;
        set
        {
            Debug.Log($"[Character] MaxHealth 변경: {_maxHealth} → {value}");
            _maxHealth = value;
            if (Health > _maxHealth)
                Health = _maxHealth;

            if (CanvasManager.singleton != null)
                CanvasManager.singleton.UpdateHealth(Health, _maxHealth);
        }
    }
    public int Health
    {
        get => _health;
        set
        {
            _health = Mathf.Clamp(value, 0, MaxHealth);
            if (CanvasManager.singleton != null)
                CanvasManager.singleton.UpdateHealth(_health, MaxHealth);
        }
    }
    public int MaxShield
    {
        get => _maxShield;
        set
        {
            _maxShield = value;
            if (_shield > _maxShield)
                _shield = _maxShield;
            CanvasManager.singleton?.UpdateShield(_shield, _maxShield);
        }
    }
    public int Shield
    {
        get => _shield;
        set
        {
            _shield = Mathf.Clamp(value, 0, _maxShield);
            CanvasManager.singleton?.UpdateShield(_shield, _maxShield);
        }
    }
    public float CriticalChance
    {
        get => criticalChance;
        set => criticalChance = Mathf.Clamp(value, 0f, 100f);
    }
    public float CriticalMultiplier
    {
        get => criticalMultiplier;
        set => criticalMultiplier = value;
    }
    public bool RollCritical()
    {
        return Random.Range(0f, 100f) <= criticalChance;
    }
    private void Awake()
    {
        _rigManager = GetComponent<RigManager>();
        _animator = GetComponent<Animator>();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (outline == null) outline = GetComponentInChildren<Outline>(true);

        // 시작 상태 안전: 무적 해제
        isInvincible = false;
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
    private void Start()
    {
        Health = MaxHealth;
        if (CanvasManager.singleton != null)
            CanvasManager.singleton.UpdateHealth(Health, MaxHealth);
        // 시작 프레임에 아웃라인 강제 OFF
        if (outlineOffOnStart && outline != null && outline.enabled)
            outline.enabled = false;
    }
    public void Heal(float amount)
    {
        int healAmount = Mathf.RoundToInt(amount);
        Health = Mathf.Min(Health + healAmount, MaxHealth);
        Debug.Log($"[Character] 체력 {healAmount} 회복 → 현재 체력 {Health}");
    }
    public void RestoreShield(int amount)
    {
        Shield = Mathf.Min(Shield + amount, MaxShield);
        Debug.Log($"[Character] 실드 {amount} 회복 → 현재 실드 {Shield}");
    }
    public void SetInvincible(bool on)
    {
        isInvincible = on;
        if (outline != null) outline.enabled = on;  // ✅ QuickOutline ON/OFF
    }
    public void RefreshStats()
    {
        Health = Mathf.Clamp(Health, 0, MaxHealth);
        if (CanvasManager.singleton != null)
            CanvasManager.singleton.UpdateHealth(Health, MaxHealth);
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
    // Character.cs
    public void ApplyDamage(GameObject attacker, Transform hitTransform, float amount)
    {
        // ✅ 무적이면 데미지 무시
        if (isInvincible)
        {
            Debug.Log("[Character] 무적 상태: 데미지 무시");
            return;
        }

        lastDamageTime = Time.time;

        int intAmount = Mathf.RoundToInt(amount);

        if (Shield > 0)
        {
            int remain = intAmount - Shield;
            Shield -= intAmount;
            if (Shield < 0) Shield = 0;
            if (remain > 0) Health -= remain;
        }
        else
        {
            Health -= intAmount;
        }

        CanvasManager.singleton?.UpdateHealth(Health, MaxHealth);
        if (Health <= 0) Die();
        OnPlayerDamaged(intAmount);
    }
    private bool isDead = false;

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[Character] 플레이어 사망 처리 시작");

        // 🧊 조작 스크립트 비활성화
        if (scriptsToDisableOnDeath != null)
        {
            foreach (var script in scriptsToDisableOnDeath)
            {
                if (script != null) script.enabled = false;
            }
        }

        // 🎯 무기 숨기기
        if (_weapon != null)
        {
            _weapon.gameObject.SetActive(false);
        }

        // 💀 레그돌 활성화
        if (ragdollController != null)
        {
            ragdollController.ActivateRagdoll();
        }

        // 🩸 애니메이션 (Ragdoll 켜면 Animator가 꺼지므로 주의)
        if (_animator != null)
        {
            _animator.SetTrigger("Die");
        }

      
    }


    private void Update()
    {
        // 🛡 일정 시간 피해를 받지 않으면 회복 시작
        if (Time.time - lastDamageTime >= shieldRegenDelay && _shield < _maxShield)
        {
            Shield += 1;
        }
        SM?.TickQuickReload(Time.deltaTime);

        HandleRendExpire(); // Rend 버프 만료 정리
        // 기존 실드 재생 로직 아래에 추가
        HandlePredator();
        HandleColdRage();
        HandleSecondWind();

    }
    void HandleRendExpire()
    {
        if (_rendMap.Count == 0) return;
        float now = Time.time;
        // 만료만 정리 (실제 데미지 가중은 Projectile에서 질의)
        List<int> remove = null;
        foreach (var kv in _rendMap)
        {
            if (kv.Value.expireTime > 0 && now >= kv.Value.expireTime)
            {
                if (remove == null) remove = new List<int>();
                remove.Add(kv.Key);
            }
        }
        if (remove != null) foreach (var id in remove) _rendMap.Remove(id);
    }
    // ===== 재장전 시작 =====
    public void Reload()
    {
        if (_weapon != null && !_reloading && _weapon.ammo < _weapon.GetEffectiveMagazineSize() && _ammo != null && _ammo.amount > 0)
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
        // 1) 방어적 체크
        if (_weapon == null)
        {
            _reloading = false;
            return;
        }
        if (_ammo == null)
        {
            _weapon.ShowMagazineMesh();
            _reloading = false;
            return;
        }

        // 2) 유효 장탄수(증강 반영) 기준으로 채우기 ✅
        int magMax = _weapon.GetEffectiveMagazineSize();
        if (_weapon.ammo < magMax && _ammo.amount > 0)
        {
            int need = Mathf.Max(0, magMax - _weapon.ammo);
            int toLoad = Mathf.Min(need, _ammo.amount);
            _weapon.ammo += toLoad;
            _ammo.amount -= toLoad;
        }

        // 3) 혹시 모를 넘침 방지(증강 해제 등) ✅
        _weapon.ClampAmmoToMagazine();

        // 4) 원래 하던 마무리(메시 노출/플래그/UI) 그대로 ✅
        _weapon.ShowMagazineMesh();
        _reloading = false;

        if (CanvasManager.singleton != null)
            CanvasManager.singleton.UpdateAmmo(_weapon.ammo, _ammo?.amount ?? 0);
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
    // ===== [조건부 특성 실행 로직] =====
    private StatModifierManager SM => StatModifierManager.Instance;

    // ===== [교체] 피격 트리거: Vengeance =====
    public void OnPlayerDamaged(float damageTaken)
    {
        if (!enableVengeance || SM == null) return;

        // 이미 켜져 있으면 '값은 그대로', 시간만 연장
        if (_vengeanceActive)
        {
            if (_vengeanceCo != null) StopCoroutine(_vengeanceCo);
            _vengeanceCo = StartCoroutine(VengeanceTimer(5f)); // 예: 5초 지속
            return;
        }

        // 꺼져 있던 상태 → 1회만 적용
        SM.AddDamageMultiplier(vengeanceValue);
        _vengeanceApplied = vengeanceValue;
        _vengeanceActive = true;

        if (_vengeanceCo != null) StopCoroutine(_vengeanceCo);
        _vengeanceCo = StartCoroutine(VengeanceTimer(5f));
    }

    // 새 코루틴
    private IEnumerator VengeanceTimer(float duration)
    {
        yield return new WaitForSeconds(duration);

        // 끝날 때 '적용돼 있던 만큼'만 정확히 회수
        if (_vengeanceApplied != 0f) SM?.AddDamageMultiplier(-_vengeanceApplied);
        _vengeanceApplied = 0f;
        _vengeanceActive = false;
        _vengeanceCo = null;
    }


   

private IEnumerator AdrenalDecayTimer(float seconds)
{
    yield return new WaitForSeconds(seconds);

    if (_adrenalApplied != 0f) SM?.AddFireRateMultiplier(-_adrenalApplied);
    _adrenalApplied = 0f;
    _adrenalStacks = 0;
    _adrenalDecayCo = null;
}

private IEnumerator BulletDecayTimer(float seconds)
{
    yield return new WaitForSeconds(seconds);

    if (_bulletApplied != 0f) SM?.AddCriticalChance(-_bulletApplied);
    _bulletApplied = 0f;
    _bulletStacks = 0;
    _bulletDecayCo = null;
}


    // ===== [교체] 처치 트리거: TriggerRush =====
    public void OnEnemyKilledHook()
    {
        if (!enableTriggerRush || SM == null) return;

        // 이미 켜져 있으면 '시간만 연장'
        if (_rushApplied != 0f)
        {
            if (_rushCo != null) StopCoroutine(_rushCo);
            _rushCo = StartCoroutine(RushTimer(3f)); // 예: 3초 지속
            return;
        }

        // 꺼져 있던 상태 → 1회만 적용
        SM.AddMoveSpeedMultiplier(triggerRushValue);
        _rushApplied = triggerRushValue;

        if (_rushCo != null) StopCoroutine(_rushCo);
        _rushCo = StartCoroutine(RushTimer(3f));
    }

    private IEnumerator RushTimer(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (_rushApplied != 0f) SM?.AddMoveSpeedMultiplier(-_rushApplied);
        _rushApplied = 0f;
        _rushCo = null;
    }


    void HandlePredator()
    {
        if (!enablePredator || SM == null) return;
        float hpRatio = (MaxHealth > 0) ? (float)Health / MaxHealth : 0f;
        if (hpRatio <= 0.5f && !_predatorActive)
        {
            SM.AddDamageMultiplier(predatorValue);
            _predatorActive = true;
        }
        else if (hpRatio > 0.5f && _predatorActive)
        {
            SM.AddDamageMultiplier(-predatorValue);
            _predatorActive = false;
        }
    }

    void HandleColdRage()
    {
        if (!enableColdRage || SM == null) return;
        float hpRatio = (MaxHealth > 0) ? (float)Health / MaxHealth : 0f;
        float targetBonus = (1f - hpRatio) * coldRageMaxBonus; // 0~max %p
        float delta = targetBonus - _coldRageApplied;
        if (Mathf.Abs(delta) > 0.01f)
        {
            SM.AddCriticalChance(delta);
            _coldRageApplied = targetBonus;
        }
    }

    void HandleSecondWind()
    {
        if (!enableSecondWind || _secondWindOnCooldown) return;
        float hpRatio = (MaxHealth > 0) ? (float)Health / MaxHealth : 0f;
        if (hpRatio <= 0.2f)
            StartCoroutine(SecondWindRoutine());
    }

    IEnumerator SecondWindRoutine()
    {
        _secondWindOnCooldown = true;
        RestoreShield(secondWindShield);
        yield return new WaitForSeconds(10f); // 쿨 10초 (원하면 조절)
        _secondWindOnCooldown = false;
    }

    // 호환용(Projectile에서 파라미터 없이 부를 때 대비)
    // 필요하면 여기서 Adrenal/BulletFever 같은 '대상 불필요' 트리거를 처리하도록 확장 가능.
    public void OnPlayerHitEnemyHook()
    {
        // 현재는 특별 처리 없이 반환(컴파일러/호출자 호환 목적)
    }


    public void OnPlayerHitEnemyHook(EnemyController enemy)
    {
        if (enemy == null) return;
        float now = Time.time;

        // Overheat
        if (enableOverheat && SM != null)
        {
            _overheatHitStreak++;
            if (_overheatHitStreak >= SM.OverheatHitNeed)
            {
                if (_overheatApplied == 0f)
                {
                    SM.AddDamageMultiplier(SM.OverheatBuffValue);
                    _overheatApplied = SM.OverheatBuffValue;
                }
                if (_overheatCo != null) StopCoroutine(_overheatCo);
                _overheatCo = StartCoroutine(_OverheatTimer(SM.OverheatBuffDuration));
                _overheatHitStreak = 0;
            }
        }

        // Rend
        if (enableRend && SM != null)
        {
            int id = enemy.GetInstanceID();
            RendInfo info;
            if (!_rendMap.TryGetValue(id, out info))
            {
                info = new RendInfo() { hits = 0, lastHitTime = 0f, expireTime = 0f };
                _rendMap[id] = info;
            }
            if (now - info.lastHitTime > SM.RendWindow) info.hits = 0;
            info.hits++;
            info.lastHitTime = now;

            if (info.hits >= SM.RendHitNeed)
            {
                info.expireTime = now + SM.RendDuration; // 5초 버프
                info.hits = 0;
            }
        }
    }



    public void OnReloadFinished_GearUp()
    {
        if (!enableGearUp || SM == null) return;

        // 한 번만 적용, 시간 연장
        if (_gearUpApplied == 0f)
        {
            SM.AddDamageMultiplier(SM.GearUpBuffValue);
            _gearUpApplied = SM.GearUpBuffValue;
        }
        if (_gearUpCo != null) StopCoroutine(_gearUpCo);
        _gearUpCo = StartCoroutine(_GearUpTimer(SM.GearUpBuffDuration));
    }

    IEnumerator _GearUpTimer(float sec)
    {
        yield return new WaitForSeconds(sec);
        if (_gearUpApplied != 0f) SM?.AddDamageMultiplier(-_gearUpApplied);
        _gearUpApplied = 0f;
        _gearUpCo = null;
    }
    

    IEnumerator _OverheatTimer(float sec)
    {
        yield return new WaitForSeconds(sec);
        if (_overheatApplied != 0f) SM?.AddDamageMultiplier(-_overheatApplied);
        _overheatApplied = 0f;
        _overheatCo = null;
    }
    public float GetRendBonusForEnemy(EnemyController enemy)
    {
        if (!enableRend || enemy == null || _rendMap.Count == 0) return 0f;
        int id = enemy.GetInstanceID();
        RendInfo info;
        if (_rendMap.TryGetValue(id, out info))
        {
            if (info.expireTime > Time.time) return SM != null ? SM.RendBonus : 0f;
        }
        return 0f;
    }

}

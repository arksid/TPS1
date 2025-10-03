using UnityEngine;

public class InteractableWeapon : MonoBehaviour, IInteractable
{
    [SerializeField] private Weapon weaponPrefab; // 장착할 무기 프리팹 (Inspector에 설정)

    private void Awake()
    {
        if (weaponPrefab == null)
        {
            weaponPrefab = GetComponent<Weapon>();
        }
    }

    // ✅ 플레이어가 무기 콜라이더(Trigger) 안에 들어오면, Character가 '가까운 무기'로 등록
    private void OnTriggerEnter(Collider other)
    {
        var character = other.GetComponent<Character>();
        if (character != null)
        {
            character.SetNearbyWeapon(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var character = other.GetComponent<Character>();
        if (character != null)
        {
            character.ClearNearbyWeapon(this);
        }
    }

    // ✅ 실제 상호작용(교체) 로직
    public void Interact(Character character)
    {
        if (character == null || weaponPrefab == null) return;

        int slot = character.GetCurrentSlotIndex();

        // 현재 무기 교체 시도
        character.ReplaceWeaponInSlot(slot, weaponPrefab);

        // 바닥 무기는 제거 (씬에서 없어짐)
        Destroy(gameObject);
    }
}

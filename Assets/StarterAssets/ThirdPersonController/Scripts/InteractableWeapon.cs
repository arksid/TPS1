using UnityEngine;

public class InteractableWeapon : MonoBehaviour, IInteractable
{
    private Weapon weapon; // 이 오브젝트에 붙은 Weapon 컴포넌트

    private void Awake()
    {
        weapon = GetComponent<Weapon>();
        if (weapon == null)
        {
            Debug.LogError("[InteractableWeapon] 같은 오브젝트에 Weapon이 필요합니다.");
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
        if (character == null || weapon == null) return;

        // 현재 들고 있는 슬롯에 교체
        int slot = character.GetCurrentSlotIndex();        // Character에 이미 있음
        character.ReplaceWeaponInSlot(slot, weapon);       // Character에 이미 있음
        // ReplaceWeaponInSlot 안에서 부모/포즈/콜라이더 정리를 하기 때문에
        // 여기서 Destroy(gameObject) 할 필요 없음.
    }
}

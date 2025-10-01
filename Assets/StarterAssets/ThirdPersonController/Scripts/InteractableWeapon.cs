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

        int slot = character.GetCurrentSlotIndex();
        character.ReplaceWeaponInSlot(slot, weapon);

        // ✅ 여기서는 Destroy 필요 없음 (씬 무기를 플레이어 손으로 옮김)
    }
}

using UnityEngine;

public class AutoPickupHealingItem : MonoBehaviour
{
    [Header("회복 아이템 정보")]
    [SerializeField] private HealingItem healingItemPrefab;
    [SerializeField] private string itemName = "Healing Item";

    private void Awake()
    {
        if (healingItemPrefab == null)
            healingItemPrefab = GetComponent<HealingItem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        var character = other.GetComponent<Character>();
        if (character != null && healingItemPrefab != null)
        {
            // 아이템을 캐릭터 인벤토리에 추가
            var newItem = Instantiate(healingItemPrefab, character.transform);
            newItem.gameObject.SetActive(false);
            character.weaponItems.Add(newItem);

            Debug.Log($"✅ {itemName}을(를) 획득했습니다!");
            Destroy(gameObject);
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

public class EnemyDropSystem : MonoBehaviour
{
    [Header("🎲 드랍 확률 가중치 (총합 100 기준 권장)")]
    public float weaponDropWeight = 50f;
    public float healDropWeight = 30f;
    public float ammoDropWeight = 20f;
    public float noDropWeight = 0f;

    [Header("🧰 힐팩 / 탄약 프리팹")]
    public GameObject healPackPrefab;
    public GameObject ammoPackPrefab;

    /// <summary>
    /// 가중치에 따라 아이템을 랜덤 드랍합니다.
    /// </summary>
    public void TryDropItemByWeight()
    {
        float totalWeight = weaponDropWeight + healDropWeight + ammoDropWeight + noDropWeight;
        if (totalWeight <= 0f)
        {
            Debug.LogWarning("[DropSystem] 총 가중치가 0입니다. 드랍 없음.");
            return;
        }

        float roll = Random.Range(0f, totalWeight);

        if (roll < weaponDropWeight)
        {
            DropWeapon();
        }
        else if (roll < weaponDropWeight + healDropWeight)
        {
            DropHeal();
        }
        else if (roll < weaponDropWeight + healDropWeight + ammoDropWeight)
        {
            DropAmmo();
        }
        else
        {
            Debug.Log("[DropSystem] 드랍 없음 (No Drop Weight)");
        }
    }

    private void DropWeapon()
    {
        var manager = PrefabManager.singleton;
        if (manager == null || manager._items == null || manager._items.Length == 0)
        {
            Debug.LogWarning("[DropSystem] PrefabManager에 무기가 없습니다.");
            return;
        }

        string currentPrefabName = gameObject.name;
        var filteredList = new List<Item>();

        foreach (var item in manager._items)
        {
            if (item == null) continue;
            if (item.gameObject.name == currentPrefabName) continue; // 자기 자신 제외
            filteredList.Add(item);
        }

        if (filteredList.Count == 0)
        {
            Debug.LogWarning("[DropSystem] 자기 자신 제외 후 드랍할 무기가 없습니다.");
            return;
        }

        int randIndex = Random.Range(0, filteredList.Count);
        Item selected = filteredList[randIndex];
        Instantiate(selected.gameObject, transform.position, Quaternion.identity);

        Debug.Log($"[DropSystem] 무기 드랍: {selected.name}");
    }

    private void DropHeal()
    {
        if (healPackPrefab != null)
        {
            Instantiate(healPackPrefab, transform.position, Quaternion.identity);
            Debug.Log("[DropSystem] 힐팩 드랍");
        }
    }

    private void DropAmmo()
    {
        if (ammoPackPrefab != null)
        {
            Instantiate(ammoPackPrefab, transform.position, Quaternion.identity);
            Debug.Log("[DropSystem] 탄약 드랍");
        }
    }
}

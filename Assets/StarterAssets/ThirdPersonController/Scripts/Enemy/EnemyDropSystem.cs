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
    /// 확률 기반으로 드랍을 결정하고, 무기 또는 힐팩/탄약을 떨굽니다.
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
            Debug.Log("[DropSystem] 드랍 없음 (No Drop Weight 발동)");
        }
    }

    /// <summary>
    /// PrefabManager에 등록된 무기 중 자기 프리팹만 제외하고 랜덤 드랍
    /// </summary>
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
            if (item.gameObject.name == currentPrefabName) continue;
            filteredList.Add(item);
        }

        if (filteredList.Count == 0)
        {
            Debug.LogWarning("[DropSystem] 자기 자신을 제외한 무기가 없습니다.");
            return;
        }

        int randIndex = Random.Range(0, filteredList.Count);
        Item selected = filteredList[randIndex];
        Instantiate(selected.gameObject, transform.position, Quaternion.identity);

        Debug.Log($"[DropSystem] 무기 드랍: {selected.name}");
    }

    /// <summary>
    /// 힐팩 드랍
    /// </summary>
    private void DropHeal()
    {
        if (healPackPrefab != null)
        {
            Instantiate(healPackPrefab, transform.position, Quaternion.identity);
            Debug.Log("[DropSystem] 힐팩 드랍");
        }
    }

    /// <summary>
    /// 탄약팩 드랍
    /// </summary>
    private void DropAmmo()
    {
        if (ammoPackPrefab != null)
        {
            Instantiate(ammoPackPrefab, transform.position, Quaternion.identity);
            Debug.Log("[DropSystem] 탄약 드랍");
        }
    }
}

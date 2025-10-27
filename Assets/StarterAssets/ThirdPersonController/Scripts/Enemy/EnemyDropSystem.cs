// EnemyDropSystem.cs
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

        // ✅ 인스턴스 생성
        GameObject dropObj = Instantiate(selected.gameObject, transform.position, Quaternion.identity);

        // ✅ 드랍된 무기에서 Outline 전부 비활성화(또는 제거)
        //    - 비활성화: 향후 필요 시 다시 켤 수 있음
        //    - 제거: 아예 컴포넌트를 없앰 (원하면 아래 DestroyImmediate 라인 사용)
        var outlines = dropObj.GetComponentsInChildren<Outline>(true);
        for (int i = 0; i < outlines.Length; i++)
        {
            if (outlines[i] == null) continue;
            // 방법 1) 비활성화
            outlines[i].enabled = false;

            // 방법 2) 완전 제거 (필요하면 주석 해제)
            // Destroy(outlines[i]); // 런타임에서 안전 제거
        }

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

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

    [Header("⏱ 자동 삭제 설정")]
    public float itemLifeTimeSeconds = 30f;
    public float warnBeforeSeconds = 3f;
    public float warnBlinkIntervalSec = 0.2f;

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
            // no drop
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

        // ✅ QuickOutline 충돌 방지: 드랍된 무기에서 Outline 전부 비활성화
        DisableAllOutlines(dropObj);

        // ✅ 자동 삭제 컴포넌트 부착/설정
        AttachAutoDespawn(dropObj);

        Debug.Log($"[DropSystem] 무기 드랍: {selected.name}");
    }

    private void DropHeal()
    {
        if (healPackPrefab == null) return;

        var obj = Instantiate(healPackPrefab, transform.position, Quaternion.identity);
        AttachAutoDespawn(obj);
        // 힐팩은 Outline이 거의 없지만 혹시 붙어있다면 방지 차원
        DisableAllOutlines(obj);
    }

    private void DropAmmo()
    {
        if (ammoPackPrefab == null) return;

        var obj = Instantiate(ammoPackPrefab, transform.position, Quaternion.identity);
        AttachAutoDespawn(obj);
        DisableAllOutlines(obj);
    }

    // ---------------- Helper ----------------

    private void DisableAllOutlines(GameObject root)
    {
        if (root == null) return;

        var outlines = root.GetComponentsInChildren<Outline>(true);
        for (int i = 0; i < outlines.Length; i++)
        {
            if (outlines[i] == null) continue;
            outlines[i].enabled = false;
            // 필요 시 완전 제거:
            // Destroy(outlines[i]);
        }
    }

    private void AttachAutoDespawn(GameObject go)
    {
        if (go == null) return;

        var despawn = go.GetComponent<AutoDespawn>();
        if (despawn == null) despawn = go.AddComponent<AutoDespawn>();

        despawn.lifetime = Mathf.Max(0.1f, itemLifeTimeSeconds);
        despawn.warnBefore = Mathf.Max(0f, warnBeforeSeconds);
        despawn.blinkInterval = Mathf.Max(0f, warnBlinkIntervalSec);
    }
}

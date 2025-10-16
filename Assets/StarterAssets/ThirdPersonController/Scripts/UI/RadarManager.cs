using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RadarManager : MonoBehaviour
{
    public static RadarManager Instance;

    [Header("🎯 Radar Settings")]
    public Transform player;
    public RectTransform radarUI;
    public GameObject blipEnemyPrefab;       // 적 아이콘
    public GameObject blipPlayerPrefab;      // 플레이어 아이콘
    public GameObject blipHealingPrefab;     // 힐템 아이콘
    public GameObject blipAmmoPrefab;        // 탄약 아이콘
    public float radarRange = 50f;

    private readonly Dictionary<Transform, GameObject> blips = new Dictionary<Transform, GameObject>();
    private GameObject playerBlip;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 🧭 플레이어 아이콘 생성
        if (blipPlayerPrefab != null && radarUI != null)
        {
            playerBlip = Instantiate(blipPlayerPrefab, radarUI);
            playerBlip.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }

    private void Update()
    {
        if (player == null || radarUI == null) return;

        List<Transform> toRemove = new List<Transform>();

        foreach (var kvp in blips)
        {
            Transform target = kvp.Key;
            GameObject blip = kvp.Value;

            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Destroy(blip);
                toRemove.Add(target);
                continue;
            }

            Vector3 offset = target.position - player.position;
            float distance = offset.magnitude;

            if (distance <= radarRange)
            {
                float angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg - player.eulerAngles.y;
                float scaledDistance = distance / radarRange * (radarUI.rect.width / 2f);

                Vector2 pos = new Vector2(
                    scaledDistance * Mathf.Sin(angle * Mathf.Deg2Rad),
                    scaledDistance * Mathf.Cos(angle * Mathf.Deg2Rad)
                );

                blip.SetActive(true);
                blip.GetComponent<RectTransform>().anchoredPosition = pos;
            }
            else
            {
                blip.SetActive(false);
            }
        }

        foreach (var t in toRemove)
            blips.Remove(t);
    }

    // ✅ 적 등록
    public void RegisterEnemy(Transform enemy)
    {
        RegisterTarget(enemy, blipEnemyPrefab);
    }

    // ✅ 힐템 등록
    public void RegisterHealingItem(Transform item)
    {
        RegisterTarget(item, blipHealingPrefab);
    }

    // ✅ 탄약 등록
    public void RegisterAmmoBox(Transform item)
    {
        RegisterTarget(item, blipAmmoPrefab);
    }

    // ✅ 공통 등록 로직
    private void RegisterTarget(Transform target, GameObject prefab)
    {
        if (target == null || prefab == null || blips.ContainsKey(target)) return;

        GameObject blip = Instantiate(prefab, radarUI);
        blips[target] = blip;
    }

    // ✅ 제거
    public void UnregisterTarget(Transform target)
    {
        if (target == null) return;
        if (blips.ContainsKey(target))
        {
            Destroy(blips[target]);
            blips.Remove(target);
        }
    }
    public void UnregisterEnemy(Transform enemy)
    {
        UnregisterTarget(enemy);
    }
}

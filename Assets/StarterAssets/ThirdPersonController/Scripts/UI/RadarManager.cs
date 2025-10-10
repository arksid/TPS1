using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RadarManager : MonoBehaviour
{
    public static RadarManager Instance;

    [Header("Radar Settings")]
    public Transform player;              // 플레이어 Transform
    public RectTransform radarUI;         // 레이더 원형 UI
    public GameObject blipPrefab;         // 작은 점 Prefab
    public float radarRange = 50f;        // 탐지 범위

    [Header("Player FOV")]
    public RectTransform fovIndicator;    // 시야각 V자 Image
    public float fovAngle = 60f;          // 시야각 (도)

    private Dictionary<Transform, GameObject> blips = new Dictionary<Transform, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
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

            if (offset.magnitude <= radarRange)
            {
                float angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg - player.eulerAngles.y;
                float distance = offset.magnitude / radarRange * (radarUI.rect.width / 2f);

                Vector2 pos = new Vector2(
                    distance * Mathf.Sin(angle * Mathf.Deg2Rad),
                    distance * Mathf.Cos(angle * Mathf.Deg2Rad)
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
        {
            blips.Remove(t);
        }

        UpdateFOV();
    }

    private void UpdateFOV()
    {
        if (fovIndicator == null) return;

        float playerYaw = player.eulerAngles.y;
        fovIndicator.localRotation = Quaternion.Euler(0, 0, -playerYaw);
        fovIndicator.sizeDelta = new Vector2(fovAngle, radarUI.rect.width);
    }

    // -------------------------------
    // 등록/제거 함수
    // -------------------------------
    public void RegisterTarget(Transform target, Color color)
    {
        if (target == null) return;
        if (blips.ContainsKey(target)) return; // 중복 방지
        if (!target.gameObject.activeInHierarchy) return;

        GameObject blip = Instantiate(blipPrefab, radarUI);
        Image img = blip.GetComponent<Image>();
        if (img != null) img.color = color;

        blips[target] = blip;
    }

    public void UnregisterTarget(Transform target)
    {
        if (target == null) return;

        if (blips.ContainsKey(target))
        {
            Destroy(blips[target]);
            blips.Remove(target);
        }
    }

    // Enemy 호환용 함수
    public void RegisterEnemy(Transform enemy)
    {
        RegisterTarget(enemy, Color.red);
    }

    public void UnregisterEnemy(Transform enemy)
    {
        UnregisterTarget(enemy);
    }
}

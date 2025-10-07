using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Radar : MonoBehaviour
{
    [Header("Radar Settings")]
    public Transform player;
    public float radarRange = 50f;
    public RectTransform radarUI;
    public GameObject blipPrefab;

    [Header("Dynamic Targets")]
    public List<Transform> enemies = new List<Transform>();
    public List<Transform> allies = new List<Transform>();

    [Header("Player FOV")]
    public RectTransform fovIndicator;
    public float fovAngle = 60f;

    private Dictionary<Transform, GameObject> blipMap = new Dictionary<Transform, GameObject>();

    void Start()
    {
        
    }

    void Update()
    {
        // 각 Blip 위치 갱신
        foreach (var kvp in blipMap)
        {
            Transform target = kvp.Key;
            GameObject blip = kvp.Value;

            if (target == null)
            {
                blip.SetActive(false);
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
                blip.SetActive(false); // 범위 밖이면 숨기기
            }
        }

        UpdateFOV();
    }

    private void CreateBlipForTarget(Transform target, Color color)
    {
        if (target == null || blipMap.ContainsKey(target)) return;

        GameObject blip = Instantiate(blipPrefab, radarUI);
        blip.GetComponent<Image>().color = color;
        blipMap[target] = blip;
    }

    private void UpdateFOV()
    {
        if (fovIndicator == null) return;

        float playerYaw = player.eulerAngles.y;
        fovIndicator.localRotation = Quaternion.Euler(0, 0, -playerYaw);
        fovIndicator.sizeDelta = new Vector2(fovAngle, radarUI.rect.width);
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Radar : MonoBehaviour
{
    [Header("Radar Settings")]
    public Transform player;             // 플레이어
    public float radarRange = 50f;       // 탐지 범위
    public RectTransform radarUI;        // 레이더 원형 UI (Image)
    public GameObject blipPrefab;        // 작은 점 Prefab

    [Header("Targets")]
    public Transform[] enemies;          // 적 대상들
    public Transform[] allies;           // 아군 대상들

    private List<GameObject> blips = new List<GameObject>();

    void Update()
    {
        // 기존 점들 제거
        foreach (var b in blips) Destroy(b);
        blips.Clear();

        // 적 표시
        foreach (var e in enemies)
        {
            CreateBlip(e, Color.red);
        }

        // 아군 표시
        foreach (var a in allies)
        {
            CreateBlip(a, Color.blue);
        }
    }

    private void CreateBlip(Transform target, Color color)
    {
        Vector3 offset = target.position - player.position;

        // 탐지 범위 안에만 표시
        if (offset.magnitude <= radarRange)
        {
            float angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg - player.eulerAngles.y;
            float distance = offset.magnitude / radarRange * (radarUI.rect.width / 2f);

            Vector2 pos = new Vector2(
                distance * Mathf.Sin(angle * Mathf.Deg2Rad),
                distance * Mathf.Cos(angle * Mathf.Deg2Rad)
            );

            // 블립 생성
            GameObject blip = Instantiate(blipPrefab, radarUI);
            blip.GetComponent<RectTransform>().anchoredPosition = pos;

            // 색상 적용
            Image img = blip.GetComponent<Image>();
            if (img != null)
                img.color = color;

            blips.Add(blip);
        }
    }
}

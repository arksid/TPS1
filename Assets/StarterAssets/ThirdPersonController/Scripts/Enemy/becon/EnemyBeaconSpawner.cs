using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyBeaconSpawner : MonoBehaviour
{
    [Header("Beacon Prefab")]
    public GameObject beaconPrefab;

    [Header("Spawn Area (around player)")]
    [Tooltip("플레이어 기준 최소/최대 거리")]
    public float minDistance = 10f;
    public float maxDistance = 18f;
    [Tooltip("NavMesh 보정 반경")]
    public float navSampleRadius = 8f;

    [Header("Auto Spawn On Play")]
    public bool autoSpawnOnPlay = true;
    [Tooltip("게임 시작 시 처음 떨어뜨릴 개수")]
    public int initialSpawnCount = 1;

    [Header("Repeat Spawn")]
    public bool repeatSpawn = false;
    [Tooltip("반복 드랍 간격(초)")]
    public float repeatInterval = 12f;
    [Tooltip("동시에 존재 가능한 비콘 최대 수(0 = 제한 없음)")]
    public int maxActiveBeacons = 3;

    private readonly List<GameObject> _liveBeacons = new List<GameObject>();

    [ContextMenu("Spawn Beacon Near Player (Test)")]
    public void SpawnBeaconNearPlayer_EditorTest()
    {
        SpawnBeaconNearPlayer();
    }

    private void Start()
    {
        // 시작 시 자동 드랍
        if (autoSpawnOnPlay)
        {
            for (int i = 0; i < Mathf.Max(0, initialSpawnCount); i++)
                SpawnBeaconNearPlayer();
        }

        // 반복 드랍
        if (repeatSpawn)
            InvokeRepeating(nameof(TryRepeatSpawn), repeatInterval, repeatInterval);
    }

    private void TryRepeatSpawn()
    {
        CleanupList();

        if (maxActiveBeacons > 0 && _liveBeacons.Count >= maxActiveBeacons)
            return;

        SpawnBeaconNearPlayer();
    }

    public void SpawnBeaconNearPlayer()
    {
        if (beaconPrefab == null)
        {
            Debug.LogError("[BeaconSpawner] beaconPrefab이 비어있습니다.");
            return;
        }

        Transform player = Character.Instance != null
            ? Character.Instance.transform
            : GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogError("[BeaconSpawner] Player를 찾을 수 없습니다.");
            return;
        }

        // 플레이어 주변 랜덤 지점
        Vector3 pos = GetRandomPosAround(player.position, minDistance, maxDistance);

        // NavMesh 보정
        if (NavMesh.SamplePosition(pos, out var hit, navSampleRadius, NavMesh.AllAreas))
            pos = hit.position;

        var beacon = Instantiate(beaconPrefab, pos, Quaternion.identity);
        _liveBeacons.Add(beacon);
        Debug.Log($"[BeaconSpawner] 비콘 소환 at {pos}");
    }

    private Vector3 GetRandomPosAround(Vector3 center, float min, float max)
    {
        float dist = Random.Range(min, max);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
        return center + offset;
    }

    private void CleanupList()
    {
        for (int i = _liveBeacons.Count - 1; i >= 0; i--)
        {
            if (_liveBeacons[i] == null)
                _liveBeacons.RemoveAt(i);
        }
    }
}

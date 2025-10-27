using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyBeaconSpawner : MonoBehaviour
{
    [Header("Beacon Prefab")]
    public GameObject beaconPrefab;

    [Header("Spawn Around Player")]
    [Tooltip("플레이어 기준 최소/최대 거리")]
    public float minDistance = 10f;
    public float maxDistance = 18f;

    [Header("Validation")]
    [Tooltip("NavMesh에서 위치 샘플링 반경")]
    public float navSampleRadius = 6f;

    [Tooltip("비콘 주위에 확보해야 하는 반경(이만큼 건물과 겹치면 실패)")]
    public float clearanceRadius = 1.5f;

    [Tooltip("겹치면 안 되는 레이어(건물/벽/지형 콜라이더 등)")]
    public LayerMask obstacleMask;

    [Tooltip("바닥을 찾기 위한 다운 레이캐스트 길이")]
    public float groundRaycastDistance = 30f;

    [Tooltip("스폰 시도 최대 횟수(유효 위치가 안 나오면 포기)")]
    public int maxAttempts = 12;

    [Header("Auto Spawn On Play")]
    public bool autoSpawnOnPlay = true;
    public int initialSpawnCount = 1;

    [Header("Repeat Spawn")]
    public bool repeatSpawn = false;
    public float repeatInterval = 12f;
    public int maxActiveBeacons = 3;

    private readonly List<GameObject> _liveBeacons = new List<GameObject>();

    private Transform _player;

    private void Start()
    {
        _player = Character.Instance != null
            ? Character.Instance.transform
            : GameObject.FindGameObjectWithTag("Player")?.transform;

        if (_player == null)
        {
            Debug.LogError("[BeaconSpawner] Player를 찾을 수 없습니다.");
            enabled = false;
            return;
        }

        if (autoSpawnOnPlay)
        {
            for (int i = 0; i < Mathf.Max(0, initialSpawnCount); i++)
                SpawnBeaconNearPlayer();
        }

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

    [ContextMenu("Spawn Beacon Near Player (Test)")]
    public void SpawnBeaconNearPlayer()
    {
        if (beaconPrefab == null)
        {
            Debug.LogError("[BeaconSpawner] beaconPrefab이 비어있습니다.");
            return;
        }

        if (_player == null)
        {
            Debug.LogError("[BeaconSpawner] Player를 찾을 수 없습니다.");
            return;
        }

        if (!TryGetValidPosition(_player.position, out Vector3 spawnPos))
        {
            Debug.LogWarning("[BeaconSpawner] 유효한 스폰 위치를 찾지 못했습니다.");
            return;
        }

        var beacon = Instantiate(beaconPrefab, spawnPos, Quaternion.identity);
        _liveBeacons.Add(beacon);
        Debug.Log($"[BeaconSpawner] 비콘 소환 at {spawnPos}");
    }

    private bool TryGetValidPosition(Vector3 center, out Vector3 result)
    {
        // 여러 번 시도하여 유효 위치 찾기
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 1) 플레이어 주변 랜덤 위치(수평)
            float dist = Random.Range(minDistance, maxDistance);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector3 candidate = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;

            // 2) NavMesh 샘플(근처 파란 바닥 점을 찾기)
            if (!NavMesh.SamplePosition(candidate, out var navHit, navSampleRadius, NavMesh.AllAreas))
                continue;

            Vector3 pos = navHit.position;

            // 3) 바닥으로 정확히 붙이기(혹시 살짝 떠 있거나 박혀있으면 보정)
            if (Physics.Raycast(pos + Vector3.up * groundRaycastDistance, Vector3.down, out var groundHit, groundRaycastDistance * 2f, ~0, QueryTriggerInteraction.Ignore))
            {
                pos = groundHit.point;
            }

            // 4) 겹침 검사(건물/벽과 겹치면 탈락)
            //    원형 체크: Physics.CheckSphere(반지름) — 필요시 CheckBox로 교체 가능
            bool overlap = Physics.CheckSphere(pos + Vector3.up * 0.2f, clearanceRadius, obstacleMask, QueryTriggerInteraction.Ignore);
            if (overlap)
                continue;

            // 5) NavMeshAgent로 워프가 가능한지(선택) — 장애물 가장자리 검사
            //    가장자리에 너무 붙으면 밀려나거나 끼일 수 있으므로 가장자리 거리도 확인
            if (NavMesh.FindClosestEdge(pos, out var edgeHit, NavMesh.AllAreas))
            {
                // 가장자리까지 거리가 너무 가까우면(예: 0.2m 이하) 재시도
                if (edgeHit.distance < 0.2f)
                    continue;
            }

            // 유효!
            result = pos;
            return true;
        }

        result = center;
        return false;
    }

    private void CleanupList()
    {
        for (int i = _liveBeacons.Count - 1; i >= 0; i--)
            if (_liveBeacons[i] == null) _liveBeacons.RemoveAt(i);
    }

    private void OnDrawGizmosSelected()
    {
        if (_player == null) return;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.15f);
        Gizmos.DrawWireSphere(_player.position, minDistance);
        Gizmos.DrawWireSphere(_player.position, maxDistance);
    }
}

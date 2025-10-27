using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// "플레이어 주변에 비콘 하나 생성" 유틸.
/// 1) 테스트는 ContextMenu로
/// 2) 게임 중엔 어디서든 SpawnBeaconNearPlayer() 호출
/// </summary>
public class EnemyBeaconSpawner : MonoBehaviour
{
    public GameObject beaconPrefab;

    [Tooltip("플레이어 기준 드롭 최소/최대 거리(지면 보정)")]
    public float minDistance = 10f;
    public float maxDistance = 18f;

    [Tooltip("NavMesh 보정 시도 반경")]
    public float navSampleRadius = 8f;

    [ContextMenu("Spawn Beacon Near Player (Test)")]
    public void SpawnBeaconNearPlayer_EditorTest()
    {
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

        // 플레이어 주변 랜덤 위치
        Vector3 pos = GetRandomPosAround(player.position, minDistance, maxDistance);

        // NavMesh 보정
        if (NavMesh.SamplePosition(pos, out var hit, navSampleRadius, NavMesh.AllAreas))
            pos = hit.position;

        Instantiate(beaconPrefab, pos, Quaternion.identity);
        Debug.Log($"[BeaconSpawner] 비콘 소환 at {pos}");
    }

    private Vector3 GetRandomPosAround(Vector3 center, float min, float max)
    {
        // 바닥 평면에 랜덤 링 샘플
        float dist = Random.Range(min, max);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
        return center + offset;
    }
}

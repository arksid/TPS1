using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySwarmDirector : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        [Tooltip("이 웨이브에서 소환할 총 적 수")]
        public int totalCount = 10;

        [Tooltip("한 번에 몇 마리씩 뿌릴지")]
        public int batchSize = 3;

        [Tooltip("한 배치 사이 간격(초)")]
        public float batchInterval = 2f;

        [Tooltip("지상/비행/저격 가중치(합이 1이 아니어도 됨)")]
        public float groundWeight = 1f;
        public float flyingWeight = 0f;
        public float sniperWeight = 0f;
    }

    [Header("플레이어(비워두면 Tag=Player 자동 탐색)")]
    public Transform player;

    [Header("스폰 포인트들")]
    public Transform[] spawnPoints;

    [Header("프리팹 등록")]
    public GameObject[] groundEnemyPrefabs; // EnemyController 계열
    public GameObject[] flyingEnemyPrefabs; // FlyingEnemyController
    public GameObject[] sniperEnemyPrefabs; // SniperEnemy(EnemyController 계열)

    [Header("웨이브 구성")]
    public List<Wave> waves = new List<Wave>();

    [Header("지상 스폰 옵션")]
    public float navmeshSampleRadius = 2f;
    public float groundSpawnYOffset = 0.2f;

    [Header("비행 스폰 옵션")]
    public float flyingSpawnHeight = 8f;

    [Header("웨이브 사이 대기(초)")]
    public float intervalBetweenWaves = 5f;

    [Header("자동 시작")]
    public bool autoStartOnPlay = false; // ⬅️ false
    public bool startDisabled = true;    // ⬅️ true (Awake에서 SetActive(false))

    // === 추가: 중지 플래그/메서드 ===
    bool _stopRequested = false;
    public void RequestStopWaves()
    {
        _stopRequested = true;
        StopAllCoroutines(); // 진행 중 코루틴 즉시 중단
        Debug.Log("[EnemySwarmDirector] 웨이브 중지 요청");
    }

    void Awake()
    {
        if (startDisabled)
        {
            // 게임 시작 직후 스스로 꺼짐
            gameObject.SetActive(false);
            return; // 꺼졌으니 이하 로직 미실행
        }
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (autoStartOnPlay) StartCoroutine(RunWaves());
    }

    public IEnumerator RunWaves()
    {
        _stopRequested = false;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[EnemySwarmDirector] spawnPoints가 비었습니다.");
            yield break;
        }

        for (int w = 0; w < waves.Count; w++)
        {
            if (_stopRequested) yield break; // ⬅️ 중지 즉시 종료

            var wave = waves[w];
            int spawned = 0;
            Debug.Log($"[EnemySwarmDirector] Wave {w + 1}/{waves.Count} 시작 (총 {wave.totalCount})");

            while (spawned < wave.totalCount)
            {
                if (_stopRequested) yield break; // ⬅️ 중지 즉시 종료

                int spawnNow = Mathf.Min(wave.batchSize, wave.totalCount - spawned);
                for (int i = 0; i < spawnNow; i++)
                {
                    SpawnOneByWeights(wave);
                    spawned++;
                }
                if (spawned < wave.totalCount)
                    yield return new WaitForSeconds(wave.batchInterval);
            }

            if (w < waves.Count - 1)
            {
                Debug.Log($"[EnemySwarmDirector] Wave {w + 1} 종료. {intervalBetweenWaves}초 대기");
                float t = 0f;
                while (t < intervalBetweenWaves && !_stopRequested)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                if (_stopRequested) yield break;
            }
        }

        Debug.Log("[EnemySwarmDirector] 모든 웨이브 종료");
    }

    void SpawnOneByWeights(Wave wave)
    {
        float gw = Mathf.Max(0, wave.groundWeight);
        float fw = Mathf.Max(0, wave.flyingWeight);
        float sw = Mathf.Max(0, wave.sniperWeight);
        float sum = gw + fw + sw;
        if (sum <= 0f) { gw = 1f; sum = 1f; }

        float r = Random.value * sum;

        if (r < gw && groundEnemyPrefabs.Length > 0)
        {
            SpawnGround(groundEnemyPrefabs[Random.Range(0, groundEnemyPrefabs.Length)]);
            return;
        }
        r -= gw;

        if (r < fw && flyingEnemyPrefabs.Length > 0)
        {
            SpawnFlying(flyingEnemyPrefabs[Random.Range(0, flyingEnemyPrefabs.Length)]);
            return;
        }
        if (sniperEnemyPrefabs.Length > 0)
        {
            SpawnGround(sniperEnemyPrefabs[Random.Range(0, sniperEnemyPrefabs.Length)]);
            return;
        }

        Debug.LogWarning("[EnemySwarmDirector] 적 프리팹이 비어있습니다.");
    }

    void SpawnGround(GameObject prefab)
    {
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 pos = sp.position + Vector3.up * groundSpawnYOffset;

        if (NavMesh.SamplePosition(pos, out var hit, navmeshSampleRadius, NavMesh.AllAreas))
            pos = hit.position;

        var go = Instantiate(prefab, pos, Quaternion.identity);

        var ec = go.GetComponent<EnemyController>();
        if (ec != null && player != null)
        {
            ec.SetPlayer(player);
        }
        else
        {
            Debug.LogWarning($"[EnemySwarmDirector] {prefab.name}에 EnemyController가 없거나 player가 없습니다.");
        }
    }

    void SpawnFlying(GameObject prefab)
    {
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 pos = sp.position + Vector3.up * flyingSpawnHeight;

        var go = Instantiate(prefab, pos, Quaternion.identity);

        var fe = go.GetComponent<FlyingEnemyController>();
        if (fe != null && player != null)
        {
            fe.SetTarget(player);
        }
        else
        {
            Debug.LogWarning($"[EnemySwarmDirector] {prefab.name}에 FlyingEnemyController가 없거나 player가 없습니다.");
        }
    }
    // 중지 플래그/메서드가 이미 있다면 재사용하고,
    // 없다면 최소한 이 메서드만 추가하세요.
    public void EndWave()
    {
        // 예: RequestStopWaves()가 있다면 연결
        // RequestStopWaves();

        // 없다면 최소 구현:
        StopAllCoroutines();
        Debug.Log("[EnemySwarmDirector] EndWave 호출 → 모든 웨이브 중지");
    }

#if UNITY_EDITOR
    [ContextMenu("▶ 테스트: 웨이브 즉시 시작")]
    void EditorStartWaves()
    {
        StopAllCoroutines();
        StartCoroutine(RunWaves());
    }

    [ContextMenu("＋ 테스트: 지상형 1마리")]
    void EditorSpawnGroundOnce()
    {
        if (groundEnemyPrefabs.Length > 0) SpawnGround(groundEnemyPrefabs[0]);
    }

    [ContextMenu("＋ 테스트: 비행형 1마리")]
    void EditorSpawnFlyingOnce()
    {
        if (flyingEnemyPrefabs.Length > 0) SpawnFlying(flyingEnemyPrefabs[0]);
    }
#endif
}

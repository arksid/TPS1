using UnityEngine;
using System.Collections;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject[] enemyPrefabs;      // 여러 적 종류
    public Transform[] spawnPoints;        // 스폰 포인트
    public float spawnRadius = 10f;        // 스폰 반경
    public int enemiesPerWave = 5;         // 한 웨이브당 적 수
    public float spawnDelay = 0.5f;        // 적 개별 스폰 간격
    public float waveDelay = 5f;           // 웨이브 간 대기시간

    [Header("플레이어 설정")]
    public Transform playerTransform;

    private int currentWave = 0;

    private void Start()
    {
        // 플레이어 자동 탐색
        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        // 웨이브 시작
        StartCoroutine(StartWaves());
    }

    private IEnumerator StartWaves()
    {
        while (true)
        {
            currentWave++;
            Debug.Log($"🌊 웨이브 {currentWave} 시작!");
            yield return StartCoroutine(SpawnWave());
            yield return new WaitForSeconds(waveDelay);
        }
    }

    private IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnRandomEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SpawnRandomEnemy()
    {
        if (enemyPrefabs.Length == 0) return;

        // ✅ 1. 랜덤한 적 선택
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // ✅ 2. 랜덤 스폰 위치 계산
        Vector3 spawnPos = GetRandomSpawnPosition();

        // ✅ 3. 적 생성
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // ✅ 4. 플레이어 지정 (공통)
        var enemyController = enemy.GetComponent<EnemyController>();
        var suicideEnemy = enemy.GetComponent<SuicideEnemyController>();

        if (playerTransform != null)
        {
            if (enemyController != null)
                enemyController.SetPlayer(playerTransform);
            if (suicideEnemy != null)
            {
                // 자폭형은 직접 target을 넣는 구조일 수도 있으므로
                suicideEnemy.SendMessage("SetTarget", playerTransform, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePos;

        // 특정 스폰포인트 중 하나 선택
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            basePos = point.position;
        }
        else
        {
            basePos = transform.position;
        }

        // 지정 반경 내 무작위 위치 생성
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 finalPos = basePos + new Vector3(randomOffset.x, 0f, randomOffset.y);

        return finalPos;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (var point in spawnPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, spawnRadius);
            }
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
#endif
}

using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
    public float spawnRadius = 10f;
    public int enemiesPerWave = 5;
    public float spawnDelay = 0.5f;
    public float waveDelay = 5f;

    [Header("플레이어 설정")]
    public Transform playerTransform;

    private int currentWave = 0;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

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
        if (enemyPrefabs.Length == 0)
        {
            Debug.LogError("❌ enemyPrefabs 비어있음!");
            return;
        }
        if (playerTransform == null)
        {
            Debug.LogError("❌ playerTransform이 null입니다!");
            return;
        }

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // ✅ 스폰 위치 샘플링(가능하면 NavMesh 위로 보정)
        Vector3 spawnPos = GetRandomSpawnPosition();
        if (NavMesh.SamplePosition(spawnPos, out var hitPos, 5f, NavMesh.AllAreas))
            spawnPos = hitPos.position;

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // ✅ 스폰 직후 에이전트 워프(경계/공중 대비)
        var ag = enemy.GetComponent<NavMeshAgent>();
        if (ag && NavMesh.SamplePosition(enemy.transform.position, out var navHit, 5f, NavMesh.AllAreas))
        {
            ag.Warp(navHit.position);
        }

        var flyingEnemy = enemy.GetComponent<FlyingEnemyController>();
        if (flyingEnemy != null)
        {
            Debug.Log($"✅ {prefab.name}에 FlyingEnemyController 발견, 타겟 설정 시도");
            flyingEnemy.SetTarget(playerTransform);
        }
        // NavMesh 에이전트형 적(지상)이라면 EnemyController가 알아서 추적할 것임
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePos;
        if (spawnPoints != null && spawnPoints.Length > 0)
            basePos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        else
            basePos = transform.position;

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        return basePos + new Vector3(randomOffset.x, 0f, randomOffset.y);
    }
}


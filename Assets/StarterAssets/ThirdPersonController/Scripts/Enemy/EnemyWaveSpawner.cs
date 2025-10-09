using UnityEngine;
using System.Collections;

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
        if (enemyPrefabs.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        var enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null && playerTransform != null)
            enemyController.SetPlayer(playerTransform);
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

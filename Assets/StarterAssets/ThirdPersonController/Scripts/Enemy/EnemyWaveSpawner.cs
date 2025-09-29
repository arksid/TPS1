using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject[] enemyPrefabs;       // 일반 적 프리팹 배열
    public GameObject semiBossPrefab;       // 보스 프리팹
    public Transform[] spawnPoints;         // 스폰 위치 배열
    public float spawnDelay = 0.5f;         // 스폰 간격

    [Header("Wave Settings")]
    public int enemiesPerWave = 5;
    public int totalWaves = 3;
    public float timeBetweenWaves = 5f;

    private int currentWave = 0;
    private bool spawning = false;
    private List<GameObject> aliveEnemies = new List<GameObject>();
    private Transform playerTransform;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        while (currentWave < totalWaves)
        {
            spawning = true;
            currentWave++;

            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnDelay);
            }

            spawning = false;

            // 다음 웨이브까지 대기
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        // 보스 웨이브
        SpawnBoss();
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject newEnemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        // 플레이어 설정
        EnemyController controller = newEnemy.GetComponent<EnemyController>();
        if (controller != null)
            controller.SetPlayer(playerTransform);

        // 🔥 Radar 등록
        if (RadarManager.Instance != null)
            RadarManager.Instance.RegisterEnemy(newEnemy.transform);

        aliveEnemies.Add(newEnemy);
    }

    private void SpawnBoss()
    {
        if (semiBossPrefab == null || spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject boss = Instantiate(semiBossPrefab, spawnPoint.position, spawnPoint.rotation);

        // 플레이어 설정
        SemiBossController controller = boss.GetComponent<SemiBossController>();
        if (controller != null)
            controller.SetPlayer(playerTransform);

        // 🔥 Radar 등록 (보스도 적으로 취급)
        if (RadarManager.Instance != null)
            RadarManager.Instance.RegisterEnemy(boss.transform);

        aliveEnemies.Add(boss);
    }
}

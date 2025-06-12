using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs; //  다양한 적 프리팹
    public Transform[] spawnPoints;
    public Transform playerTransform;

    [Header("Wave Settings")]
    public int enemiesPerWave = 5;
    public float spawnDelay = 0.5f;
    public float waveDelay = 5f;
    public int maxWaves = 5;

    [Header("Boss Settings")]
    public GameObject semiBossPrefab;
    public int semiBossWaveInterval = 5;

    private int currentWave = 1;
    private List<GameObject> aliveEnemies = new List<GameObject>();
    private bool isSpawning = false;
    private bool gameCleared = false;

    void Update()
    {
        if (gameCleared)
            return;

        aliveEnemies.RemoveAll(e => e == null);

        if (currentWave > maxWaves && aliveEnemies.Count == 0)
        {
            Debug.Log("게임 클리어!");
            gameCleared = true;
            return;
        }

        if (!isSpawning && aliveEnemies.Count == 0)
        {
            StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        isSpawning = true;
        Debug.Log($"Wave {currentWave} 시작!");

        // 세미보스 웨이브 체크
        if (currentWave % semiBossWaveInterval == 0 && semiBossPrefab != null)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject boss = Instantiate(semiBossPrefab, spawnPoint.position, spawnPoint.rotation);

            //  플레이어 설정
            SemiBossController controller = boss.GetComponent<SemiBossController>();
            if (controller != null)
                controller.SetPlayer(playerTransform); // 여기 중요!

            aliveEnemies.Add(boss);
        }
        else
        {
            for (int i = 0; i < enemiesPerWave; i++)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                GameObject newEnemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

                EnemyController controller = newEnemy.GetComponent<EnemyController>();
                if (controller != null)
                    controller.SetPlayer(playerTransform);

                aliveEnemies.Add(newEnemy);
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        currentWave++;
        enemiesPerWave += 2;
        yield return new WaitForSeconds(waveDelay);
        isSpawning = false;
    }
}


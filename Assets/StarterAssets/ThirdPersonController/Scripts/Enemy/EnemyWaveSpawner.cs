using UnityEngine;
using System.Collections;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int enemiesPerWave = 5;
    public float spawnDelay = 0.5f;
    public float waveDelay = 5f;

    [Header("플레이어 설정")]
    public Transform playerTransform;

    private int currentWave = 0;
    private bool spawning = false;

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
            Debug.Log($"웨이브 {currentWave} 시작!");
            yield return StartCoroutine(SpawnWave());
            yield return new WaitForSeconds(waveDelay);
        }
    }

    private IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoints.Length == 0) return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);

        // ✅ 웨이브 스폰 시 플레이어 지정
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null && playerTransform != null)
        {
            controller.SetPlayer(playerTransform);
        }
    }
}

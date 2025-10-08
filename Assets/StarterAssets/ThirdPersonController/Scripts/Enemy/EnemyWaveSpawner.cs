using UnityEngine;
using System.Collections;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public string[] enemyKeys = { "NormalEnemy", "SuicideEnemy", "FlyingEnemy" }; // PoolManager 키들
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
        if (enemyKeys.Length == 0) return;

        // ✅ 1. 랜덤 키 선택
        string key = enemyKeys[Random.Range(0, enemyKeys.Length)];

        // ✅ 2. 랜덤 위치
        Vector3 spawnPos = GetRandomSpawnPosition();

        // ✅ 3. PoolManager에서 가져오기
        GameObject enemy = PoolManager.Instance.Get(key, spawnPos, Quaternion.identity);
        if (enemy == null) return;

        // ✅ 4. 초기화
        var enemyController = enemy.GetComponent<EnemyController>();
        var suicideEnemy = enemy.GetComponent<SuicideEnemyController>();
        var flyingEnemy = enemy.GetComponent<SmartFlyingEnemyController>();


        if (playerTransform != null)
        {
            if (enemyController != null)
                enemyController.SetPlayer(playerTransform);
            if (suicideEnemy != null)
                suicideEnemy.SendMessage("SetTarget", playerTransform, SendMessageOptions.DontRequireReceiver);
            if (flyingEnemy != null)
                flyingEnemy.SendMessage("SetTarget", playerTransform, SendMessageOptions.DontRequireReceiver);
        }

        // ✅ 풀에서 꺼냈으므로 체력, NavMeshAgent 등 초기화 필요
        if (enemyController != null) enemyController.ResetEnemy();
        if (suicideEnemy != null) suicideEnemy.ResetEnemy();
        if (flyingEnemy != null) flyingEnemy.ResetEnemy();
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

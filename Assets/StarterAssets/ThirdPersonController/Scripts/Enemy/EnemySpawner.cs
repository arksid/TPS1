using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;

    public void SpawnEnemy()
    {
        int index = Random.Range(0, spawnPoints.Length);
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoints[index].position, Quaternion.identity);

        // 🔥 여기서만 레이더 등록
        if (RadarManager.Instance != null)
        {
            RadarManager.Instance.RegisterEnemy(newEnemy.transform);
        }
    }
}

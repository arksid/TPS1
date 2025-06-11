using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public Transform playerTransform;

    [Header("Wave Settings")]
    public int enemiesPerWave = 5;
    public float spawnDelay = 0.5f;
    public float waveDelay = 5f;
    public int maxWaves = 5;  //  �߰�: �ִ� ���̺� ��

    private int currentWave = 1;
    private List<GameObject> aliveEnemies = new List<GameObject>();
    private bool isSpawning = false;
    public TextMeshProUGUI waveText;
    void Update()
    {
        if (currentWave > maxWaves)
            return; //  �ִ� ���̺� ������ �ƹ��͵� �� ��

        aliveEnemies.RemoveAll(e => e == null);

        if (!isSpawning && aliveEnemies.Count == 0)
        {
            StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        isSpawning = true;

        // UI ������Ʈ
        if (waveText != null)
        {
            if (currentWave <= maxWaves)
                waveText.text = $"Wave: {currentWave}";
            else
                waveText.text = "All Waves Cleared!";
        }
        Debug.Log($"Wave {currentWave} ����!");
        for (int i = 0; i < enemiesPerWave; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            EnemyController controller = newEnemy.GetComponent<EnemyController>();
            if (controller != null)
                controller.SetPlayer(playerTransform);

            aliveEnemies.Add(newEnemy);
            yield return new WaitForSeconds(spawnDelay);
        }

        // ���� ���̺� �غ�
        currentWave++;
        enemiesPerWave += 2; // ���� ���� (�ɼ�)
        yield return new WaitForSeconds(waveDelay);
        isSpawning = false;
    }
}


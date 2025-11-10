using System.Collections;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject bossPrefab;
    public Transform spawnPoint;
    public float spawnDelay = 0f;

    [Header("한 번만 스폰")]
    public bool spawnOnce = true;
    GameObject _spawned;

    public bool HasSpawned => _spawned != null;

    public void SpawnNow()
    {
        if (spawnOnce && _spawned != null) return;
        StartCoroutine(CoSpawn());
    }

    IEnumerator CoSpawn()
    {
        if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : transform.rotation;
        _spawned = Instantiate(bossPrefab, pos, rot);
    }
}

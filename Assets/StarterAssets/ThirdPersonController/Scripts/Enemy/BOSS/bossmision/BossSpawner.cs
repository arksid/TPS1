using System.Collections;
using UnityEngine;
using UnityEngine.Events; // ★ 이벤트 사용

public class BossSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject bossPrefab;
    public Transform spawnPoint;
    [Min(0)] public float spawnDelay = 0f;
    public bool spawnOnce = true;

    [Header("Bind UI On Spawn")]
    public bool showBossUIOnSpawn = true;
    public string uiDisplayName = "BOSS";
    public BossUIBinder uiBinder;           // 비우면 BossUIBinder.Instance 사용

    [Header("Events")]
    public UnityEvent<BossMonster> onSpawned; // ★ BossRoomGuide가 구독하는 이벤트

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

        // 보스 컴포넌트 찾기
        var boss = _spawned.GetComponentInChildren<BossMonster>(true);
        if (!boss)
        {
            Debug.LogWarning("[BossSpawner] BossMonster 컴포넌트를 찾지 못했습니다.");
            yield break;
        }

        // UI 연결 (선택)
        if (showBossUIOnSpawn)
        {
            var binder = uiBinder ? uiBinder : BossUIBinder.Instance;
            if (binder)
            {
                binder.ShowFor(boss, uiDisplayName);
            }
            else
            {
                Debug.LogWarning("[BossSpawner] BossUIBinder가 씬에 없습니다.");
            }
        }

        // ★ BossRoomGuide가 기다리는 이벤트 호출
        onSpawned?.Invoke(boss);
    }
}

using System.Collections;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NextMissionTrigger : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";
    public bool onlyOnce = true;

    [Header("킬 미션 (50마리)")]
    public Kill100Mission killMission;      // 미션 컴포넌트(비활성로 배치 → 여기서 켬)

    [Header("스포너 대상")]
    [Tooltip("실제 스포너 컴포넌트를 드래그 (예: EnemyWaveSpawner, EnemySwarmDirector 등)")]
    public MonoBehaviour spawner;
    public GameObject spawnerGameObject;    // 비우면 spawner.gameObject 사용

    [Header("활성 보장")]
    public bool activateGameObject = true;  // 스포너 GO가 꺼져 있으면 켜줌
    public bool enableComponent = true;   // 스포너 컴포넌트가 꺼져 있으면 켜줌

    [Header("시작 메서드 후보 (자동 탐색 순서)")]
    public string[] startMethodCandidates = new[] { "RunWaves", "StartSpawning", "StartSpawn", "StartWave", "Begin", "Play" };

    [Header("웨이포인트 정리 옵션")]
    public bool clearWaypointOnStart = true;       // 웨이브 시작 시 마커/메시지 제거
    public bool disableOutlinesOnStart = true;     // (선택) 아웃라인도 끄기
    public GameObject[] outlinesToDisable;         // 끌 대상 오브젝트(없으면 생략)

    bool _fired;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired && onlyOnce) return;
        if (!other.CompareTag(playerTag)) return;

        // 1) (선택) 웨이포인트/메시지/아웃라인 먼저 정리
        if (clearWaypointOnStart) ClearGuidesNow();

        // 2) 킬 미션 활성화 (인스펙터에서 targetKills=50 확인)
        if (killMission && !killMission.gameObject.activeSelf)
            killMission.gameObject.SetActive(true);

        // 3) 스포너 시작
        TryStartSpawner();

        _fired = true;
    }

    [ContextMenu("Clear Guides Now")]
    public void ClearGuidesNow()
    {
        WaypointDirector.Clear(); // 마커 + 메시지 싹 정리

        if (disableOutlinesOnStart && outlinesToDisable != null)
        {
            foreach (var go in outlinesToDisable)
                if (go) OutlineHelper.SetOutline(go, false);
        }
    }

    public void TryStartSpawner()
    {
        if (!spawner)
        {
            Debug.LogWarning("[NextMissionTrigger] spawner(컴포넌트)가 비어 있습니다.");
            return;
        }

        var go = spawnerGameObject ? spawnerGameObject : spawner.gameObject;

        if (activateGameObject && go && !go.activeSelf) go.SetActive(true);
        if (enableComponent && !spawner.enabled) spawner.enabled = true;

        // 시작 메서드 자동 탐색
        MethodInfo found = null;
        foreach (var name in startMethodCandidates)
        {
            found = spawner.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (found != null) break;
        }

        if (found == null)
        {
            Debug.LogWarning("[NextMissionTrigger] 시작 메서드를 찾지 못했습니다. 후보: " +
                             string.Join(", ", startMethodCandidates));
            return;
        }

        // IEnumerator면 코루틴으로 실행
        var ret = found.Invoke(spawner, null);
        if (ret is IEnumerator ie) StartCoroutine(ie);
    }
}

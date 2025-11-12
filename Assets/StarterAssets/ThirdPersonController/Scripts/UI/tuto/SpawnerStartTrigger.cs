using System.Collections;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpawnerStartTrigger : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("스포너 대상")]
    [Tooltip("시작시킬 스포너 컴포넌트를 직접 드래그(예: EnemyWaveSpawner, EnemySwarmDirector 등)")]
    public MonoBehaviour spawner;            // ★ 여기에 스포너 컴포넌트를 직접 넣으세요
    public GameObject spawnerGameObject;     // 비워두면 spawner.gameObject 사용

    [Header("실행 타이밍")]
    public bool startOnEnable = false;       // 활성 시 즉시 시작(선택)
    public bool startOnEnter = true;         // 플레이어가 이 트리거에 들어오면 시작
    public bool onlyOnce = true;             // 한 번만 실행

    [Header("활성 보장")]
    public bool activateGameObject = true;   // 스포너 GO가 비활성이면 켜줌
    public bool enableComponent = true;      // 스포너 컴포넌트가 꺼져있으면 켜줌

    [Header("실행 방식(자동 탐색)")]
    [Tooltip("아래 이름 순서대로 메서드를 찾아 실행합니다. 존재하는 첫 번째를 사용.")]
    public string[] startMethodCandidates = new[] {
        "RunWaves", "StartSpawning", "StartSpawn", "StartWave", "Begin", "Play"
    };

    bool _started;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        if (startOnEnable) TryStart();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!startOnEnter) return;
        if (!other.CompareTag(playerTag)) return;

        TryStart();
    }

    public void TryStart()
    {
        if (onlyOnce && _started) return;

        var targetComp = spawner;
        if (!targetComp)
        {
            Debug.LogWarning("[SpawnerStartTrigger] spawner(컴포넌트)가 비어 있습니다.");
            return;
        }

        var go = spawnerGameObject ? spawnerGameObject : targetComp.gameObject;

        // 1) 활성 보장
        if (activateGameObject && !go.activeSelf) go.SetActive(true);
        if (enableComponent && !targetComp.enabled) targetComp.enabled = true;

        // 2) 시작 메서드 탐색
        MethodInfo found = null;
        foreach (var name in startMethodCandidates)
        {
            found = targetComp.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (found != null) break;
        }

        if (found == null)
        {
            Debug.LogWarning("[SpawnerStartTrigger] 시작 메서드를 찾지 못했습니다. 후보: " +
                             string.Join(", ", startMethodCandidates));
            return;
        }

        // 3) 반환형에 따라 호출 방식 결정
        var retType = found.ReturnType;

        try
        {
            if (retType == typeof(IEnumerator))
            {
                // IEnumerator면 코루틴으로 실행(이 트리거에서 안전하게 실행)
                var enumerator = (IEnumerator)found.Invoke(targetComp, null);
                if (enumerator != null) StartCoroutine(enumerator);
            }
            else
            {
                // void 등은 그냥 호출
                found.Invoke(targetComp, null);
            }

            _started = true;
            // Debug.Log($"[SpawnerStartTrigger] '{found.Name}' 실행 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SpawnerStartTrigger] 실행 중 예외: " + ex.Message);
        }
    }
}

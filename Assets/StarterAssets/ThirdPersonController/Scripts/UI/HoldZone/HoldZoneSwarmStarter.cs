using System.Collections;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HoldZoneSwarmStarter : MonoBehaviour
{
    [Header("시작 조건")]
    public bool startOnEnable = false;       // 트리거 오브젝트가 켜질 때 즉시 시작
    public bool startOnEnter = true;         // 플레이어가 트리거에 들어오면 시작
    public bool useProximityStart = true;    // 거리 조건으로도 시작(트리거 실패 대비)
    public float startRadius = 4f;           // 근접 시작 반경

    [Header("대상(스포너/디렉터)")]
    public MonoBehaviour swarm;              // EnemySwarmDirector 할당
    public Transform holdZoneCenter;         // 없으면 이 오브젝트의 위치 사용
    public Transform player;                 // 없으면 Tag=Player 자동 탐색

    [Header("한 번만 시작")]
    public bool onlyOnce = true;

    bool _started;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        // 플레이어 자동 연결
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void OnEnable()
    {
        // 옵션: 켜지자마자 시작
        if (startOnEnable) TryStart();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!startOnEnter || _started) return;
        if (!other.CompareTag("Player")) return;
        TryStart();
    }

    void Update()
    {
        if (!useProximityStart || _started) return;
        if (player == null) return;

        Vector3 center = holdZoneCenter ? holdZoneCenter.position : transform.position;
        if ((player.position - center).sqrMagnitude <= startRadius * startRadius)
        {
            Debug.Log("[HoldZoneSwarmStarter] 근접 조건 충족 → 웨이브 시작");
            TryStart();
        }
    }

    void TryStart()
    {
        if (_started && onlyOnce) return;
        if (swarm == null)
        {
            Debug.LogWarning("[HoldZoneSwarmStarter] swarm 미지정");
            return;
        }

        var t = swarm.GetType();

        // (선호) 먼저 EndWave() 있으면 호출 → 이전 런 잔여 상태 초기화
        var endWave = t.GetMethod("EndWave", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (endWave != null)
        {
            endWave.Invoke(swarm, null);
            Debug.Log("[HoldZoneSwarmStarter] EndWave() 호출");
        }

        // StartWaves(void) 우선 시도
        var startWaves = t.GetMethod("StartWaves", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (startWaves != null)
        {
            startWaves.Invoke(swarm, null);
            _started = true;
            Debug.Log("[HoldZoneSwarmStarter] StartWaves() 호출 → 시작됨");
            return;
        }

        // RunWaves() (IEnumerator) 코루틴 실행 시도
        var runWaves = t.GetMethod("RunWaves", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (runWaves != null)
        {
            var routine = runWaves.Invoke(swarm, null) as IEnumerator;
            if (routine != null)
            {
                // ★ 전역 러너로 안전 실행 (비활성/타이밍 이슈 방지)
                GameFlowRunner.Run(routine);
                _started = true;
                Debug.Log("[HoldZoneSwarmStarter] RunWaves() 코루틴 시작");
                return;
            }
        }

        Debug.LogWarning("[HoldZoneSwarmStarter] StartWaves()/RunWaves() 둘 다 찾지 못했습니다.");
    }
}

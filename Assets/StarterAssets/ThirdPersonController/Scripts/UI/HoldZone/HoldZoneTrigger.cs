using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HoldZoneTrigger : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("연결")]
    public SimpleWaypointUI waypointUI; // 마커 UI
    public Transform holdZone;          // 거점의 가시 오브젝트(아웃라인 대상). 비우면 자기 transform
    public HoldZoneMission mission;     // 같은 구역의 미션 컴포넌트
    public EnemySwarmDirector swarm;    // 웨이브 디렉터

    [Header("표시 문구")]
    [TextArea] public string message = "지정된 구역으로 이동해 거점을 유지하세요!";

    [Header("동작 옵션")]
    public bool hideWaypointUIOnEnable = true; // 트리거 켜지면 UI만 끔(아웃라인 유지)
    public bool clearOnEnter = true;           // 들어오면 표식 완전 정리
    public bool startSwarmOnEnable = true;     // 켜지는 순간 웨이브 시작(튜토리얼 완료 이후에만)
    public bool startSwarmOnEnter = false;    // 들어오면 시작

    bool _fired;
    bool _swarmStarted;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        // UI만 숨김(아웃라인은 유지)
        if (hideWaypointUIOnEnable)
            WaypointDirector.HideUIOnly();

        // 플레이어 유도(튜토리얼 완료 후에만 허용)
        var target = holdZone ? holdZone : transform;
        if (WaypointDirector.HintsEnabled)
            WaypointDirector.Show(waypointUI, target, message);

        // 켜질 때 웨이브 시작(튜토리얼 완료 후에만)
        if (startSwarmOnEnable && WaypointDirector.HintsEnabled)
            TryStartSwarmOnce();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (clearOnEnter)
            WaypointDirector.Clear(); // 표식 정리(마커+아웃라인)

        if (!_fired)
        {
            _fired = true;

            if (startSwarmOnEnter)
                TryStartSwarmOnce();

            // 미션 UI 보이기(옵션)
            if (mission && mission.ui) mission.ui.Show();
        }
    }

    void TryStartSwarmOnce()
    {
        if (_swarmStarted) return;
        if (!swarm)
        {
            Debug.LogWarning("[HoldZoneTrigger] EnemySwarmDirector 미지정");
            return;
        }

        if (!swarm.gameObject.activeSelf) swarm.gameObject.SetActive(true);
        if (!swarm.enabled) swarm.enabled = true;

        // 스포너 내부 코루틴 정리 후 시작
        if (swarm.isActiveAndEnabled) swarm.StopAllCoroutines();
        StartCoroutine(swarm.RunWaves());

        _swarmStarted = true;
        Debug.Log("[HoldZoneTrigger] 웨이브 시작");
    }
}

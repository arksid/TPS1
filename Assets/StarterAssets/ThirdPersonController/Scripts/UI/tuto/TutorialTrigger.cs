using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("연결(기존 UI/미션)")]
    public TutorialUI ui;                // 상단 안내(제목/힌트)
    public Kill100Mission killMission;   // 100킬 미션 UI & 로직
    public PlayerControlLocker locker;   // 조작 잠금/해제 (선택)

    [Header("진행 옵션")]
    public bool makePlayerInvincible = true;    // 안내 중 무적
    public bool lockControlsDuringIntro = true; // 안내 중 조작잠금
    public KeyCode continueKey = KeyCode.E;     // 다음 단계 진행 키
    public float autoContinueAfter = 0f;        // 0이면 키 기다림

    [Header("안내 문구")]
    [TextArea] public string introTitle = "튜토리얼: 미션 안내";
    [TextArea] public string introHint = "E키를 눌러 진행하세요.\n이번 미션: 적 100마리 처치";
    [TextArea] public string missionTitle = "미션 시작!";
    [TextArea] public string missionHint = "좌상단 게이지를 확인하세요.";

    [Header("다음 지역 유도(마커/아웃라인)")]
    public SimpleWaypointUI waypointUI;         // Canvas 아래 SimpleWaypointUI
    public Transform nextDestination;           // 목적지(없으면 자기 위치)
    [TextArea] public string nextMessage = "다음 지역으로 이동해";

    [Header("트리거 활성/진입 시 처리")]
    public bool showOnEnable = false;           // 활성 시 자동표시 금지(요구사항)
    public bool hideWaypointUIOnEnable = true;  // 활성 시 "UI만" 숨김(아웃라인 유지)
    public bool clearOnEnter = true;     
    
    // 진입 시 마커+아웃라인 완전 정리
    [Header("스포너(웨이브)")]
    public EnemySwarmDirector swarm;
    public bool startSwarmOnEnable = false; // ⬅️ false 로 바꿔주세요
    public bool startSwarmOnEnter = true;  // ⬅️ true 로 바꿔주세요

    bool _fired;
    bool _swarmStarted; // 중복 실행 방지



    void Reset()
    {

        // 안정적 트리거를 위한 Kinematic Rigidbody 권장
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnEnable()
    {
        // 요구사항: 트리거가 켜지면 웨이포인트 "UI만" 숨기고(아웃라인 유지)
        if (hideWaypointUIOnEnable)
            WaypointDirector.HideUIOnly();

        // ★ 변경 포인트:
        // - startSwarmOnEnable == true 이고
        // - 튜토리얼이 완료되어 WaypointDirector.HintsEnabled == true 일 때만
        //   트리거가 켜지는 순간에 스폰 시작
        if (startSwarmOnEnable && WaypointDirector.HintsEnabled)
            TryStartSwarmOnce();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;

        // (요구사항) 트리거에 들어오면 표식 완전 정리
        if (clearOnEnter)
            WaypointDirector.Clear();

        // ★ 원한다면 '들어왔을 때' 시작도 가능(옵션)
        if (startSwarmOnEnter)
            TryStartSwarmOnce();

        _fired = true;

        var player = other.GetComponentInParent<Character>() ?? other.GetComponent<Character>();
        StartCoroutine(RunTutorial(player));
    }

    void TryStartSwarmOnce()
    {
        if (_swarmStarted) return;

        if (swarm == null)
        {
            Debug.LogWarning("[TutorialTrigger] swarm(EnemySwarmDirector) 미지정");
            return;
        }

        // 1) 스포너 오브젝트/컴포넌트 활성 보장
        if (!swarm.gameObject.activeSelf)
        {
            Debug.Log("[TutorialTrigger] EnemySwarmDirector GameObject가 비활성 → 활성화합니다.");
            swarm.gameObject.SetActive(true);
        }
        if (!swarm.enabled)
        {
            Debug.Log("[TutorialTrigger] EnemySwarmDirector 컴포넌트가 비활성 → 활성화합니다.");
            swarm.enabled = true;
        }

        // 2) (선택) 스포너 내부에 돌고 있는 코루틴이 있었다면 안전하게 정리
        if (swarm.isActiveAndEnabled)
            swarm.StopAllCoroutines();

        // 3) ★중요: '스포너'가 아니라 '현재 활성 MonoBehaviour(이 스크립트)'에서 코루틴을 시작
        //    이렇게 하면 스포너 오브젝트가 비활성이어도 시작 지점이 활성이라 안전합니다.
        StartCoroutine(swarm.RunWaves());

        _swarmStarted = true;
        Debug.Log("[TutorialTrigger] EnemySwarmDirector 웨이브 시작");
    }


    IEnumerator RunTutorial(Character player)
    {
        // 1) 안내 준비: 무적/조작 잠금
        if (player != null && makePlayerInvincible) player.SetInvincible(true);
        if (locker && lockControlsDuringIntro) locker.LockControls(true);

        // 2) 상단 UI 안내
        if (ui) ui.Show(introTitle, introHint);

        // 3) 진행 대기(시간 또는 키)
        if (autoContinueAfter > 0f) yield return new WaitForSecondsRealtime(autoContinueAfter);
        else while (!Input.GetKeyDown(continueKey)) yield return null;

        // 4) 미션 안내 문구 변경
        if (ui) ui.Show(missionTitle, missionHint);

        // 5) 미션 시작
        if (killMission)
        {
            if (!killMission.gameObject.activeSelf) killMission.gameObject.SetActive(true);
            killMission.targetKills = 100; // 필요 시 수정
        }

        // 6) 컨트롤/무적 해제
        if (locker) locker.LockControls(false);
        if (player != null && makePlayerInvincible) player.SetInvincible(false);

        // 7) (필요 시) 다음 목적지 표식 다시 띄우고 싶으면 아래 주석 해제
        // var target = nextDestination ? nextDestination : transform;
        // WaypointDirector.Show(waypointUI, target, nextMessage);
    }
}

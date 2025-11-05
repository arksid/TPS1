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
    public bool clearOnEnter = true;            // 진입 시 마커+아웃라인 완전 정리

    bool _fired;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // 안정적 트리거를 위한 Kinematic Rigidbody 권장
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnEnable()
    {
        // 요구사항: 트리거가 "켜지면" 웨이포인트 UI만 끄고(아웃라인 유지)
        if (hideWaypointUIOnEnable)
            WaypointDirector.HideUIOnly();

        // 자동 표시는 금지(원하면 주석 해제해서 사용)
        // if (showOnEnable)
        // {
        //     var target = nextDestination ? nextDestination : transform;
        //     WaypointDirector.Show(waypointUI, target, nextMessage);
        // }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;

        // 트리거에 "들어오면" 기존 마커/아웃라인 완전 정리 (이중 안전)
        if (clearOnEnter)
            WaypointDirector.Clear();

        _fired = true;

        var player = other.GetComponentInParent<Character>() ?? other.GetComponent<Character>();
        StartCoroutine(RunTutorial(player));
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

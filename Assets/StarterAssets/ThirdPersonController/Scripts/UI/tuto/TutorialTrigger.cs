using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("연결(필요한 것만)")]
    public TutorialUI ui;                // 상단 안내(제목/힌트)
    public Kill100Mission killMission;   // 처치 미션(인스펙터 Target Kills 값 사용)
    public PlayerControlLocker locker;   // 조작 잠금/해제(선택)

    [Header("진행 옵션")]
    public bool makePlayerInvincible = true;    // 안내 중 무적
    public bool lockControlsDuringIntro = true; // 안내 중 조작잠금
    public KeyCode continueKey = KeyCode.E;     // 다음 단계 진행 키
    public float autoContinueAfter = 0f;        // 0이면 키 입력 대기

    [Header("안내 문구")]
    [TextArea] public string introTitle = "튜토리얼: 미션 안내";
    [TextArea] public string introHintFallback = "E키를 눌러 진행하세요.";
    [TextArea] public string missionTitle = "미션 시작!";
    [TextArea] public string missionHint = "좌상단 게이지를 확인하세요.";

    // 내부 상태
    bool _fired;

    void Reset()
    {
        // 트리거 안정화를 위해 Kinematic Rigidbody 권장
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;

        _fired = true;

        var player = other.GetComponentInParent<Character>() ?? other.GetComponent<Character>();
        StartCoroutine(RunTutorial(player));
    }

    IEnumerator RunTutorial(Character player)
    {
        // 1) 준비: 무적 & 조작 잠금
        if (player != null && makePlayerInvincible) player.SetInvincible(true);
        if (locker && lockControlsDuringIntro) locker.LockControls(true);

        // 2) 상단 UI (목표 수 동적 표시)
        string dynamicIntro = introHintFallback;
        if (killMission != null)
            dynamicIntro = $"{introHintFallback}\n이번 미션: 적 {killMission.targetKills}마리 처치";
        if (ui) ui.Show(introTitle, dynamicIntro);

        // 3) 진행 대기(시간 또는 키)
        if (autoContinueAfter > 0f) yield return new WaitForSecondsRealtime(autoContinueAfter);
        else while (!Input.GetKeyDown(continueKey)) yield return null;

        // 4) 미션 안내 표시
        if (ui) ui.Show(missionTitle, missionHint);

        // 5) 미션 시작(활성화만; 목표 수는 인스펙터 값 그대로)
        if (killMission && !killMission.gameObject.activeSelf)
            killMission.gameObject.SetActive(true);

        // 6) 해제: 조작 & 무적
        if (locker) locker.LockControls(false);
        if (player != null && makePlayerInvincible) player.SetInvincible(false);

        // 7) 1회성 트리거라면 자신 비활성화(선택)
        // gameObject.SetActive(false);
    }
}

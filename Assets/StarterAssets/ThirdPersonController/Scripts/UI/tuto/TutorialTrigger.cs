using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("연결")]
    public TutorialUI ui;
    public Kill100Mission killMission; // 미션 UI & 로직
    public PlayerControlLocker locker; // 조작 잠금/해제

    [Header("진행 옵션")]
    public bool makePlayerInvincible = true; // 안내 중 무적
    public bool lockControlsDuringIntro = true; // 안내 중 조작잠금
    public KeyCode continueKey = KeyCode.E; // 다음 단계 진행 키
    public float autoContinueAfter = 0f;    // 0이면 키 기다림

    [Header("안내 문구")]
    [TextArea] public string introTitle = "튜토리얼: 미션 안내";
    [TextArea] public string introHint = "E키를 눌러 진행하세요.\n이번 미션: 적 100마리 처치";

    [TextArea] public string missionTitle = "미션 시작!";
    [TextArea] public string missionHint = "화면 좌상단 게이지가 오르는 걸 보세요.";

    bool _fired;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;

        _fired = true;
        var player = other.GetComponentInParent<Character>();
        if (!player) player = other.GetComponent<Character>();

        StartCoroutine(RunTutorial(player));
    }

    IEnumerator RunTutorial(Character player)
    {
        // 1) 준비: 무적/조작 잠금
        if (player != null && makePlayerInvincible)
            player.SetInvincible(true); // 아웃라인 ON 포함 (Character.SetInvincible) :contentReference[oaicite:2]{index=2}

        if (locker && lockControlsDuringIntro)
            locker.LockControls(true); // ThirdPersonController/CharacterController 비활성 :contentReference[oaicite:3]{index=3}

        // 2) 안내 표시
        if (ui) ui.Show(introTitle, introHint);

        // 3) 진행 대기(키 또는 자동)
        if (autoContinueAfter > 0f)
            yield return new WaitForSecondsRealtime(autoContinueAfter);
        else
        {
            // 키 대기
            while (!Input.GetKeyDown(continueKey)) yield return null;
        }

        // 4) 미션 안내로 문구 변경
        if (ui) ui.Show(missionTitle, missionHint);

        // 5) 미션 UI/로직 시작(활성화만 해도 UpdateUI가 돌게됨)
        if (killMission)
        {
            // 비활성 상태였다면 활성화
            if (!killMission.gameObject.activeSelf)
                killMission.gameObject.SetActive(true);

            // 필요 시 카운터 초기화/목표 변경
            killMission.targetKills = 100;
            // 이미 Kill100Mission은 이벤트로 자동 카운트업
        }

        // 6) 컨트롤/무적 해제
        if (locker) locker.LockControls(false);
        if (player != null && makePlayerInvincible)
            player.SetInvincible(false); // 아웃라인 OFF 포함 :contentReference[oaicite:4]{index=4}
    }
}

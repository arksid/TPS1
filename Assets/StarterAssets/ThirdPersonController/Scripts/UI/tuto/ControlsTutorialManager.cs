using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 실제로 입력을 수행하면 다음 단계로 넘어가는 조작 튜토리얼 매니저.
/// TMP UI(ControlsTutorialUI)와 연결해서 제목/힌트/진행도를 표시합니다.
/// 입력 체크는 기본 Input Manager (Horizontal/Vertical, Mouse X/Y, Mouse0/1 등)를 사용합니다.
/// </summary>
public class ControlsTutorialManager : MonoBehaviour
{
    public enum ActionType
    {
        PressKey,   // 특정 키 누르기 (예: Tab 등)
        HoldKey,    // 특정 키를 일정 시간 유지
        Move,       // WASD로 이동
        Look,       // 마우스로 시점 움직이기
        Jump,       // Space
        Sprint,     // Shift + 이동(유지)
        Roll,       // Shift 더블탭(구르기)
        Aim,        // 우클릭(Mouse1)
        Fire,       // 좌클릭(Mouse0)
        Reload,     // R
        Interact,   // E
        Heal,       // F
        Ultimate    // Q
    }

    [Serializable]
    public class Step
    {
        public string title = "제목";
        [TextArea] public string hint = "힌트/설명";
        public ActionType action = ActionType.Move;

        [Header("키/시간 옵션(해당 타입일 때만)")]
        public KeyCode key = KeyCode.None;   // PressKey/HoldKey 등
        public float requiredSeconds = 0.5f; // Hold/Move/Look/Sprint 등 누적 조건
        public float moveThreshold = 0.4f;   // Move 감지 임계값
        public float lookThreshold = 10f;    // Look 감지 임계값(마우스 델타 절댓값 누적)
    }

    [Header("UI 연결")]
    public ControlsTutorialUI ui;

    [Header("튜토리얼 단계")]
    public List<Step> steps = new List<Step>();

    [Header("동작 옵션")]
    public bool autoStartOnPlay = true;
    public bool hideUIOnComplete = true;

    [Header("구르기(더블탭) 인식 옵션")]
    [Tooltip("Shift를 연속으로 누르는 최대 간격(초). 이 시간 안에 2번 누르면 구르기로 인식")]
    public float rollDoubleTapWindow = 0.3f;

    // === 튜토리얼 완료 후 이동 유도 옵션 ===
    [Header("튜토리얼 완료 후 이동 유도")]
    public SimpleWaypointUI waypointUI;           // 화면에 띄울 마커 UI
    public Transform tutorialTriggerTarget;       // 다음으로 유도할 '튜토리얼 트리거'의 Transform
    [TextArea] public string waypointMessage = "저기 표시된 곳으로 이동해!";
    public bool showWaypointOnComplete = true;    // 완료 직후 자동 표시할지

    // === 튜토리얼 완료 시 활성화할 오브젝트(트리거 등) ===
    [Header("튜토리얼 완료 시 활성화할 오브젝트")]
    [Tooltip("튜토리얼이 끝나면 SetActive(true)로 바꿀 오브젝트들(예: NextMissionTrigger 등)")]
    public GameObject[] activateOnComplete;

    int _index = -1;
    float _accum;     // 현재 단계 진행 누적값
    bool _running;

    // 더블탭 감지용
    float _lastShiftDownTime = -999f;

    void Start()
    {
        EnsureSteps();                 // ★ 빈 리스트면 기본 단계 채우기
        if (autoStartOnPlay) StartTutorial();
    }

    public void StartTutorial()
    {
        EnsureSteps();                 // ★ 혹시라도 비어있으면 한 번 더 보정
        _running = true;
        _index = -1;
        NextStep();
        if (ui) ui.Show();
    }

    void EnsureSteps()
    {
        if (steps == null || steps.Count == 0)
            FillDefaultSteps();        // 기본 단계 채우기(요청 순서 반영)
    }

    void EndTutorial()
    {
        _running = false;
        if (hideUIOnComplete && ui) ui.Hide();

        // 0) 참조 자동 보정 (혹시 인스펙터 비어 있으면 찾아서 연결)
        if (waypointUI == null)
        {
            waypointUI = FindFirstObjectByType<SimpleWaypointUI>(FindObjectsInactive.Include);
            if (waypointUI == null)
                Debug.LogWarning("[ControlsTutorial] waypointUI가 비었습니다 (Canvas에 SimpleWaypointUI 배치/연결 필요)");
        }
        if (tutorialTriggerTarget == null)
        {
            var nextTrig = FindFirstObjectByType<TutorialTrigger>(FindObjectsInactive.Include);
            tutorialTriggerTarget = nextTrig ? nextTrig.transform : null;
            if (tutorialTriggerTarget == null)
                Debug.LogWarning("[ControlsTutorial] tutorialTriggerTarget이 비었습니다 (미션 트리거 Transform 연결 필요)");
        }

        // 1) 완료 즉시 트리거(등) 활성화
        if (activateOnComplete != null)
        {
            foreach (var go in activateOnComplete)
                if (go) go.SetActive(true);
        }

        // 2) 튜토리얼 끝나야만 힌트(마커/아웃라인) 허용
        WaypointDirector.EnableHints();

        // 3) 다음 지역 웨이포인트 자동 표시(옵션)
        if (showWaypointOnComplete && waypointUI != null && tutorialTriggerTarget != null)
        {
            WaypointDirector.Show(waypointUI, tutorialTriggerTarget, waypointMessage);
            Debug.Log($"[ControlsTutorial] Waypoint Activate -> {tutorialTriggerTarget.name}");
        }
    }

    void NextStep()
    {
        _index++;
        _accum = 0f;
        if (_index >= steps.Count) { EndTutorial(); return; }

        var s = steps[_index];
        if (ui)
        {
            ui.SetText(s.title, s.hint);
            ui.SetProgress01(0f);
        }
    }

    void Update()
    {
        if (!_running || _index < 0 || _index >= steps.Count) return;

        var s = steps[_index];
        bool done = CheckStep(s, Time.deltaTime);
        float req = Mathf.Max(0.0001f, RequiredTimeFor(s));
        if (ui) ui.SetProgress01(Mathf.Clamp01(_accum / req));

        if (done) NextStep();
    }

    float RequiredTimeFor(Step s)
    {
        switch (s.action)
        {
            case ActionType.PressKey:
                return 1f; // 누르면 즉시 완료
            case ActionType.HoldKey:
            case ActionType.Move:
            case ActionType.Look:
            case ActionType.Sprint:
                return Mathf.Max(0.2f, s.requiredSeconds);
            case ActionType.Jump:
            case ActionType.Roll:
            case ActionType.Aim:
            case ActionType.Fire:
            case ActionType.Reload:
            case ActionType.Interact:
            case ActionType.Heal:
            case ActionType.Ultimate:
                return 1f; // 1회 동작
            default:
                return 1f;
        }
    }

    bool CheckStep(Step s, float dt)
    {
        switch (s.action)
        {
            case ActionType.PressKey:
                if (s.key != KeyCode.None && Input.GetKeyDown(s.key)) { _accum = 1f; return true; }
                return false;

            case ActionType.HoldKey:
                if (s.key != KeyCode.None && Input.GetKey(s.key))
                {
                    _accum += dt;
                    if (_accum >= s.requiredSeconds) return true;
                }
                else _accum = 0f;
                return false;

            case ActionType.Move:
                {
                    float dx = Input.GetAxis("Horizontal");
                    float dz = Input.GetAxis("Vertical");
                    float mag = new Vector2(dx, dz).magnitude;
                    if (mag >= s.moveThreshold)
                    {
                        _accum += dt;
                        if (_accum >= s.requiredSeconds) return true;
                    }
                    else _accum = 0f;
                    return false;
                }

            case ActionType.Look:
                {
                    float mx = Mathf.Abs(Input.GetAxis("Mouse X"));
                    float my = Mathf.Abs(Input.GetAxis("Mouse Y"));
                    float sum = (mx + my) * 100f; // 민감도 보정
                    _accum += sum;
                    if (_accum >= s.lookThreshold) return true;
                    return false;
                }

            case ActionType.Jump:
                if (Input.GetKeyDown(KeyCode.Space)) { _accum = 1f; return true; }
                return false;

            case ActionType.Sprint:
                {
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    float mag = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
                    if (shift && mag >= s.moveThreshold)
                    {
                        _accum += dt;
                        if (_accum >= s.requiredSeconds) return true;
                    }
                    else _accum = 0f;
                    return false;
                }

            case ActionType.Roll:
                {
                    // Shift 더블탭 감지: window 안에 2번 KeyDown
                    if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                    {
                        float now = Time.time;
                        if (now - _lastShiftDownTime <= rollDoubleTapWindow)
                        {
                            _accum = 1f;
                            return true; // 더블탭 성공
                        }
                        _lastShiftDownTime = now;
                    }
                    return false;
                }

            case ActionType.Aim:
                if (Input.GetMouseButton(1)) { _accum = 1f; return true; } // RMB
                return false;

            case ActionType.Fire:
                if (Input.GetMouseButtonDown(0)) { _accum = 1f; return true; } // LMB
                return false;

            case ActionType.Reload:
                if (Input.GetKeyDown(KeyCode.R)) { _accum = 1f; return true; }
                return false;

            case ActionType.Interact:
                if (Input.GetKeyDown(KeyCode.E)) { _accum = 1f; return true; }
                return false;

            case ActionType.Heal:
                if (Input.GetKeyDown(KeyCode.F)) { _accum = 1f; return true; }
                return false;

            case ActionType.Ultimate:
                if (Input.GetKeyDown(KeyCode.Q)) { _accum = 1f; return true; }
                return false;

            default:
                return false;
        }
    }

    void FillDefaultSteps()
    {
        // 요청 순서: 이동 → 시점 → 상호작용 → 점프 → 달리기 → 구르기 → 재장전 → 조준 → 사격 → 회복 → 궁극기
        steps = new List<Step>
        {
            new Step{
                title="이동 (WASD)",
                hint="WASD로 0.5초 이상 이동해보세요.",
                action=ActionType.Move, requiredSeconds=0.5f, moveThreshold=0.3f
            },
            new Step{
                title="시점 이동 (마우스)",
                hint="마우스를 움직여 주변을 둘러보세요.",
                action=ActionType.Look, lookThreshold=40f
            },
            new Step{
                title="상호작용 (E)",
                hint="바닥에 있는 총을 주우세요.",
                action=ActionType.Interact
            },
            new Step{
                title="점프 (Space)",
                hint="Space 키를 눌러 점프하세요.",
                action=ActionType.Jump
            },
            new Step{
                title="달리기 (Shift 유지)",
                hint="Shift를 누른 상태로 이동하세요(0.5초 유지).",
                action=ActionType.Sprint, requiredSeconds=0.5f, moveThreshold=0.3f
            },
            new Step{
                title="구르기 (Shift 더블탭)",
                hint=$"Shift를 {rollDoubleTapWindow:0.0}초 이내에 연속으로 두 번 눌러 구르세요.\n" +
                     "TIP) 구르는 동안엔 잠깐 **무적**입니다!",
                action=ActionType.Roll
            },
            new Step{
                title="재장전 (R)",
                hint="R 키를 눌러 탄약을 보충하세요.",
                action=ActionType.Reload
            },
            new Step{
                title="조준 (우클릭)",
                hint="우클릭으로 조준 자세에 들어갑니다.",
                action=ActionType.Aim
            },
            new Step{
                title="사격 (좌클릭)",
                hint="좌클릭으로 발사하세요.",
                action=ActionType.Fire
            },
            new Step{
                title="회복 (F)",
                hint="F 키로 회복 아이템을 사용하세요.",
                action=ActionType.Heal
            },
            new Step{
                title="궁극기 (Q)",
                hint="Q 키로 궁극기를 발동해 보세요.\n" +
                     "TIP) 궁극기 동안에는 **총알이 무제한**입니다!",
                action=ActionType.Ultimate
            },
        };
    }
}

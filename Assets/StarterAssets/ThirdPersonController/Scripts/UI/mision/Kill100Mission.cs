using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class Kill100Mission : MonoBehaviour
{
    [Header("목표")]
    [Min(1)] public int targetKills = 50;   // ✅ 기본값 50

    [Header("UI (비워두면 옵션)")]
    public Slider progressSlider;           // 진행도 슬라이더
    public TextMeshProUGUI mainLabel;       // "처치: X/Y"
    public TextMeshProUGUI subLabel;        // "미션: 적 Y마리 처치" / 성공 문구
    [Tooltip("성공 시 숨기고 싶은 UI 루트(패널 오브젝트만 지정 권장)")]
    public GameObject uiRootToHideOnSuccess;

    // ★ 추가: 미션 시작 시 UI를 강제로 보이게 할 대상(패널 루트)
    [Header("미션 시작 시 UI 강제 표시")]
    [Tooltip("예: Canvas/QuestUI/KillMissionPanel (미션 시작 시 SetActive(true)로 켭니다)")]
    public GameObject uiRootToShowOnStart;
    [Tooltip("있으면 알파/상호작용까지 켜줍니다(없으면 자동으로 CanvasGroup을 붙여서 씀)")]
    public CanvasGroup uiCanvasGroupOnStart;

    [Header("다음 미션 유도(웨이포인트)")]
    public bool showNextWaypointOnSuccess = true;
    public SimpleWaypointUI waypointUI;     // 화면에 띄울 마커 UI
    public Transform nextMissionTarget;     // 다음 거점(트리거) 위치
    public string nextMissionLabel = "다음 거점으로 이동";
    public GameObject nextOutlineTarget;    // 아웃라인 걸 대상(비우면 nextMissionTarget)

    [Header("성공 시 활성화할 오브젝트(트리거/에리어 등)")]
    [Tooltip("미션 성공 순간 SetActive(true)로 만들 오브젝트들(예: HoldZoneTrigger, HoldZoneArea 루트 등)")]
    public GameObject[] activateOnSuccess;

    [Header("이벤트(선택)")]
    public UnityEvent onMissionStart;
    public UnityEvent onMissionSuccess;

    int currentKills;
    bool isCompleted;

    // ─────────────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        // ★ 미션 시작 순간 UI 패널을 '확실히' 켠다
        ForceShowUIAtStart();

        MissionEvents.OnEnemyKilled += HandleEnemyKilled;

        if (progressSlider)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = Mathf.Max(1, targetKills);
            progressSlider.value = 0;
        }

        UpdateUI(initialize: true);
        onMissionStart?.Invoke();
    }

    void OnDisable()
    {
        MissionEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    void HandleEnemyKilled()
    {
        if (isCompleted) return;

        currentKills++;
        if (currentKills >= targetKills)
        {
            currentKills = targetKills;
            isCompleted = true;
            UpdateUI();

            OnMissionSuccess();
        }
        else
        {
            UpdateUI();
        }
    }

    void UpdateUI(bool initialize = false)
    {
        // 메인 라벨: "처치: X/Y"
        if (mainLabel)
            mainLabel.text = $"처치: {currentKills}/{targetKills}";

        // 서브 라벨: 진행 중 / 성공
        if (subLabel)
        {
            if (!isCompleted)
                subLabel.text = $"미션: 적 {targetKills}마리 처치";
            else
                subLabel.text = $"✅ 미션 성공! (적 {targetKills}마리 처치)";
        }

        if (progressSlider)
            progressSlider.value = currentKills;

        if (initialize) return;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    void OnMissionSuccess()
    {
        // 0) 다음 단계 필요한 트리거/에리어 활성화
        if (activateOnSuccess != null && activateOnSuccess.Length > 0)
        {
            foreach (var go in activateOnSuccess)
            {
                if (!go) continue;
                if (!go.activeSelf) go.SetActive(true);
            }
        }

        // 1) 다음 웨이포인트 + 아웃라인 표시
        if (showNextWaypointOnSuccess && waypointUI && nextMissionTarget)
        {
            WaypointDirector.Clear();
            WaypointDirector.Show(waypointUI, nextMissionTarget, nextMissionLabel);

            var outlineGO = nextOutlineTarget ? nextOutlineTarget : nextMissionTarget.gameObject;
            EnsureOutlineNow(outlineGO);
        }

        // 2) 성공 시 UI 패널 숨김(선택)
        if (uiRootToHideOnSuccess)
            uiRootToHideOnSuccess.SetActive(false);

        // 3) 외부 이벤트
        onMissionSuccess?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 시작 시 UI를 '무조건' 보이게 만든다 (부모가 꺼져 있어도 끌어올림)

    void ForceShowUIAtStart()
    {
        if (uiRootToShowOnStart)
        {
            if (!uiRootToShowOnStart.activeSelf) uiRootToShowOnStart.SetActive(true);

            var cg = uiCanvasGroupOnStart ? uiCanvasGroupOnStart
                   : uiRootToShowOnStart.GetComponent<CanvasGroup>();
            if (!cg) cg = uiRootToShowOnStart.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // 상위 계층에 비활성 부모가 있으면 몇 단계까지는 깨워줌
            var t = uiRootToShowOnStart.transform.parent;
            for (int i = 0; i < 4 && t != null; i++)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
        }
        else
        {
            // 패널을 직접 지정 안 했을 경우: 슬라이더/라벨 기준으로 부모 패널을 깨워줌
            TryWakeUpByComponent(progressSlider);
            TryWakeUpByComponent(mainLabel);
            TryWakeUpByComponent(subLabel);
        }
    }

    void TryWakeUpByComponent(Component c)
    {
        if (!c) return;

        // CanvasGroup/Canvas를 찾아서 보이도록
        var cg = c.GetComponentInParent<CanvasGroup>(true);
        if (cg)
        {
            if (!cg.gameObject.activeSelf) cg.gameObject.SetActive(true);
            cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
        }

        var canvas = c.GetComponentInParent<Canvas>(true);
        if (canvas && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);

        // 최상위 몇 단계의 비활성 부모도 깨우기
        var t = c.transform;
        for (int i = 0; i < 4 && t != null; i++)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 아웃라인 보강: "바로" 켭니다.

    void EnsureOutlineNow(GameObject go)
    {
        if (!go) return;

        OutlineHelper.SetOutline(go, true);

        if (HasEnabledOutline(go)) return;

        var qo = GetOrAddBehaviour(go, "QuickOutline");
        if (qo == null) qo = GetOrAddBehaviour(go, "Outline");
        if (qo != null) qo.enabled = true;
    }

    bool HasEnabledOutline(GameObject go)
    {
        var comps = go.GetComponents<Behaviour>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            var n = c.GetType().Name;
            if ((n == "QuickOutline" || n == "Outline") && c.enabled) return true;
        }
        return false;
    }

    Behaviour GetOrAddBehaviour(GameObject go, string typeName)
    {
        var t = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => {
                try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(x => x != null && x.Name == typeName && typeof(Behaviour).IsAssignableFrom(x));
        if (t == null) return null;

        var exist = go.GetComponent(t) as Behaviour;
        if (exist != null) return exist;

        return go.AddComponent(t) as Behaviour;
    }

#if UNITY_EDITOR
    [ContextMenu("테스트: +10킬")]
    void Context_Add10() { for (int i = 0; i < 10; i++) MissionEvents.RaiseEnemyKilled(); }

    [ContextMenu("테스트: 즉시 완료")]
    void Context_CompleteNow()
    {
        currentKills = targetKills;
        isCompleted = true;
        UpdateUI();
        OnMissionSuccess();
    }
#endif
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Kill100Mission : MonoBehaviour
{
    [Header("목표")]
    [Min(1)] public int targetKills = 100;

    [Header("UI (비워두면 자동 생성)")]
    public Slider progressSlider;
    public TextMeshProUGUI mainLabel; // "처치: 0/100"
    public TextMeshProUGUI subLabel;  // "미션: 적 100마리 처치"
    [Tooltip("성공 시 숨기고 싶은 UI 루트(캔버스 전체 말고, '패널' 오브젝트만 지정 권장)")]
    public GameObject uiRootToHideOnSuccess;

    [Header("미션 성공 시 호출(선택)")]
    public MonoBehaviour waveController;      // 예: EnemySwarmDirector
    public string endWaveMethodName = "EndWave";

    [Header("미션2(홀드존) 연동")]
    public SimpleWaypointUI waypointUI;       // 웨이포인트 UI
    public Transform nextMissionTrigger;      // HoldZoneTrigger 오브젝트(Transform)
    [TextArea] public string nextMessage = "다음 거점으로 이동!";
    public bool activateNextTriggerOnSuccess = true;  // 성공 시 트리거 SetActive(true)
    public bool showWaypointOnSuccess = true;         // 성공 시 웨이포인트 표시

    [Header("튜토리얼 UI 연동")]
    public TutorialUI tutorialUIToHide;       // 미션1 성공 시 끌 튜토리얼 UI

    [Header("상태(읽기전용)")]
    public int currentKills = 0;
    public bool isCompleted = false;

    [Header("디버그/테스트")]
    public bool completeImmediatelyOnStart = false;   // 시작하자마자 클리어(테스트용)

    // 내부
    GameObject _autoUiPanel;   // 자동 생성된 '패널'만 기억해서 끈다(캔버스는 건드리지 않음)

    void Awake()
    {
        EnsureUI();
        UpdateUI();

        if (completeImmediatelyOnStart)
        {
            currentKills = targetKills;
            isCompleted = true;
            UpdateUI();
            OnMissionSuccess(); // 여기서도 안전 (코루틴은 GameFlowRunner가 돌림)
        }
    }

    void OnEnable() => MissionEvents.OnEnemyKilled += HandleEnemyKilled;
    void OnDisable() => MissionEvents.OnEnemyKilled -= HandleEnemyKilled;

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

    void OnMissionSuccess()
    {
        if (subLabel) subLabel.text = "✅ 미션 성공! (적 100마리 처치)";

        // (선택) 웨이브 종료 훅 (없어도 됨)
        TryEndWave();

        // 1) 내 미션1 UI 패널만 끄기 (캔버스/웨이포인트UI는 유지!)
        TryHideOwnUI();

        // 2) 튜토리얼 UI 끄기
        if (tutorialUIToHide) tutorialUIToHide.Hide();

        // 3) 다음 트리거 켜기
        if (activateNextTriggerOnSuccess && nextMissionTrigger)
            nextMissionTrigger.gameObject.SetActive(true);

        // 4) 다음 웨이포인트 표시 (한 프레임 뒤, 항상 활성 보장)
        if (showWaypointOnSuccess && waypointUI && nextMissionTrigger)
            GameFlowRunner.Run(Co_ShowNextMissionWaypointSafely());
    }

    IEnumerator Co_ShowNextMissionWaypointSafely()
    {
        // 트리거 OnEnable 초기화/숨김 등이 먼저 끝나도록 한 프레임 대기
        yield return null;

        WaypointDirector.EnableHints();

        // 표시 직전, UI/캔버스 활성 보장
        if (!waypointUI.gameObject.activeSelf)
            waypointUI.gameObject.SetActive(true);
        if (waypointUI.canvas && !waypointUI.canvas.gameObject.activeSelf)
            waypointUI.canvas.gameObject.SetActive(true);

        WaypointDirector.Show(waypointUI, nextMissionTrigger, nextMessage);

        // 필요하면 1~2프레임 여유로 재표시
        yield return null;
        WaypointDirector.Show(waypointUI, nextMissionTrigger, nextMessage);
    }

    void TryEndWave()
    {
        if (waveController == null || string.IsNullOrEmpty(endWaveMethodName)) return;

        var m = waveController.GetType().GetMethod(
            endWaveMethodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
        );
        if (m != null) m.Invoke(waveController, null);
        else Debug.LogWarning($"[Kill100Mission] {waveController.GetType().Name}에 {endWaveMethodName} 없음");
    }

    void TryHideOwnUI()
    {
        // 1순위: 사용자 지정 루트만 끈다 (절대 캔버스 전체 넣지 말 것)
        if (uiRootToHideOnSuccess != null)
        {
            uiRootToHideOnSuccess.SetActive(false);
            return;
        }

        // 2순위: 자동 생성 UI의 '패널'만 끈다 (캔버스는 계속 ON)
        if (_autoUiPanel != null)
        {
            _autoUiPanel.SetActive(false);
            return;
        }
    }

    void UpdateUI()
    {
        if (progressSlider)
        {
            float ratio = targetKills <= 0 ? 0f : (float)currentKills / targetKills;
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = ratio;
        }

        if (mainLabel) mainLabel.text = $"처치: {currentKills}/{targetKills}";
        if (subLabel && !isCompleted) subLabel.text = "미션: 적 100마리 처치";
    }

    void EnsureUI()
    {
        if (progressSlider && mainLabel && subLabel) return;

        var canvasGO = new GameObject("Kill100MissionUI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panel = panelGO.AddComponent<Image>();
        panel.color = new Color(0, 0, 0, 0.35f);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.02f, 0.88f);
        pr.anchorMax = new Vector2(0.40f, 0.98f);
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;

        // Slider
        var sliderGO = new GameObject("ProgressSlider");
        sliderGO.transform.SetParent(panelGO.transform, false);
        progressSlider = sliderGO.AddComponent<Slider>();

        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.15f);
        var bgRT = bgImg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.05f, 0.25f);
        bgRT.anchorMax = new Vector2(0.95f, 0.65f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0.05f, 0.25f);
        faRT.anchorMax = new Vector2(0.95f, 0.65f);
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 1f, 0.85f);
        var fr = fillImg.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;

        progressSlider.fillRect = fillImg.rectTransform;
        progressSlider.targetGraphic = fillImg;
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        progressSlider.value = 0f;

        var mainGO = new GameObject("MainLabel");
        mainGO.transform.SetParent(panelGO.transform, false);
        mainLabel = mainGO.AddComponent<TextMeshProUGUI>();
        mainLabel.alignment = TextAlignmentOptions.Left;
        mainLabel.fontSize = 24;
        var mr = mainLabel.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(0.05f, 0.65f);
        mr.anchorMax = new Vector2(0.95f, 0.95f);
        mr.offsetMin = Vector2.zero; mr.offsetMax = Vector2.zero;

        var subGO = new GameObject("SubLabel");
        subGO.transform.SetParent(panelGO.transform, false);
        subLabel = subGO.AddComponent<TextMeshProUGUI>();
        subLabel.alignment = TextAlignmentOptions.Left;
        subLabel.fontSize = 18;
        var sr = subLabel.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.05f, 0.05f);
        sr.anchorMax = new Vector2(0.95f, 0.35f);
        sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;

        // 자동 생성된 '패널'만 기억 (캔버스는 끄지 않음)
        _autoUiPanel = panelGO;
    }

#if UNITY_EDITOR
    [ContextMenu("테스트: +10킬")]
    void Context_Add10Kills()
    {
        for (int i = 0; i < 10; i++) MissionEvents.RaiseEnemyKilled();
    }

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

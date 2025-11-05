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
    [Tooltip("성공 시 숨기고 싶은 UI 루트(없으면 자동 생성된 캔버스를 끔)")]
    public GameObject uiRootToHideOnSuccess;

    [Header("미션 성공 시 호출(선택)")]
    public MonoBehaviour waveController;      // 예: AdvancedEnemyWaveSpawner
    public string endWaveMethodName = "EndWave";

    [Header("미션2(홀드존) 연동")]
    public SimpleWaypointUI waypointUI;       // 웨이포인트 UI
    public Transform nextMissionTrigger;      // HoldZoneTrigger 오브젝트(Transform)
    [TextArea] public string nextMessage = "다음 거점으로 이동!";
    public bool activateNextTriggerOnSuccess = true;  // 성공 시 트리거 SetActive(true)
    public bool showWaypointOnSuccess = true;         // 성공 시 웨이포인트 표시

    [Header("상태(읽기전용)")]
    public int currentKills = 0;
    public bool isCompleted = false;

    [Header("디버그/테스트")]
    public bool completeImmediatelyOnStart = false;   // 시작하자마자 클리어(테스트용)
    bool _deferShowNextWaypoint;   // 비활성/Awake 시점에 웨이포인트 표시를 지연
    GameObject _autoUiRoot; // 자동 생성한 '패널'을 기억했다가 이것만 끈다
    void Awake()
    {
        EnsureUI();
        UpdateUI();

        if (completeImmediatelyOnStart)
        {
            currentKills = targetKills;
            isCompleted = true;
            UpdateUI();
            OnMissionSuccess();
        }
    }
    void Start()
    {
        if (_deferShowNextWaypoint)
        {
            _deferShowNextWaypoint = false;
            GlobalCoroutineRunner.Run(Co_ShowNextMissionWaypointSafely());
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
        TryEndWave();
        TryHideOwnUI();

        if (activateNextTriggerOnSuccess && nextMissionTrigger)
            nextMissionTrigger.gameObject.SetActive(true);

        if (showWaypointOnSuccess)
        {
            // 이 스크립트가 활성이라면 여기서, 아니라면 글로벌 러너로
            var routine = Co_ShowNextMissionWaypointSafely();

            if (isActiveAndEnabled) StartCoroutine(routine);
            else
            {
                _deferShowNextWaypoint = true;        // Start()에서 다시 실행
                GlobalCoroutineRunner.Run(routine);   // 즉시 전역에서 실행(둘 중 하나는 반드시 활성 상태)
            }
        }
    }



    System.Collections.IEnumerator Co_ShowNextMissionWaypointSafely()
    {
        // 트리거 OnEnable(초기화/숨김)이 먼저 끝나도록 1프레임 대기
        yield return null;

        // 혹시 한 프레임 더 필요할 수 있어서 최대 5프레임까지 재시도
        const int maxTries = 5;

        // 힌트 허용(안전)
        WaypointDirector.EnableHints();

        if (waypointUI)
        {
            // SimpleWaypointUI 자신
            if (!waypointUI.gameObject.activeSelf)
                waypointUI.gameObject.SetActive(true);

            // 연결된 캔버스도 켜주기 (캔버스가 꺼져 있으면 자식이 켜져도 안 보임)
            if (waypointUI.canvas && !waypointUI.canvas.gameObject.activeSelf)
                waypointUI.canvas.gameObject.SetActive(true);
        }

        // 이제 확실히 표기
        WaypointDirector.Show(waypointUI, nextMissionTrigger, nextMessage);

        for (int i = 0; i < maxTries; i++)
        {
            if (waypointUI != null && nextMissionTrigger != null)
            {
                WaypointDirector.Show(waypointUI, nextMissionTrigger, nextMessage);
                Debug.Log($"[Kill100Mission] 다음 미션 웨이포인트 표시 시도: {i + 1}");
                yield return null; // 만약 OnEnable 쪽에서 또 건드리면 다음 루프에서 다시 표시
            }
            else
            {
                Debug.LogWarning("[Kill100Mission] waypointUI 또는 nextMissionTrigger 미지정");
                yield break;
            }
        }
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
        // 1순위: 사용자가 지정한 루트만 끈다 (캔버스 전체를 넣지 마세요)
        if (uiRootToHideOnSuccess != null)
        {
            uiRootToHideOnSuccess.SetActive(false);
            return;
        }

        // 2순위: 자동 생성 UI의 '패널'만 끈다 (캔버스는 그대로)
        if (_autoUiRoot != null)
        {
            _autoUiRoot.SetActive(false);
            return;
        }

        // ★ 기존처럼 progressSlider의 최상위 부모(root)까지 타고 올라가서 끄지 마세요.
        // (웨이포인트 UI가 같은 캔버스에 있으면 같이 꺼져버립니다)
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
        _autoUiRoot = panelGO;
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

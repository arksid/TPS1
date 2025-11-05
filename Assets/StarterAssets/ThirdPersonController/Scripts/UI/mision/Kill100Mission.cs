using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★ TMP 전용

public class Kill100Mission : MonoBehaviour
{
    [Header("목표")]
    [Min(1)] public int targetKills = 100;

    [Header("UI (비워두면 자동 생성)")]
    public Slider progressSlider;
    public TextMeshProUGUI mainLabel; // "처치: 0/100"
    public TextMeshProUGUI subLabel;  // "미션: 적 100마리 처치"

    [Header("미션 성공 시 호출(선택)")]
    public MonoBehaviour waveController;      // 예: AdvancedEnemyWaveSpawner
    public string endWaveMethodName = "EndWave";

    [Header("상태(읽기전용)")]
    public int currentKills = 0;
    public bool isCompleted = false;

    void Awake()
    {
        EnsureUI();
        UpdateUI();
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
        Debug.Log("[Kill100Mission] 성공");
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
        // 이미 수동으로 연결해 두었다면 자동 생성 안 함
        if (progressSlider && mainLabel && subLabel) return;

        // Canvas 생성
        var canvasGO = new GameObject("Kill100MissionUI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 상단 패널
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

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.15f);
        var bgRT = bgImg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.05f, 0.25f);
        bgRT.anchorMax = new Vector2(0.95f, 0.65f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0.05f, 0.25f);
        faRT.anchorMax = new Vector2(0.95f, 0.65f);
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

        // Fill
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

        // Main Label
        var mainGO = new GameObject("MainLabel");
        mainGO.transform.SetParent(panelGO.transform, false);
        mainLabel = mainGO.AddComponent<TextMeshProUGUI>();
        mainLabel.alignment = TextAlignmentOptions.Left;
        mainLabel.fontSize = 24;
        var mr = mainLabel.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(0.05f, 0.65f);
        mr.anchorMax = new Vector2(0.95f, 0.95f);
        mr.offsetMin = Vector2.zero; mr.offsetMax = Vector2.zero;

        // Sub Label
        var subGO = new GameObject("SubLabel");
        subGO.transform.SetParent(panelGO.transform, false);
        subLabel = subGO.AddComponent<TextMeshProUGUI>();
        subLabel.alignment = TextAlignmentOptions.Left;
        subLabel.fontSize = 18;
        var sr = subLabel.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.05f, 0.05f);
        sr.anchorMax = new Vector2(0.95f, 0.35f);
        sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;
    }

#if UNITY_EDITOR
    [ContextMenu("테스트: +10킬")]
    void Context_Add10Kills()
    {
        for (int i = 0; i < 10; i++) MissionEvents.RaiseEnemyKilled();
    }
#endif
}

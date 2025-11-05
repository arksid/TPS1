// Assets/Scripts/System/SceneLoader.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Loading UI")]
    public LoadingScreen loadingScreenPrefab;                 // (선택) 인스펙터에 프리팹 연결
    [SerializeField] private string resourcesPath = "UI/LoadingScreen"; // Resources/UI/LoadingScreen.prefab

    [Header("Timing")]
    [Tooltip("로딩 화면 최소 표시 시간(초)")]
    public float minDisplaySeconds = 1.2f;                    // ▶ 숫자 올리면 오래 보임
    [Tooltip("최소 표시 후 추가로 더 보여줄 시간(초)")]
    public float extraHoldSeconds = 0.5f;                     // ▶ 보너스 대기

    [Header("Progress Smoothing")]
    [Tooltip("진행바가 부드럽게 차는 속도(값이 클수록 빨리 따라감)")]
    public float visualLerpSpeed = 1.6f;                      // ▶ 1.2~2.0 권장

    [Header("Fade")]
    [Tooltip("로딩 시작 시 페이드 인 시간(초)")]
    public float fadeInSeconds = 0.25f;
    [Tooltip("씬 전환 직후 페이드 아웃 시간(초)")]
    public float fadeOutSeconds = 0.25f;

    private LoadingScreen _ui;
    private float _shownAt;
    private float _visualProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadSceneAsync(string sceneName) => StartCoroutine(CoLoadByName(sceneName));
    public void LoadSceneAsync(int buildIndex) => StartCoroutine(CoLoadByIndex(buildIndex));

    IEnumerator CoLoadByName(string sceneName)
    {
        ShowLoadingUI();                               // UI 띄움(+필요하면 자동 생성)
        _shownAt = Time.unscaledTime;
        _visualProgress = 0f;

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 페이드 인
        yield return FadeCanvas(_uiCanvasGroup, 0f, 1f, fadeInSeconds);

        // 진행 업데이트(부드럽게)
        while (op.progress < 0.9f)
        {
            float target = op.progress / 0.9f;        // 0~1 보정
            _visualProgress = Mathf.MoveTowards(_visualProgress, target, Time.unscaledDeltaTime * visualLerpSpeed);
            UpdateLoadingUI(_visualProgress);
            yield return null;
        }

        // 100%까지 부드럽게 채우기
        while (_visualProgress < 1f)
        {
            _visualProgress = Mathf.MoveTowards(_visualProgress, 1f, Time.unscaledDeltaTime * visualLerpSpeed);
            UpdateLoadingUI(_visualProgress);
            yield return null;
        }

        // 최소 표시 + 추가 대기 보장
        float remain = minDisplaySeconds - (Time.unscaledTime - _shownAt);
        if (remain < 0f) remain = 0f;
        yield return new WaitForSecondsRealtime(remain + extraHoldSeconds);

        op.allowSceneActivation = true;               // 실제 씬 활성화
        yield return null;                             // 다음 프레임까지 대기

        // 페이드 아웃 후 UI 제거
        yield return FadeCanvas(_uiCanvasGroup, 1f, 0f, fadeOutSeconds);
        HideLoadingUI();
    }

    IEnumerator CoLoadByIndex(int buildIndex)
    {
        ShowLoadingUI();
        _shownAt = Time.unscaledTime;
        _visualProgress = 0f;

        var op = SceneManager.LoadSceneAsync(buildIndex);
        op.allowSceneActivation = false;

        yield return FadeCanvas(_uiCanvasGroup, 0f, 1f, fadeInSeconds);

        while (op.progress < 0.9f)
        {
            float target = op.progress / 0.9f;
            _visualProgress = Mathf.MoveTowards(_visualProgress, target, Time.unscaledDeltaTime * visualLerpSpeed);
            UpdateLoadingUI(_visualProgress);
            yield return null;
        }

        while (_visualProgress < 1f)
        {
            _visualProgress = Mathf.MoveTowards(_visualProgress, 1f, Time.unscaledDeltaTime * visualLerpSpeed);
            UpdateLoadingUI(_visualProgress);
            yield return null;
        }

        float remain = minDisplaySeconds - (Time.unscaledTime - _shownAt);
        if (remain < 0f) remain = 0f;
        yield return new WaitForSecondsRealtime(remain + extraHoldSeconds);

        op.allowSceneActivation = true;
        yield return null;

        yield return FadeCanvas(_uiCanvasGroup, 1f, 0f, fadeOutSeconds);
        HideLoadingUI();
    }

    // ---------------- UI 생성/업데이트/삭제 ----------------

    private CanvasGroup _uiCanvasGroup;

    private void ShowLoadingUI()
    {
        if (_ui != null) return;

        // 1) 인스펙터 프리팹
        if (loadingScreenPrefab != null)
        {
            _ui = Instantiate(loadingScreenPrefab);
            SetupUICommon(_ui.gameObject);
            return;
        }

        // 2) Resources 폴백
        var res = Resources.Load<LoadingScreen>(resourcesPath);
        if (res != null)
        {
            _ui = Instantiate(res);
            SetupUICommon(_ui.gameObject);
            return;
        }

        // 3) 최종 폴백: 즉석 UI 생성(슬라이더 + 텍스트)
        var go = new GameObject("LoadingScreen(Fallback)");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // 슬라이더
        var sliderGO = new GameObject("Bar");
        sliderGO.transform.SetParent(go.transform, false);
        var slider = sliderGO.AddComponent<Slider>();
        var rt = sliderGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.48f);
        rt.anchorMax = new Vector2(0.9f, 0.52f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // 텍스트
        var textGO = new GameObject("Percent");
        textGO.transform.SetParent(go.transform, false);
        var rt2 = textGO.AddComponent<RectTransform>();
        rt2.anchorMin = new Vector2(0.5f, 0.6f);
        rt2.anchorMax = new Vector2(0.5f, 0.6f);
        rt2.sizeDelta = new Vector2(200, 60);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 38f;
        tmp.text = "0%";

        var ls = go.AddComponent<LoadingScreen>();
        ls.progressBar = slider;
        ls.percentText = tmp;
        ls.canvasGroup = cg;

        _ui = ls;
        SetupUICommon(go);
    }

    private void SetupUICommon(GameObject go)
    {
        DontDestroyOnLoad(go);
        var canvas = go.GetComponentInChildren<Canvas>();
        if (canvas != null) canvas.sortingOrder = 5000;

        _uiCanvasGroup = _ui.canvasGroup != null ? _ui.canvasGroup : go.GetComponent<CanvasGroup>();
        if (_uiCanvasGroup == null) _uiCanvasGroup = go.AddComponent<CanvasGroup>();

        go.SetActive(true);
        UpdateLoadingUI(0f);
    }

    private void UpdateLoadingUI(float t)
    {
        if (_ui != null) _ui.SetProgress(t);
    }

    private void HideLoadingUI()
    {
        if (_ui != null)
        {
            Destroy(_ui.gameObject);
            _ui = null;
            _uiCanvasGroup = null;
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float seconds)
    {
        if (cg == null || seconds <= 0f)
        {
            if (cg != null) cg.alpha = to;
            yield break;
        }

        cg.alpha = from;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        cg.alpha = to;
    }
}

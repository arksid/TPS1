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
    [Tooltip("로딩 UI 최소 표시 시간(초)")]
    public float minDisplaySeconds = 0.75f;
    [Tooltip("씬 전환 직후 추가로 잠깐 붙잡는 시간(초)")]
    public float extraHoldSeconds = 0.15f;

    [Header("Fade")]
    [Tooltip("로딩 UI 등장 페이드 시간(초)")]
    public float fadeInSeconds = 0.2f;
    [Tooltip("로딩 진행바 수치 보간 속도")]
    public float visualLerpSpeed = 2.0f;
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

        // 최소 노출 시간 보장
        float remain = minDisplaySeconds - (Time.unscaledTime - _shownAt);
        if (remain < 0f) remain = 0f;
        yield return new WaitForSecondsRealtime(remain + extraHoldSeconds);

        // 씬 활성화
        op.allowSceneActivation = true;
        yield return null;

        // 페이드 아웃 및 UI 정리
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

    void ShowLoadingUI()
    {
        if (_ui != null) return;

        // 우선순위 1: 인스펙터 프리팹, 2: Resources 경로
        LoadingScreen prefab = loadingScreenPrefab;
        if (prefab == null)
            prefab = Resources.Load<LoadingScreen>(resourcesPath);

        if (prefab != null)
        {
            _ui = Instantiate(prefab);
            SetupUICommon(_ui.gameObject);
        }
        else
        {
            // 최소한의 임시 UI 생성(슬라이더+퍼센트)
            var go = new GameObject("LoadingScreen (Auto)");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
    }

    void SetupUICommon(GameObject go)
    {
        _uiCanvasGroup = go.GetComponent<CanvasGroup>();
        if (_uiCanvasGroup == null) _uiCanvasGroup = go.AddComponent<CanvasGroup>();
        _uiCanvasGroup.alpha = 0f;

        var canvas = go.GetComponent<Canvas>();
        if (canvas == null) canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<GraphicRaycaster>();
        go.transform.SetParent(null, false);
        DontDestroyOnLoad(go);
    }

    void UpdateLoadingUI(float t)
    {
        if (_ui == null) return;
        _ui.SetProgress(t);
    }

    void HideLoadingUI()
    {
        if (_ui == null) return;
        if (_ui.canvasGroup != null) _ui.canvasGroup.alpha = 0f;
        Destroy(_ui.gameObject);
        _ui = null;
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float seconds)
    {
        if (cg == null)
        {
            yield return null;
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

    // ===================== [ Post-Load Cutscene ] =====================
    [Header("Post-Load Cutscene")]
    public PrologueSequence prologuePrefab;                 // (선택) 인스펙터에서 프리팹 직접 연결
    [SerializeField] private string prologueResourcePath = "UI/PrologueSequence"; // Resources 경로 폴백

    /// <summary>
    /// 첫 씬을 로드 → 프롤로그를 재생 → 다음 씬을 로드하는 일괄 실행 메서드
    /// </summary>
    /// <param name="firstScene">먼저 보여줄 씬 이름(로딩 UI 표시)</param>
    /// <param name="nextScene">프롤로그 후에 넘어갈 다음 씬 이름</param>
    public void LoadSceneThenPrologueThenNext(string firstScene, string nextScene)
    {
        StartCoroutine(CoLoadThenPrologueThenNext(firstScene, nextScene));
    }

    private IEnumerator CoLoadThenPrologueThenNext(string firstScene, string nextScene)
    {
        // 1) 첫 씬 로딩 (기존 로딩 UI 로직 재사용)
        yield return CoLoadByName(firstScene);   // 로딩 → 활성화 → 페이드아웃까지 완료
        yield return null;                       // 다음 프레임 안전 대기

        // 2) 프롤로그 프리팹 인스턴스(인스펙터 우선, 없으면 Resources 폴백)
        PrologueSequence prefab = prologuePrefab;
        if (prefab == null)
            prefab = Resources.Load<PrologueSequence>(prologueResourcePath);

        if (prefab != null)
        {
            var seq = Instantiate(prefab);
            seq.playOnSceneStart = false;        // 수동으로 Play
            // 프롤로그 실행 & 완료까지 대기
            yield return seq.PlayAndWait();
            if (seq) Destroy(seq.gameObject);
        }
        // (prefab이 없어도 에러 없이 그냥 건너뜀)

        // 3) 다음 스테이지로 로딩
        if (!string.IsNullOrEmpty(nextScene))
            yield return CoLoadByName(nextScene);
    }
}

// Assets/Scripts/UI/PrologueSequence.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(1000)] // 씬 로딩 직후에 가장 늦게 뜨도록
public class PrologueSequence : MonoBehaviour
{
    [System.Serializable]
    public class Slide
    {
        public Sprite background;
        [TextArea(2, 5)] public string text;
        [Tooltip("자동 진행 기다리는 시간(초). 0이면 자동 대기 없이 '다음/스킵'만으로 진행")]
        public float holdSeconds = 2.0f;
    }

    [Header("Play")]
    [Tooltip("씬 시작 시 자동 재생")]
    public bool playOnSceneStart = true;

    [Header("Slides (배경 2장 + 문장)")]
    public List<Slide> slides = new List<Slide>();

    [Header("UI 옵션")]
    public float fadeSeconds = 0.4f;
    public float betweenFadeSeconds = 0.15f;
    [Tooltip("배경 어둡게(가독용)")]
    public float darken = 0.25f;

    [Header("입력")]
    [Tooltip("다음: 마우스 좌클릭/스페이스, 스킵: ESC")]
    public KeyCode nextKey = KeyCode.Space;
    public KeyCode skipKey = KeyCode.Escape;

    [Header("Font")]
    public TMP_FontAsset koreanFont;

    // 내부 UI
    Canvas _canvas;
    CanvasGroup _rootCg;
    Image _bgDim;          // 살짝 어둡게
    Image _bg;             // 배경 이미지
    TextMeshProUGUI _txt;  // 본문
    bool _isPlaying;
    bool _requestedNext;
    bool _requestedSkip;

    // (New) 끝남 이벤트: 외부에서 재생 완료를 받을 수 있어요.
    public event Action OnFinished;

    // (New) 외부에서 재생 상태 확인
    public bool IsPlaying => _isPlaying;

    // (New) 바깥에서 '끝날 때까지 기다리기' 코루틴
    public IEnumerator PlayAndWait()
    {
        if (!_isPlaying)
            yield return CoPlay();        // 처음 실행
        else
        {
            while (_isPlaying) yield return null;
        }
    }

    void Start()
    {
        if (playOnSceneStart) Play();
    }

    void Update()
    {
        if (!_isPlaying) return;
        if (Input.GetKeyDown(skipKey)) _requestedSkip = true;
        if (Input.GetKeyDown(nextKey) || Input.GetMouseButtonDown(0)) _requestedNext = true;
    }

    /// <summary>외부에서 수동 호출 가능</summary>
    public void Play()
    {
        if (_isPlaying) return;
        StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        _isPlaying = true;
        BuildUI();

        // 슬라이드가 비어있다면 Resources/Prologue/bg1,bg2 시도 + 예시 문구
        if (slides == null || slides.Count == 0)
        {
            var s1 = Resources.Load<Sprite>("Prologue/bg1");
            var s2 = Resources.Load<Sprite>("Prologue/bg2");
            slides = new List<Slide>()
            {
                new Slide{ background = s1, text="사람들은 하나둘씩 변했고,\n도시는 멈췄습니다.", holdSeconds=2.0f },
                new Slide{ background = s2, text="당신은 정화자가 되어\n마지막 탈출로를 찾아 나섭니다.", holdSeconds=2.0f }
            };
        }

        // 전체 페이드 인
        yield return Fade(_rootCg, 0f, 1f, fadeSeconds);

        for (int i = 0; i < slides.Count; i++)
        {
            if (_requestedSkip) break;

            // 배경제목 설정
            _bg.sprite = slides[i].background;
            _bg.enabled = _bg.sprite != null;
            _txt.text = slides[i].text ?? "";

            // 슬라이드 페이드 인
            yield return Fade(_bg, 0f, 1f, fadeSeconds);
            yield return Fade(_txt, 0f, 1f, fadeSeconds);

            // 대기: (시간 경과) 또는 (다음 입력)
            _requestedNext = false;
            float t = 0f, wait = Mathf.Max(0f, slides[i].holdSeconds);
            while (!_requestedNext && !_requestedSkip)
            {
                if (wait <= 0f) break; // 자동대기 없음
                t += Time.unscaledDeltaTime;
                if (t >= wait) break;
                yield return null;
            }

            if (_requestedSkip) break;

            // 슬라이드 페이드 아웃
            yield return Fade(_txt, 1f, 0f, fadeSeconds);
            yield return Fade(_bg, 1f, 0f, fadeSeconds);
            yield return new WaitForSecondsRealtime(betweenFadeSeconds);
        }

        // 전체 페이드 아웃
        yield return Fade(_rootCg, 1f, 0f, fadeSeconds);

        // 정리
        Destroy(_canvas.gameObject);
        _isPlaying = false;
        OnFinished?.Invoke();
    }

    void BuildUI()
    {
        // Canvas
        var go = new GameObject("PrologueUI");
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000; // 로딩보다 위
        go.AddComponent<GraphicRaycaster>();
        _rootCg = go.AddComponent<CanvasGroup>();
        _rootCg.alpha = 0f;

        // Dim (어두운 배경)
        var dim = new GameObject("Dim");
        dim.transform.SetParent(go.transform, false);
        _bgDim = dim.AddComponent<Image>();
        _bgDim.color = new Color(0f, 0f, 0f, darken);
        var dimRt = dim.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        _bg = bg.AddComponent<Image>();
        _bg.preserveAspect = true;
        _bg.color = Color.white;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        _bg.canvasRenderer.SetAlpha(0f);

        // Text
        var tx = new GameObject("Text");
        tx.transform.SetParent(go.transform, false);
        _txt = tx.AddComponent<TextMeshProUGUI>();
        if (koreanFont != null) _txt.font = koreanFont;
        _txt.alignment = TextAlignmentOptions.Center;
        _txt.fontSize = 42;
        _txt.enableWordWrapping = true;
        _txt.raycastTarget = false;
        _txt.color = Color.white;
        _txt.outlineWidth = 0.15f; // 가독
        var txRt = tx.GetComponent<RectTransform>();
        txRt.anchorMin = new Vector2(0.1f, 0.1f);
        txRt.anchorMax = new Vector2(0.9f, 0.35f);
        txRt.offsetMin = txRt.offsetMax = Vector2.zero;
        _txt.canvasRenderer.SetAlpha(0f);
    }

    IEnumerator Fade(Graphic g, float from, float to, float seconds)
    {
        if (g == null) yield break;
        if (seconds <= 0f) { g.canvasRenderer.SetAlpha(to); yield break; }

        g.canvasRenderer.SetAlpha(from);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / seconds);
            g.canvasRenderer.SetAlpha(a);
            yield return null;
        }
        g.canvasRenderer.SetAlpha(to);
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float seconds)
    {
        if (cg == null) yield break;
        if (seconds <= 0f) { cg.alpha = to; yield break; }

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

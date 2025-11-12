using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndCreditsController : MonoBehaviour
{
    [Header("UI 참조")]
    public CanvasGroup canvasGroup;          // 패널 전체 CanvasGroup(없으면 자동 추가)
    public TextMeshProUGUI titleText;        // 게임 이름
    public TextMeshProUGUI teamText;         // 팀명
    public TextMeshProUGUI membersText;      // 만든 사람 표기(가로/세로 전환)
    public TextMeshProUGUI skipHintText;     // '아무 키나 누르면 건너뜀'(선택)

    [Header("표시할 내용")]
    [TextArea] public string gameTitle = "DUSKBORN";
    [TextArea] public string teamName = "Team Radiant Star";
    [Tooltip("한 줄에 한 명씩 입력하세요.")]
    public string[] members = new[] { "DFDF", "Alice", "Bob" };

    public enum MembersLayout { Horizontal, Vertical }
    [Header("멤버 표기 방식")]
    public MembersLayout membersLayout = MembersLayout.Horizontal; // ← 기본: 가로
    [Tooltip("가로 표기 시 이름 사이에 들어갈 구분자")]
    public string horizontalSeparator = "  •  ";
    [Tooltip("가로 표기를 한 줄로 고정(줄바꿈 금지)할지")]
    public bool horizontalNoWrap = true;
    [Tooltip("가로 표기 시 글자 자동 크기 조절 여부")]
    public bool horizontalAutoSize = true;
    [Tooltip("자동 크기 최소/최대 (가로 표기)")]
    public float horizontalFontSizeMin = 28f;
    public float horizontalFontSizeMax = 48f;

    [Header("타이밍(실시간)")]
    public float fadeIn = 1.0f;
    public float holdTitle = 1.0f;
    public float holdTeam = 1.0f;
    public float holdMembers = 3.0f;
    public float fadeOut = 1.0f;

    [Header("동작 옵션")]
    public bool allowAnyKeySkip = true;
    public bool autoGoToNextScene = true;
    public string nextSceneName = "Title";   // 다음 씬(메인 메뉴 등)
    public bool quitIfNoNextScene = false;

    [Header("BGM(선택)")]
    public AudioSource bgm;
    public float bgmFadeOut = 1.0f;

    bool _skipping;

    void Reset()
    {
        if (!canvasGroup)
        {
            var cv = GetComponentInParent<Canvas>();
            if (cv)
                canvasGroup = cv.gameObject.GetComponent<CanvasGroup>() ?? cv.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        // 텍스트 바인딩
        if (titleText) titleText.text = gameTitle;
        if (teamText) teamText.text = teamName;
        ApplyMembersLayout(); // ← 가로/세로 적용

        if (skipHintText) skipHintText.gameObject.SetActive(allowAnyKeySkip);

        if (canvasGroup) canvasGroup.alpha = 0f; // 시작은 투명
        StartCoroutine(Co_Run());                // 타임스케일 무시
    }

    // ────────────────────────────────────────────────────────────────────────────
    void ApplyMembersLayout()
    {
        if (!membersText) return;

        if (membersLayout == MembersLayout.Horizontal)
        {
            // 가로: 구분자로 한 줄 구성
            membersText.text = string.Join(horizontalSeparator, members ?? new string[0]);
            membersText.alignment = TextAlignmentOptions.Center;

            // 줄바꿈/자동 크기 세팅
            membersText.enableWordWrapping = !horizontalNoWrap;
            membersText.enableAutoSizing = horizontalAutoSize;
            if (horizontalAutoSize)
            {
                membersText.fontSizeMin = horizontalFontSizeMin;
                membersText.fontSizeMax = horizontalFontSizeMax;
            }
        }
        else
        {
            // 세로: 줄바꿈으로 나열
            membersText.text = string.Join("\n", members ?? new string[0]);
            membersText.alignment = TextAlignmentOptions.Center;

            // 기본값 추천: 자동 크기 OFF, 줄바꿈 ON
            membersText.enableAutoSizing = false;
            membersText.enableWordWrapping = true;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    IEnumerator Co_Run()
    {
        // 페이드 인
        yield return StartCoroutine(Co_Fade(0f, 1f, fadeIn));

        // 타이틀 유지
        yield return StartCoroutine(Co_Wait(holdTitle));

        // 팀명 유지
        yield return StartCoroutine(Co_Wait(holdTeam));

        // 멤버 유지
        yield return StartCoroutine(Co_Wait(holdMembers));

        // 페이드 아웃(+BGM 다운)
        if (bgm) StartCoroutine(Co_FadeBgmDown());
        yield return StartCoroutine(Co_Fade(1f, 0f, fadeOut));

        // 다음 씬 or 종료
        if (autoGoToNextScene && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else if (quitIfNoNextScene || string.IsNullOrEmpty(nextSceneName))
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    IEnumerator Co_Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            if (allowAnyKeySkip && !_skipping && Input.anyKeyDown)
            {
                _skipping = true; // 현재 단계 즉시 스킵
                break;
            }
            yield return null;
        }
        _skipping = false; // 다음 단계 대비 초기화
    }

    IEnumerator Co_Fade(float from, float to, float duration)
    {
        if (!canvasGroup || duration <= 0f)
        {
            if (canvasGroup) canvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // 타임스케일 무시
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;

            if (allowAnyKeySkip && Input.anyKeyDown)
            {
                canvasGroup.alpha = to;
                break;
            }
        }
    }

    IEnumerator Co_FadeBgmDown()
    {
        if (!bgm || bgmFadeOut <= 0f) yield break;
        float start = bgm.volume;
        float t = 0f;
        while (t < bgmFadeOut)
        {
            t += Time.unscaledDeltaTime;
            bgm.volume = Mathf.Lerp(start, 0f, t / bgmFadeOut);
            yield return null;
        }
        bgm.volume = 0f;
    }
}

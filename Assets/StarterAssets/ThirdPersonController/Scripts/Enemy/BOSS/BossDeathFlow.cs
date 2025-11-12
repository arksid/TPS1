using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.UI; // 페이드용

public class BossDeathFlow : MonoBehaviour
{
    [Header("참조(비워두면 자동 탐색)")]
    public BossMonster boss;
    public Animator animator;
    public BossPatternDirector pattern;
    public BossLocomotionAnimDriver locomotionDriver;
    public NavMeshAgent agent;

    [Header("죽는 연출")]
    public string deathTrigger = "Die";
    public float deathWaitSeconds = 3.0f;   // 실시간 대기

    [Header("엔딩 전 페이드아웃")]
    public CanvasGroup fadeCanvasGroup;     // 없으면 자동 생성
    public bool autoCreateFadeOverlay = true;
    public float fadeDuration = 1.5f;
    public Color fadeColor = Color.black;
    public bool blockRaycastsDuringFade = true;

    [Header("엔딩 전환")]
    public string endingSceneName = "Ending"; // 빌드 세팅 등록 必

    bool _done;
    bool _sceneLoading;
    bool _fadeStarted;

    void Reset()
    {
        boss = GetComponent<BossMonster>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        pattern = GetComponent<BossPatternDirector>();
        locomotionDriver = GetComponentInChildren<BossLocomotionAnimDriver>();
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        if (!boss) boss = GetComponent<BossMonster>();
        if (boss) boss.onBossDead.AddListener(HandleBossDead);
    }

    void OnDisable()
    {
        if (boss) boss.onBossDead.RemoveListener(HandleBossDead);
    }

    public void HandleBossDead()
    {
        if (_done) return;
        _done = true;
        StartCoroutine(Co_DeathSequence());
    }

    System.Collections.IEnumerator Co_DeathSequence()
    {
        // 1) 즉시 AI/이동 정지
        if (pattern) pattern.enabled = false;
        SafeStopAndDisableAgent();           // ← 문제 있었던 부분을 안전하게 처리
        if (locomotionDriver) locomotionDriver.enabled = false;

        // 2) 죽는 애니 트리거(있으면)
        if (animator && !string.IsNullOrEmpty(deathTrigger) &&
            HasParam(animator, deathTrigger, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(deathTrigger);
            animator.SetTrigger(deathTrigger);
        }

        // 3) 애니 끝이나 지정시간 대기(실시간)
        yield return new WaitForSecondsRealtime(deathWaitSeconds);

        // 4) 페이드 후 씬 전환
        yield return StartCoroutine(Co_FadeOutAndLoad());
    }

    // 애니메이션 이벤트로 직접 호출하면 대기 없이 즉시 페이드 시작
    public void OnDeathAnimFinished()
    {
        if (_fadeStarted) return;
        _fadeStarted = true;
        StartCoroutine(Co_FadeOutAndLoad());
    }

    // NavMeshAgent 안전 정지/비활성화
    void SafeStopAndDisableAgent()
    {
        if (!agent) return;

        // 활성, NavMesh 위, 계층상 활성일 때만 정지 API 사용
        if (agent.enabled && agent.gameObject.activeInHierarchy)
        {
            // isOnNavMesh 체크: NavMesh 위가 아니면 Stop/ResetPath를 호출하지 않는다
            bool onNav = false;
            try
            {
                onNav = agent.isOnNavMesh; // 안전하게 false/true만 얻는다
            }
            catch
            {
                onNav = false;
            }

            if (onNav)
            {
                // 안전한 정지
                agent.ResetPath();
                agent.isStopped = true;
            }
        }

        // 어쨌든 비활성화하여 이후 호출 차단
        agent.enabled = false;
    }

    System.Collections.IEnumerator Co_FadeOutAndLoad()
    {
        if (_sceneLoading) yield break;
        _sceneLoading = true;

        EnsureFadeOverlay();

        float t = 0f;
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = blockRaycastsDuringFade;
        fadeCanvasGroup.interactable = blockRaycastsDuringFade;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // 타임스케일 무시
            fadeCanvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(0.15f);

        if (!string.IsNullOrEmpty(endingSceneName))
            SceneManager.LoadScene(endingSceneName);
    }

    void EnsureFadeOverlay()
    {
        if (fadeCanvasGroup) return;

        if (!autoCreateFadeOverlay)
        {
            Debug.LogWarning("[BossDeathFlow] fadeCanvasGroup이 없습니다. autoCreateFadeOverlay=false면 직접 연결하세요.");
            // 그래도 안전하게 생성
        }

        var root = new GameObject("AutoFadeOverlay");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        root.AddComponent<GraphicRaycaster>();
        fadeCanvasGroup = root.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = blockRaycastsDuringFade;
        fadeCanvasGroup.interactable = blockRaycastsDuringFade;

        var imgGO = new GameObject("Panel");
        imgGO.transform.SetParent(root.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = fadeColor;

        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    bool HasParam(Animator a, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in a.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }
}

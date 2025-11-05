// Assets/StarterAssets/ThirdPersonController/Scripts/UI/Seen/DeathPanel.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathPanel : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs (비워도 자동 탐색)")]
    [SerializeField] private GameObject root;                 // 패널 루트(보통 GameOverCanvas)
    [SerializeField] private CanvasGroup canvasGroup;         // 페이드용
    [SerializeField] private TextMeshProUGUI titleText;       // "사망"
    [SerializeField] private TextMeshProUGUI descText;        // "아무 곳이나 클릭하면 ..."

    [Header("Options")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool pauseTimeOnShow = true;     // 보여줄 때 시간 멈춤
    [SerializeField] private float clickUnlockDelay = 0.30f;  // 표시 직후 오클릭 방지
    [SerializeField] private float fadeInSeconds = 0.25f;    // 페이드 인 시간
    [SerializeField] private float fadeOutSeconds = 0.25f;    // 페이드 아웃 시간
    [SerializeField] private int sortingOrder = 9999;     // 맨 위로 보이게

    private bool _shown;
    private float _shownAt;
    private Coroutine _fadeCo;

    // ---------- 안전한 참조 유틸(비활성 포함 탐색) ----------
    private T FindInChildrenInactive<T>(Transform t = null) where T : Component
    {
        if (t == null) t = transform;
        var arr = t.GetComponentsInChildren<T>(true);
        return (arr != null && arr.Length > 0) ? arr[0] : null;
    }

    /// <summary>
    /// 이미 있으면 재사용, 없을 때만 추가/세팅
    /// </summary>
    private void EnsureUIInfrastructure()
    {
        if (root == null) root = gameObject;

        // 1) Canvas 가져오되, 절대로 중복 추가하지 않음
        //    (자신 또는 부모에 이미 있으면 재사용)
        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas == null) canvas = root.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            // 정말로 어디에도 없을 때만 추가
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvas.sortingOrder = sortingOrder;

        // 2) 클릭 이벤트용 Raycaster 보장
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        // 3) CanvasScaler(권장) – 없으면 추가
        if (canvas.GetComponent<CanvasScaler>() == null)
        {
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 4) EventSystem 보장(씬에 1개 필요)
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem").AddComponent<EventSystem>();
            es.gameObject.AddComponent<StandaloneInputModule>();
        }

        // 5) CanvasGroup 확보(없으면 추가)
        if (canvasGroup == null)
        {
            canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = FindInChildrenInactive<CanvasGroup>(root.transform);
            if (canvasGroup == null) canvasGroup = root.AddComponent<CanvasGroup>();
        }
    }

    private void Awake()
    {
        EnsureUIInfrastructure();
        // 시작 시 보이지 않게
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        if (root != null && root.activeSelf) root.SetActive(false);
        _shown = false;
    }

    /// <summary>
    /// 게임오버 표시(페이드 인)
    /// </summary>
    public void Show(string title = "사망",
                     string desc = "화면을 클릭하면 메인 메뉴로 이동합니다.")
    {
        EnsureUIInfrastructure();

        if (titleText == null) titleText = FindInChildrenInactive<TextMeshProUGUI>(transform);
        if (descText == null) descText = FindInChildrenInactive<TextMeshProUGUI>(transform);

        if (titleText) titleText.text = title;
        if (descText) descText.text = desc;

        if (root != null && !root.activeSelf) root.SetActive(true);

        // Animator가 CanvasGroup을 제어하면 충돌하므로 일단 비활성
        var anim = root.GetComponentInChildren<Animator>(true);
        if (anim) anim.enabled = false;

        if (pauseTimeOnShow) Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 페이드 인
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeCanvas(canvasGroup, toAlpha: 1f, seconds: fadeInSeconds, makeInteractableAtEnd: true));

        _shown = true;
        _shownAt = Time.unscaledTime;

        Debug.Log("[DeathPanel] Show(): 페이드 인 시작");
    }

    /// <summary>
    /// 숨김(페이드 아웃)
    /// </summary>
    public void Hide()
    {
        EnsureUIInfrastructure();

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeCanvas(canvasGroup, toAlpha: 0f, seconds: fadeOutSeconds, makeInteractableAtEnd: false, onEnded: () =>
        {
            if (root != null && root.activeSelf) root.SetActive(false);
        }));

        _shown = false;
    }

    // 화면 아무 곳 클릭
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_shown) return;
        if (Time.unscaledTime - _shownAt < clickUnlockDelay) return; // 표시 직후 오클릭 방지
        GoMainMenu();
    }

    private void GoMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("[DeathPanel] mainMenuSceneName 비어있음. Build Settings에 등록된 씬 이름을 입력하세요.");
            return;
        }

        Time.timeScale = 1f;

        // SceneLoader가 있으면 로딩화면과 함께
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneAsync(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    // ------------------- 페이드 코루틴 -------------------
    private IEnumerator FadeCanvas(CanvasGroup cg, float toAlpha, float seconds, bool makeInteractableAtEnd, System.Action onEnded = null)
    {
        if (cg == null)
        {
            onEnded?.Invoke();
            yield break;
        }

        // 시작값 캡처
        float from = cg.alpha;
        float t = 0f;

        // 페이드 시작 시에는 클릭차단만이라도 켜서 뒤 UI 클릭 방지
        cg.blocksRaycasts = true;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;                     // 시간 멈춤과 무관하게 페이드
            cg.alpha = Mathf.Lerp(from, toAlpha, t / seconds);
            yield return null;
        }
        cg.alpha = toAlpha;

        // 끝난 뒤 인터랙션 상태
        cg.interactable = makeInteractableAtEnd && toAlpha > 0.99f;
        cg.blocksRaycasts = toAlpha > 0.01f;                 // 0이면 클릭 통과, 1이면 클릭 받음

        onEnded?.Invoke();
    }

#if UNITY_EDITOR
    // 에디터 빠른 테스트: P=표시, O=숨김
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) Show();
        if (Input.GetKeyDown(KeyCode.O)) Hide();
    }
#endif
}

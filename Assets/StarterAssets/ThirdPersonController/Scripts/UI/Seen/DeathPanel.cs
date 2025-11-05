// Assets/StarterAssets/ThirdPersonController/Scripts/UI/Seen/DeathPanel.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class DeathPanel : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs (비워도 자동으로 찾습니다)")]
    [SerializeField] private GameObject root;           // 패널 루트(캔버스 또는 그 하위)
    [SerializeField] private CanvasGroup canvasGroup;   // 패널의 CanvasGroup
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;

    [Header("Options")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool pauseTimeOnShow = true;
    [SerializeField] private float clickUnlockDelay = 0.3f;

    private bool _shown;
    private float _shownAt;

    // --------- 공통 유틸: 비활성 포함 안전 탐색 ----------
    private T FindInChildrenInactive<T>(Transform t = null) where T : Component
    {
        if (t == null) t = transform;
        var arr = t.GetComponentsInChildren<T>(true); // includeInactive:true
        return (arr != null && arr.Length > 0) ? arr[0] : null;
    }

    /// <summary>
    /// Canvas / CanvasGroup / GraphicRaycaster / EventSystem을
    /// "이미 있으면 재사용"하고, 없으면 "그때만 추가"합니다.
    /// </summary>
    private void EnsureUIInfrastructure()
    {
        // 0) root 지정 (없으면 자기 자신)
        if (root == null) root = gameObject;

        // 1) Canvas 가져오기 (이미 있으면 재사용)
        //    - 우선 root에서 찾고, 없으면 부모에서도 찾아봄
        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = root.GetComponentInParent<Canvas>(true); // 비활성 부모도 탐색
            if (canvas == null)
            {
                // 정말 없을 때만 추가
                canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
        // 최상단 보장
        canvas.sortingOrder = 9999;

        // 2) GraphicRaycaster (클릭 받기 위함)
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        // 3) CanvasScaler(선택) – 없으면 추가 (UI 스케일 안정화)
        if (canvas.GetComponent<CanvasScaler>() == null)
            canvas.gameObject.AddComponent<CanvasScaler>();

        // 4) EventSystem 1개 보장
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // 5) CanvasGroup 찾기/생성
        if (canvasGroup == null)
        {
            // 우선 root에서 찾아보고, 없으면 자식들에서(비활성 포함) 찾음
            canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = FindInChildrenInactive<CanvasGroup>(root.transform);
            if (canvasGroup == null)
                canvasGroup = root.AddComponent<CanvasGroup>(); // 마지막 수단
        }
    }

    private void Awake()
    {
        EnsureUIInfrastructure();
        Hide(); // 시작 시 숨김 상태로
    }

    public void Show(string title = "사망",
                     string desc = "화면을 클릭하면 메인 메뉴로 이동합니다.")
    {
        EnsureUIInfrastructure(); // 참조 보장

        if (titleText == null) titleText = FindInChildrenInactive<TextMeshProUGUI>(transform);
        if (descText == null) descText = FindInChildrenInactive<TextMeshProUGUI>(transform); // 필요시 별도 지정

        if (titleText) titleText.text = title;
        if (descText) descText.text = desc;

        // ✅ 루트 활성화 강제 (비활성이어도 켬)
        if (!root.activeSelf) root.SetActive(true);

        // ✅ CanvasGroup 표시/인터랙션 강제
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        // 애니메이터가 CanvasGroup을 제어하면 충돌하므로 잠시 비활성
        var anim = root.GetComponentInChildren<Animator>(true);
        if (anim) anim.enabled = false;

        if (pauseTimeOnShow) Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _shown = true;
        _shownAt = Time.unscaledTime;

        Debug.Log("[DeathPanel] Show(): root 활성화 + alpha=1 + 상단 정렬 완료");
    }

    public void Hide()
    {
        EnsureUIInfrastructure(); // 참조 보장

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (root.activeSelf) root.SetActive(false);
        _shown = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_shown) return;
        if (Time.unscaledTime - _shownAt < clickUnlockDelay) return; // 오클릭 방지
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

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneAsync(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

#if UNITY_EDITOR
    // 에디터 테스트용: P=표시, O=숨김
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) Show();
        if (Input.GetKeyDown(KeyCode.O)) Hide();
    }
#endif
}

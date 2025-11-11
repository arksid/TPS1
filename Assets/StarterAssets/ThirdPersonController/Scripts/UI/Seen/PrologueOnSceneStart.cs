using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PrologueOnSceneStart : MonoBehaviour
{
    [Header("Prologue Prefab (Inspector or Resources fallback)")]
    public PrologueSequence prologuePrefab;
    [SerializeField] private string prologueResourcePath = "UI/PrologueSequence";
    // => Assets/Resources/UI/PrologueSequence.prefab 가 있으면 자동으로 씁니다.

    [Header("Gameplay Lock")]
    [Tooltip("컷씬 동안 시간을 멈춰 전체 움직임/물리/AI 정지")]
    public bool pauseTimeWhilePrologue = true;

    [Tooltip("컷씬 동안 비활성화할 게임오브젝트들(플레이어 루트, HUD 등)")]
    public GameObject[] disableObjectsDuring;
    [Tooltip("컷씬 동안 비활성화할 컴포넌트들(PlayerInput, 캐릭터 컨트롤러, 카메라 입력 등)")]
    public MonoBehaviour[] disableComponentsDuring;

    [Tooltip("컷씬 이후 다시 활성화할 게임오브젝트들")]
    public GameObject[] enableObjectsAfter;
    [Tooltip("컷씬 이후 다시 활성화할 컴포넌트들")]
    public MonoBehaviour[] enableComponentsAfter;

    [Header("Hook (선택)")]
    public UnityEvent onBeforePrologue;   // 컷씬 직전 호출
    public UnityEvent onAfterPrologue;    // 컷씬 직후 호출(여기서 게임 시작 로직 연결)

    [Header("Session")]
    [Tooltip("한 세션(게임 실행) 동안 이 씬에서 한 번만 재생")]
    public bool runOnlyOncePerSession = true;

    // 내부: 원상복구를 위해 이전 활성 상태 저장
    List<(Behaviour comp, bool prev)> _compPrev = new();
    List<(GameObject go, bool prev)> _objPrev = new();
    static bool _alreadyRanThisSession = false;

    void Start()
    {
        if (runOnlyOncePerSession && _alreadyRanThisSession)
        {
            // 이전에 이미 재생됨 → 그냥 켜둘 것들만 켜고 종료
            RestoreOrEnableAfter();
            Destroy(this);
            return;
        }
        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        onBeforePrologue?.Invoke();

        // 사전 차단
        if (pauseTimeWhilePrologue) Time.timeScale = 0f;
        LockTargets(true);

        // 프리팹 찾기(인스펙터 우선 → Resources 폴백)
        PrologueSequence prefab = prologuePrefab ?? Resources.Load<PrologueSequence>(prologueResourcePath);

        if (prefab != null)
        {
            var seq = Instantiate(prefab);
            seq.playOnSceneStart = false; // 수동 재생
            yield return seq.PlayAndWait(); // 끝날 때까지 대기 (코루틴은 unscaledDeltaTime으로 동작)
            if (seq) Destroy(seq.gameObject);
        }
        // 프리팹이 없어도 그냥 넘어감(에러 X)

        if (pauseTimeWhilePrologue) Time.timeScale = 1f;

        _alreadyRanThisSession = true;

        // 원복 + 이후 활성 대상 켜기
        LockTargets(false);
        RestoreOrEnableAfter();

        onAfterPrologue?.Invoke();
        Destroy(this);
    }

    void LockTargets(bool disable)
    {
        if (disable)
        {
            _compPrev.Clear();
            _objPrev.Clear();

            if (disableComponentsDuring != null)
                foreach (var c in disableComponentsDuring)
                    if (c) { _compPrev.Add((c, c.enabled)); c.enabled = false; }

            if (disableObjectsDuring != null)
                foreach (var go in disableObjectsDuring)
                    if (go) { _objPrev.Add((go, go.activeSelf)); go.SetActive(false); }
        }
        else
        {
            foreach (var (c, prev) in _compPrev)
                if (c) c.enabled = prev;

            foreach (var (go, prev) in _objPrev)
                if (go) go.SetActive(prev);

            _compPrev.Clear();
            _objPrev.Clear();
        }
    }

    void RestoreOrEnableAfter()
    {
        if (enableObjectsAfter != null)
            foreach (var go in enableObjectsAfter) if (go) go.SetActive(true);

        if (enableComponentsAfter != null)
            foreach (var c in enableComponentsAfter) if (c) c.enabled = true;
    }
}

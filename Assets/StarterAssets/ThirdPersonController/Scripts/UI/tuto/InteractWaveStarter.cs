using System.Collections;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractWaveStarter : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("상호작용 키")]
    public KeyCode interactKey = KeyCode.E;
    public bool onlyOnce = true;              // 한 번만 실행
    public bool deactivateAfterStart = false; // 시작 후 이 트리거를 끌지

    [Header("스포너 대상")]
    [Tooltip("시작시킬 스포너 컴포넌트(예: EnemyWaveSpawner, EnemySwarmDirector 등)")]
    public MonoBehaviour spawner;              // ★ 스포너 컴포넌트를 여기로 드래그
    public GameObject spawnerGameObject;       // 비우면 spawner.gameObject 사용

    [Header("활성 보장")]
    public bool activateGameObject = true;     // 스포너 GO가 꺼져 있으면 켜줌
    public bool enableComponent = true;        // 스포너 컴포넌트가 꺼져 있으면 켜줌

    [Header("시작 메서드 이름(자동 탐색 순서)")]
    public string[] startMethodCandidates = new[] {
        "RunWaves", "StartSpawning", "StartSpawn", "StartWave", "Begin", "Play"
    };

    [Header("프롬프트(선택) - TutorialUI 활용")]
    public TutorialUI tutorialUI;              // 있으면 안내 표시/숨김에 사용
    [TextArea] public string promptTitle = "상호작용";
    [TextArea] public string promptMessage = "E 키를 눌러 웨이브 시작";
    public bool hidePromptWhenStarted = true;

    bool inRange;
    bool started;

    void Reset()
    {
        // 충돌체를 트리거로
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = true;
        ShowPrompt(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = false;
        ShowPrompt(false);
    }

    void Update()
    {
        if (!inRange) return;
        if (onlyOnce && started) return;

        if (Input.GetKeyDown(interactKey))
            TryStartSpawner();
    }

    [ContextMenu("TryStartSpawner (테스트)")]
    public void TryStartSpawner()
    {
        if (onlyOnce && started) return;

        if (!spawner)
        {
            Debug.LogWarning("[InteractWaveStarter] spawner(컴포넌트)가 비어 있습니다.");
            return;
        }

        var go = spawnerGameObject ? spawnerGameObject : spawner.gameObject;

        // 1) 활성 보장
        if (activateGameObject && go && !go.activeSelf) go.SetActive(true);
        if (enableComponent && !spawner.enabled) spawner.enabled = true;

        // 2) 시작 메서드 찾기
        MethodInfo found = null;
        foreach (var name in startMethodCandidates)
        {
            found = spawner.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (found != null) break;
        }

        if (found == null)
        {
            Debug.LogWarning("[InteractWaveStarter] 시작 메서드를 찾지 못했습니다. 후보: " +
                             string.Join(", ", startMethodCandidates));
            return;
        }

        // 3) 호출 (IEnumerator면 코루틴으로)
        var ret = found.Invoke(spawner, null);
        if (ret is IEnumerator ie) StartCoroutine(ie);

        started = true;
        if (hidePromptWhenStarted) ShowPrompt(false);
        if (deactivateAfterStart) gameObject.SetActive(false);
        // Debug.Log($"[InteractWaveStarter] '{found.Name}' 실행 완료");
    }

    void ShowPrompt(bool on)
    {
        if (!tutorialUI) return;

        if (on)
        {
            tutorialUI.Show(promptTitle, promptMessage);
        }
        else
        {
            // TutorialUI.Hide()가 없을 수도 있으니 리플렉션으로 시도
            var hide = tutorialUI.GetType().GetMethod(
                "Hide",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (hide != null) hide.Invoke(tutorialUI, null);
            else tutorialUI.Show(string.Empty, string.Empty); // 대체: 빈 메시지로 덮기
        }
    }
}

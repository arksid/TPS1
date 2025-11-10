using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class HoldZoneMission : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("게이지 설정")]
    [Range(0, 100)] public float progressPercent;      // 현재 %
    [Range(1, 100)] public float targetPercent = 100f; // 목표 %
    public float fillPerSec = 25f;    // 안에 있을 때 /sec
    public float decayPerSec = 15f;   // 밖에 있을 때 /sec
    public bool clampToZero = true;   // 음수 방지

    [Header("UI")]
    public HoldZoneUI ui;
    public bool autoShowUIOnEnable = false;
    [TextArea] public string enterMsg = "거점 안에 머물러 게이지를 채우세요!";
    [TextArea] public string leavingMsg = "거점 밖입니다! 안으로 복귀하세요!";
    [TextArea] public string completeMsg = "거점 확보 완료!";

    [Header("스폰/잔류 정리 (구 스웜 디렉터 호환)")]
    public EnemySwarmDirector swarm;
    public EnemySwarmDirector[] extraSwarms;
    public bool stopSwarmOnComplete = true;

    [Header("스포너 제어 (EnemyWaveSpawner)")]
    [Tooltip("트리거 진입 시 켜줄 스포너 게임오브젝트(시작 시 비활성이어야 함). 켜지면 EnemyWaveSpawner의 Start()가 바로 웨이브를 시작합니다.")]
    public GameObject spawnerGOToEnableOnEnter;
    public bool enableSpawnerOnEnter = true;

    [Tooltip("클리어 시 EnemyWaveSpawner를 멈춥니다. EndWave()가 없으면 GO를 꺼서 중지합니다.")]
    public bool stopWaveSpawnerOnComplete = true;
    [Tooltip("클리어 시 스포너 GO를 끕니다(SetActive false).")]
    public bool disableSpawnerGOOnComplete = false;

    [Tooltip("직접 참조(선택). 없으면 spawnerGOToEnableOnEnter에서 EnemyWaveSpawner를 찾아 사용.")]
    public EnemyWaveSpawner waveSpawnerRef;

    [Header("잔류 오브젝트 정리")]
    public bool despawnLeftovers = true;
    public string enemyTag = "Enemy";
    public string projectileTag = "EnemyProjectile";
    public float despawnDelay = 0f;

    [Header("완료 후 네비게이션")]
    public bool clearWaypointOnComplete = true;
    public bool showNextWaypointOnComplete = true;
    public SimpleWaypointUI nextWaypointUI;
    public Transform nextTarget;
    [TextArea] public string nextMessage = "보스 방으로 이동!";

    [Header("완료 후 다음 트리거 활성화")]
    public bool activateNextTriggerOnComplete = true;
    public Transform nextTriggerToActivate;
    [Tooltip("부모가 꺼져 있으면 먼저 켭니다.")]
    public GameObject nextTriggerRoot;

    [Header("완료 UI")]
    public bool hideUIOnComplete = true;

    [Header("추가 훅")]
    public UnityEvent onCompleted;

    bool _playerInside;
    bool _completed;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    public void ForceEnter()
    {
        _playerInside = true;
        if (ui) ui.Show();
        TryEnableSpawnerOnce(); // 워프/겹침으로 강제 진입된 경우에도 켜 주기
    }
    public void ForceExit() => _playerInside = false;

    void OnEnable()
    {
        _completed = false;
        if (ui)
        {
            ui.SetProgress(targetPercent > 0 ? progressPercent / targetPercent : 1f);
            ui.SetHint(enterMsg);
            if (autoShowUIOnEnable) ui.Show(); else ui.Hide();
        }
    }

    void Update()
    {
        if (_completed) return;

        float dt = Time.deltaTime;
        progressPercent += (_playerInside ? fillPerSec : -decayPerSec) * dt;

        if (clampToZero && progressPercent < 0f) progressPercent = 0f;
        if (progressPercent > targetPercent) progressPercent = targetPercent;

        if (ui)
        {
            ui.SetProgress(targetPercent > 0 ? progressPercent / targetPercent : 1f);
            ui.SetHint(_playerInside ? enterMsg : leavingMsg);
        }

        if (progressPercent >= targetPercent)
        {
            _completed = true;
            if (ui) { ui.SetProgress(1f); ui.SetHint(completeMsg); }
            StartCoroutine(Co_CompleteSequence());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = true;
        if (ui) ui.Show();

        TryEnableSpawnerOnce(); // ← 여기서 스포너 GO 켜줌
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = false;
    }

    bool _spawnerEnabledOnce;
    void TryEnableSpawnerOnce()
    {
        if (!enableSpawnerOnEnter || _spawnerEnabledOnce) return;
        _spawnerEnabledOnce = true;

        if (spawnerGOToEnableOnEnter)
        {
            ActivateHierarchy(spawnerGOToEnableOnEnter); // 부모까지 켜주기(부모가 꺼져 있어도 OK)
            Debug.Log("[HoldZoneMission] 스포너 활성화: " + spawnerGOToEnableOnEnter.name);
        }

        // 참조 캐시: 명시 참조가 비었으면 GO에서 찾아둠
        if (!waveSpawnerRef && spawnerGOToEnableOnEnter)
            waveSpawnerRef = spawnerGOToEnableOnEnter.GetComponentInChildren<EnemyWaveSpawner>(true);
    }

    IEnumerator Co_CompleteSequence()
    {
        yield return null;

        // 1) (구) EnemySwarmDirector 정지
        if (stopSwarmOnComplete)
        {
            StopSwarmSafe(swarm);
            if (extraSwarms != null) foreach (var s in extraSwarms) StopSwarmSafe(s);
        }

        // 2) (신) EnemyWaveSpawner 정지/비활성
        if (stopWaveSpawnerOnComplete)
            StopWaveSpawnerSafe(ResolveWaveSpawner());
        if (disableSpawnerGOOnComplete && spawnerGOToEnableOnEnter)
            spawnerGOToEnableOnEnter.SetActive(false);

        // 3) 잔류 적/투사체 정리
        if (despawnLeftovers)
        {
            if (despawnDelay > 0f) yield return new WaitForSeconds(despawnDelay);
            DespawnByTag(enemyTag);
            DespawnByTag(projectileTag);
        }

        // 4) 홀드 UI 숨김
        if (hideUIOnComplete && ui) ui.Hide();

        // 5) 웨이포인트 정리/다음 안내
        if (clearWaypointOnComplete) WaypointDirector.Clear();

        if (showNextWaypointOnComplete && nextWaypointUI && nextTarget)
        {
            if (!nextWaypointUI.gameObject.activeSelf) nextWaypointUI.gameObject.SetActive(true);
            if (nextWaypointUI.canvas && !nextWaypointUI.canvas.gameObject.activeSelf) nextWaypointUI.canvas.gameObject.SetActive(true);
            WaypointDirector.EnableHints();
            WaypointDirector.Show(nextWaypointUI, nextTarget, nextMessage);
        }

        // 6) 다음 트리거 활성화(부모→자식 순서)
        if (activateNextTriggerOnComplete)
        {
            if (nextTriggerRoot && !nextTriggerRoot.activeSelf)
            {
                nextTriggerRoot.SetActive(true);
                Debug.Log("[HoldZoneMission] nextTriggerRoot 활성화");
                yield return null;
            }
            if (nextTriggerToActivate && !nextTriggerToActivate.gameObject.activeSelf)
            {
                nextTriggerToActivate.gameObject.SetActive(true);
                Debug.Log($"[HoldZoneMission] 보스방 트리거 활성화: {nextTriggerToActivate.name}");
            }
        }

        onCompleted?.Invoke();
    }

    // --- 헬퍼들 ---

    void StopSwarmSafe(EnemySwarmDirector s)
    {
        if (!s) return;
        var m = s.GetType().GetMethod("EndWave", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null) m.Invoke(s, null);
        else s.StopAllCoroutines();
    }

    EnemyWaveSpawner ResolveWaveSpawner()
    {
        if (waveSpawnerRef) return waveSpawnerRef;
        if (spawnerGOToEnableOnEnter)
            return spawnerGOToEnableOnEnter.GetComponentInChildren<EnemyWaveSpawner>(true);
        return null;
    }

    void StopWaveSpawnerSafe(EnemyWaveSpawner s)
    {
        if (!s) return;

        // EndWave()가 있다면 호출, 없으면 GO를 꺼서 중지
        var m = s.GetType().GetMethod("EndWave", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null)
        {
            m.Invoke(s, null);
            Debug.Log("[HoldZoneMission] EnemyWaveSpawner.EndWave() 호출");
        }
        else
        {
            if (s.gameObject.activeSelf) s.gameObject.SetActive(false);
            Debug.Log("[HoldZoneMission] EndWave 없음 → 스포너 GO 비활성으로 중지");
        }
    }

    void DespawnByTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return;
        GameObject[] arr;
        try { arr = GameObject.FindGameObjectsWithTag(tagName); }
        catch { return; }
        foreach (var go in arr) if (go) Destroy(go);
    }

    static void ActivateHierarchy(GameObject leaf)
    {
        // 부모가 꺼져 있어도 루트→리프 순으로 전부 켠다
        var stack = new System.Collections.Generic.Stack<Transform>();
        var t = leaf.transform;
        while (t != null) { stack.Push(t); t = t.parent; }
        while (stack.Count > 0)
        {
            var cur = stack.Pop().gameObject;
            if (!cur.activeSelf) cur.SetActive(true);
        }
    }
}

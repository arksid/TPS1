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
    public float fillPerSec = 25f;    // 안에 있을 때 / 초
    public float decayPerSec = 15f;   // 밖에 있을 때 / 초
    public bool clampToZero = true;   // 0 미만 방지

    [Header("UI")]
    public HoldZoneUI ui;
    public bool autoShowUIOnEnable = false;
    [TextArea] public string enterMsg = "거점 안에 머물러 게이지를 채우세요!";
    [TextArea] public string leavingMsg = "거점 밖입니다! 안으로 복귀하세요!";
    [TextArea] public string completeMsg = "거점 확보 완료!";

    [Header("(구) 스웜 디렉터 정지")]
    public EnemySwarmDirector swarm;
    public EnemySwarmDirector[] extraSwarms;
    public bool stopSwarmOnComplete = true;

    [Header("(신) 웨이브 스포너 제어 (EnemyWaveSpawner)")]
    [Tooltip("트리거 진입 시 켜줄 스포너 오브젝트(시작 시 비활성). 켜지면 스포너의 Start()에서 자동 시작.")]
    public GameObject spawnerGOToEnableOnEnter;
    public bool enableSpawnerOnEnter = true;

    [Tooltip("클리어 시 EnemyWaveSpawner를 중지합니다. EndWave()가 없으면 GO를 꺼서 중지합니다.")]
    public bool stopWaveSpawnerOnComplete = true;

    [Tooltip("클리어 시 스포너 GO를 끕니다(SetActive(false)).")]
    public bool disableSpawnerGOOnComplete = false;

    [Tooltip("참조가 비어있으면 spawnerGOToEnableOnEnter에서 자동으로 찾습니다.")]
    public EnemyWaveSpawner waveSpawnerRef;

    [Header("잔류 오브젝트 정리")]
    public bool despawnLeftovers = true;
    public string enemyTag = "Enemy";
    public string projectileTag = "EnemyProjectile";
    public float despawnDelay = 0f;

    [Header("완료 후 웨이포인트/다음 안내")]
    public bool clearWaypointOnComplete = true;
    public bool showNextWaypointOnComplete = true;
    public SimpleWaypointUI nextWaypointUI;
    public Transform nextTarget;
    [TextArea] public string nextMessage = "보스 방으로 이동!";

    [Header("완료 후 다음 트리거 활성화")]
    public bool activateNextTriggerOnComplete = true;
    public Transform nextTriggerToActivate;
    [Tooltip("부모가 꺼져 있다면 먼저 켜줍니다.")]
    public GameObject nextTriggerRoot;

    [Header("완료 UI")]
    public bool hideUIOnComplete = true;

    [Header("완료 시 내 트리거 처리")]
    public bool disableOwnTriggerOnComplete = true;   // ★ 완료 후 내 콜라이더 비활성

    [Header("추가 이벤트")]
    public UnityEvent onCompleted;

    // 내부 상태
    bool _playerInside;
    bool _completed;
    public bool IsCompleted => _completed;            // ★ 외부 가드용

    Collider _col;
    bool _spawnerEnabledOnce;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    public void ForceEnter()
    {
        _playerInside = true;
        // ★ 완료된 뒤에는 UI를 다시 켜지 않음
        if (!_completed && ui) ui.Show();
        TryEnableSpawnerOnce();
    }

    public void ForceExit() => _playerInside = false;

    void OnEnable()
    {
        _completed = false;
        _col = GetComponent<Collider>();

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
        if (_completed) return;                 // ★ 완료 후 재표시 차단
        _playerInside = true;
        if (ui) ui.Show();

        TryEnableSpawnerOnce();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (_completed) return;                 // ★ 완료 후 무시
        _playerInside = false;
    }

    void TryEnableSpawnerOnce()
    {
        if (!enableSpawnerOnEnter || _spawnerEnabledOnce) return;
        _spawnerEnabledOnce = true;

        if (spawnerGOToEnableOnEnter)
        {
            ActivateHierarchy(spawnerGOToEnableOnEnter); // 부모까지 켜줌
            Debug.Log("[HoldZoneMission] 스포너 활성화: " + spawnerGOToEnableOnEnter.name);
        }

        if (!waveSpawnerRef && spawnerGOToEnableOnEnter)
            waveSpawnerRef = spawnerGOToEnableOnEnter.GetComponentInChildren<EnemyWaveSpawner>(true);
    }

    IEnumerator Co_CompleteSequence()
    {
        yield return null;

        // (1) 스웜/웨이브 정지
        if (stopSwarmOnComplete)
        {
            StopSwarmSafe(swarm);
            if (extraSwarms != null) foreach (var s in extraSwarms) StopSwarmSafe(s);
        }

        if (stopWaveSpawnerOnComplete)
            StopWaveSpawnerSafe(ResolveWaveSpawner());

        if (disableSpawnerGOOnComplete && spawnerGOToEnableOnEnter)
            spawnerGOToEnableOnEnter.SetActive(false);

        // (2) 잔류 정리
        if (despawnLeftovers)
        {
            if (despawnDelay > 0f) yield return new WaitForSeconds(despawnDelay);
            DespawnByTag(enemyTag);
            DespawnByTag(projectileTag);
        }

        // (3) UI 확실히 숨김
        if (hideUIOnComplete) SafeHideUI();     // ★ 강제 비활성

        // (4) 내 트리거 비활성 (재진입 방지)
        if (disableOwnTriggerOnComplete && _col)
            _col.enabled = false;

        // (5) 웨이포인트/다음 안내
        if (clearWaypointOnComplete) WaypointDirector.Clear();

        if (showNextWaypointOnComplete && nextWaypointUI && nextTarget)
        {
            if (!nextWaypointUI.gameObject.activeSelf) nextWaypointUI.gameObject.SetActive(true);
            if (nextWaypointUI.canvas && !nextWaypointUI.canvas.gameObject.activeSelf) nextWaypointUI.canvas.gameObject.SetActive(true);
            WaypointDirector.EnableHints();
            WaypointDirector.Show(nextWaypointUI, nextTarget, nextMessage);
        }

        // (6) 다음 트리거 활성화 (부모→자식 순서)
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
                Debug.Log($"[HoldZoneMission] 다음 트리거 활성화: {nextTriggerToActivate.name}");
            }
        }

        onCompleted?.Invoke();
    }

    // ───── 헬퍼 ─────
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
        var stack = new System.Collections.Generic.Stack<Transform>();
        var t = leaf.transform;
        while (t != null) { stack.Push(t); t = t.parent; }
        while (stack.Count > 0)
        {
            var cur = stack.Pop().gameObject;
            if (!cur.activeSelf) cur.SetActive(true);
        }
    }

    void SafeHideUI()
    {
        if (!ui) return;
        ui.Hide();                           // 내부 Hide
        ui.gameObject.SetActive(false);      // 이중 안전
        var cg = ui.canvasGroup;
        if (cg) cg.alpha = 0f;
    }
}

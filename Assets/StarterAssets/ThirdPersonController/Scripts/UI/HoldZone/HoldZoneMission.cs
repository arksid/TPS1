using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class HoldZoneMission : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("게이지 설정")]
    [Range(0, 100)] public float progressPercent; // 현재 %
    [Range(1, 100)] public float targetPercent = 100f;  // 목표 %
    public float fillPerSec = 25f;   // 구역 안일 때 초당 %
    public float decayPerSec = 15f;  // 구역 밖일 때 초당 %
    public bool clampToZero = true;  // 밖일 때 0 밑으로 내려가지 않게

    [Header("표시/UI")]
    public HoldZoneUI ui;            // 게이지/문구 표시용 (없어도 동작)
    public bool autoShowUIOnEnable = false;
    [TextArea] public string enterMsg = "거점 안에 머물러 게이지를 채우세요!";
    [TextArea] public string leavingMsg = "거점 밖입니다! 안으로 복귀하세요!";
    [TextArea] public string completeMsg = "거점 확보 완료!";

    [Header("웨이브 연동")]
    public EnemySwarmDirector swarm;           // 주 스웜 디렉터(있으면 중지)
    public EnemySwarmDirector[] extraSwarms;   // 추가로 중지할 디렉터들(선택)
    public bool stopSwarmOnComplete = true;

    [Header("완료 시 다음 진행(웨이포인트)")]
    public bool clearWaypointOnComplete = true;
    public bool showNextWaypointOnComplete = true;
    public SimpleWaypointUI nextWaypointUI;
    public Transform nextTarget;                      // 다음 지역(보스방 문/트리거)
    [TextArea] public string nextMessage = "보스 방으로 이동!";

    [Header("완료 후 다음 트리거 활성화(보스방)")]
    public bool activateNextTriggerOnComplete = true; // ← 추가
    public Transform nextTriggerToActivate;           // ← 추가 (예: BossDoorTrigger)
    [Tooltip("보스방 트리거의 부모 컨테이너가 꺼져 있으면 먼저 이걸 켭니다.")]
    public GameObject nextTriggerRoot;                // ← 추가 (선택)

    [Header("완료 시 UI 처리")]
    public bool hideUIOnComplete = true;

    [Header("잔류 오브젝트 정리(옵션)")]
    public bool despawnLeftovers = true;
    public string enemyTag = "Enemy";
    public string projectileTag = "EnemyProjectile";
    public float despawnDelay = 0f;

    [Header("완료 이벤트(추가 훅)")]
    public UnityEvent onCompleted;

    bool _playerInside;
    bool _completed;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    // 플레이어가 이미 안에 있는 상태로 간주(트리거 겹침/워프 대비)
    public void ForceEnter()
    {
        var wasCompleted = _completed;
        _playerInside = true;
        if (ui) ui.Show();
        if (wasCompleted) return;
    }
    public void ForceExit() => _playerInside = false;

    void OnEnable()
    {
        _completed = false;

        if (ui)
        {
            ui.SetProgress(targetPercent > 0.0001f ? progressPercent / targetPercent : 1f);
            ui.SetHint(enterMsg);

            if (autoShowUIOnEnable) ui.Show();
            else ui.Hide();
        }
    }

    void Update()
    {
        if (_completed) return;

        float dt = Time.deltaTime;
        if (_playerInside) progressPercent += fillPerSec * dt;
        else progressPercent -= decayPerSec * dt;

        if (clampToZero && progressPercent < 0f) progressPercent = 0f;
        if (progressPercent > targetPercent) progressPercent = targetPercent;

        if (ui) ui.SetProgress(targetPercent > 0.0001f ? progressPercent / targetPercent : 1f);
        if (ui) ui.SetHint(_playerInside ? enterMsg : leavingMsg);

        if (progressPercent >= targetPercent)
        {
            _completed = true;
            if (ui)
            {
                ui.SetProgress(1f);
                ui.SetHint(completeMsg);
            }
            StartCoroutine(Co_CompleteSequence());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = true;
        if (ui) ui.Show();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = false;
    }

    IEnumerator Co_CompleteSequence()
    {
        // 0) 한 프레임 양보(다른 트리거/디렉터 OnEnable 먼저)
        yield return null;

        // 1) 스폰 즉시 중지
        if (stopSwarmOnComplete)
        {
            StopSwarmSafe(swarm);
            if (extraSwarms != null)
                foreach (var s in extraSwarms) StopSwarmSafe(s);
        }

        // 2) 잔류 적/투사체 정리
        if (despawnLeftovers)
        {
            if (despawnDelay > 0f) yield return new WaitForSeconds(despawnDelay);
            DespawnByTag(enemyTag);
            DespawnByTag(projectileTag);
        }

        // 3) 홀드 UI 숨김
        if (hideUIOnComplete && ui) ui.Hide();

        // 4) 표식/아웃라인 정리
        if (clearWaypointOnComplete)
            WaypointDirector.Clear();

        // 5) 다음 웨이포인트 표시(보스방)
        if (showNextWaypointOnComplete && nextWaypointUI && nextTarget)
        {
            if (!nextWaypointUI.gameObject.activeSelf)
                nextWaypointUI.gameObject.SetActive(true);
            if (nextWaypointUI.canvas && !nextWaypointUI.canvas.gameObject.activeSelf)
                nextWaypointUI.canvas.gameObject.SetActive(true);

            WaypointDirector.EnableHints();
            WaypointDirector.Show(nextWaypointUI, nextTarget, nextMessage);
        }

        // 6) 다음 트리거(보스방) 확실히 켜기 (부모 → 본체)
        if (activateNextTriggerOnComplete)
        {
            if (nextTriggerRoot && !nextTriggerRoot.activeSelf)
            {
                nextTriggerRoot.SetActive(true);
                Debug.Log("[HoldZoneMission] nextTriggerRoot 활성화");
                // 부모가 켜진 다음 프레임에 자식 트리거를 켜도록 살짝 대기
                yield return null;
            }

            if (nextTriggerToActivate)
            {
                if (!nextTriggerToActivate.gameObject.activeSelf)
                {
                    nextTriggerToActivate.gameObject.SetActive(true);
                    Debug.Log($"[HoldZoneMission] 보스방 트리거 활성화: {nextTriggerToActivate.name}");
                }
            }
            else
            {
                Debug.LogWarning("[HoldZoneMission] nextTriggerToActivate 미지정(Transform). 인스펙터 연결 필요!");
            }
        }

        // 7) 추가 훅
        onCompleted?.Invoke();
    }

    void StopSwarmSafe(EnemySwarmDirector s)
    {
        if (!s) return;
        var mEnd = s.GetType().GetMethod("EndWave",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (mEnd != null) { mEnd.Invoke(s, null); return; }
        s.StopAllCoroutines();
    }

    void DespawnByTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return;
        GameObject[] arr;
        try { arr = GameObject.FindGameObjectsWithTag(tagName); }
        catch { return; }

        foreach (var go in arr) if (go) Destroy(go);
    }
}

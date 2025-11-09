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
    public bool autoShowUIOnEnable = false;   // OnEnable 때 자동 표시할지
    [TextArea] public string enterMsg = "거점 안에 머물러 게이지를 채우세요!";
    [TextArea] public string leavingMsg = "거점 밖입니다! 안으로 복귀하세요!";
    [TextArea] public string completeMsg = "거점 확보 완료!";

    [Header("웨이브 연동")]
    public EnemySwarmDirector swarm;           // 주 스웜 디렉터(있으면 중지)
    public EnemySwarmDirector[] extraSwarms;   // 추가로 중지할 디렉터들(선택)
    public bool stopSwarmOnComplete = true;

    [Header("완료 시 다음 진행(웨이포인트)")]
    public bool clearWaypointOnComplete = true;       // 기존 마커/아웃라인 정리
    public bool showNextWaypointOnComplete = true;    // 다음 웨이포인트 표기
    public SimpleWaypointUI nextWaypointUI;           // 다음 미션 안내용 UI
    public Transform nextTarget;                      // 다음 지역(트리거)의 Transform
    [TextArea] public string nextMessage = "다음 지역으로 이동!";

    [Header("완료 시 UI 처리")]
    public bool hideUIOnComplete = true;   // 이 미션의 HoldZoneUI 숨김

    [Header("잔류 오브젝트 정리(옵션)")]
    public bool despawnLeftovers = true;   // 잔류 적/투사체 제거
    public string enemyTag = "Enemy";      // 적 태그
    public string projectileTag = "EnemyProjectile"; // 적 투사체 태그
    public float despawnDelay = 0f;        // (선택) 제거 전 딜레이

    [Header("완료 이벤트(추가 훅)")]
    public UnityEvent onCompleted;         // 100% 도달 시(마지막에 호출, 한 번만)

    bool _playerInside;
    bool _completed;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    // 클래스 안 아무 곳에 추가
    public void ForceEnter()
    {
        // 플레이어가 이미 안에 있는 상태로 간주
        // UI도 보이게!
        var wasCompleted = _completed; // 완주 후 중복 호출 방어용
        _playerInside = true;
        if (ui) ui.Show();
        if (wasCompleted) return;
    }
    public void ForceExit()
    {
        _playerInside = false;
        // 나갈 때 UI를 굳이 숨길지는 프로젝트 규칙에 맞춰 판단
    }
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

        // 플레이어가 진입하면 UI는 보이도록 (자동표시가 꺼져 있어도)
        if (ui) ui.Show();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = false;
    }

    IEnumerator Co_CompleteSequence()
    {
        // 0) 한 프레임 양보(다른 트리거/디렉터 OnEnable 초기화 먼저 돌게)
        yield return null;

        // 1) 스폰 즉시 중지
        if (stopSwarmOnComplete)
        {
            StopSwarmSafe(swarm);
            if (extraSwarms != null)
            {
                foreach (var s in extraSwarms) StopSwarmSafe(s);
            }
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

        // 5) 다음 웨이포인트 표시(안내)
        if (showNextWaypointOnComplete && nextWaypointUI && nextTarget)
        {
            // 표시 직전, UI/캔버스 활성 보장
            if (!nextWaypointUI.gameObject.activeSelf)
                nextWaypointUI.gameObject.SetActive(true);
            if (nextWaypointUI.canvas && !nextWaypointUI.canvas.gameObject.activeSelf)
                nextWaypointUI.canvas.gameObject.SetActive(true);

            WaypointDirector.EnableHints();
            WaypointDirector.Show(nextWaypointUI, nextTarget, nextMessage);
        }

        // 6) 추가 훅
        onCompleted?.Invoke();
    }

    void StopSwarmSafe(EnemySwarmDirector s)
    {
        if (!s) return;

        // 우선 EndWave()가 있으면 호출
        var mEnd = s.GetType().GetMethod("EndWave",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (mEnd != null)
        {
            mEnd.Invoke(s, null);
            return;
        }

        // 없으면 StopAllCoroutines()로라도 정지
        s.StopAllCoroutines();
        // 필요 시 비활성화까지
        // s.enabled = false;
        // s.gameObject.SetActive(false);
    }

    void DespawnByTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return;
        GameObject[] arr;
        try { arr = GameObject.FindGameObjectsWithTag(tagName); }
        catch { return; } // 해당 태그가 없으면 무시

        foreach (var go in arr)
        {
            if (!go) continue;
            // 폭발/사망 연출이 있으면 여기서 트리거하고 Destroy로 변경 가능
            Destroy(go);
        }
    }
}

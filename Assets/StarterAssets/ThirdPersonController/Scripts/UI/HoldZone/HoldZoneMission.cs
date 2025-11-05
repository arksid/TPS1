using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class HoldZoneMission : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("게이지 설정")]
    [Range(0, 100)] public float progressPercent; // 현재 %
    [Range(0, 100)] public float targetPercent = 100f;  // 목표 %
    public float fillPerSec = 25f;   // 구역 안일 때 초당 %
    public float decayPerSec = 15f;  // 구역 밖일 때 초당 %
    public bool clampToZero = true;  // 밖일 때 0 밑으로 내려가지 않게

    [Header("표시/UI")]
    public HoldZoneUI ui;            // 게이지/문구 표시용 (없어도 동작)
    [TextArea] public string enterMsg = "거점 안에 머물러 게이지를 채우세요!";
    [TextArea] public string leavingMsg = "거점 밖입니다! 안으로 복귀하세요!";
    [TextArea] public string completeMsg = "거점 확보 완료!";

    [Header("웨이브 연동")]
    public EnemySwarmDirector swarm; // 현재 웨이브 디렉터
    public bool stopSwarmOnComplete = true;

    [Header("완료 이벤트")]
    public UnityEvent onCompleted;   // 100% 도달 시 발생(한 번만)

    [Header("UI 옵션")]
    public bool autoShowUIOnEnable = false;

    bool _playerInside;
    bool _completed;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        _completed = false;
        if (ui) ui.SetProgress(progressPercent / targetPercent);
        if (ui) ui.SetHint(enterMsg);

        if (ui)
        {
            if (autoShowUIOnEnable) ui.Show();
            else ui.Hide(); // ← 기본은 숨김
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

        // 힌트 문구 갱신(선택)
        if (ui)
        {
            ui.SetHint(_playerInside ? enterMsg : leavingMsg);
        }

        // 완료 체크
        if (progressPercent >= targetPercent)
        {
            _completed = true;
            if (ui)
            {
                ui.SetProgress(1f);
                ui.SetHint(completeMsg);
            }

            if (stopSwarmOnComplete && swarm)
                swarm.RequestStopWaves(); // ⬅️ 3) EnemySwarmDirector 쪽 보강 필요

            onCompleted?.Invoke();

            // 필요 시 여기서 WaypointDirector.Clear(), 다음 트리거 활성화 등 체인 처리
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = false;
    }
}

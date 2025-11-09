using System.Collections;
using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(Collider))]
public class HoldZoneTrigger : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("연결")]
    public SimpleWaypointUI waypointUI; // 마커 UI
    public Transform holdZone;          // 거점의 가시 오브젝트(아웃라인 대상). 비우면 자기 transform
    public HoldZoneMission mission;     // 같은 구역의 미션 컴포넌트
    public EnemySwarmDirector swarm;    // 웨이브 디렉터

    [Header("표시 문구")]
    [TextArea] public string message = "지정된 구역으로 이동해 거점을 유지하세요!";

    [Header("동작 옵션")]
    public bool hideWaypointUIOnEnable = true; // 트리거 켜지면 UI만 끔(아웃라인 유지)
    public bool clearOnEnter = true;           // 들어오면 표식 완전 정리
    public bool startSwarmOnEnable = true;     // 켜지는 순간 웨이브 시작(튜토리얼 완료 이후에만)
    public bool startSwarmOnEnter = false;    // 들어오면 시작

    [Header("플레이어/근접 시작(보조)")]
    public Transform player;                  // 비워두면 Tag=Player 자동 탐색
    public bool useProximityStart = true;     // 트리거가 안 먹는 경우를 위해 근접으로도 시작
    public float startRadius = 3f;            // 이 거리 안에 들어오면 시작

    // 필드 추가(클래스 상단)
    [Header("웜 스타트(활성화 시 겹침 체크)")]
    public bool warmStartOnEnable = true;
    public float warmStartDelay = 0.05f;   // 한 프레임 정도 여유
    [Header("스웜 시작 옵션")]
    public bool allowRestart = true;         // 이미 시작 상태여도 강제 재시작 허용
    public bool resetSwarmFlagOnEnable = true; // 트리거가 켜질 때 내부 시작 플래그 초기화

    bool _fired;
    bool _swarmStarted;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    void Start()
    {

        Debug.Log("[HZT] Start");
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (useProximityStart && !_swarmStarted && player != null)
        {
            var target = holdZone ? holdZone.position : transform.position;
            if ((player.position - target).sqrMagnitude <= startRadius * startRadius)
            {
                Debug.Log("[HoldZoneTrigger] 근접 시작 조건 충족 → 웨이브 시작");
                TryStartSwarmOnce();
            }
        }
    }


    void OnEnable()
    {
        Debug.Log("[HZT] OnEnable");

        if (resetSwarmFlagOnEnable) _swarmStarted = false;

        // UI만 숨김(아웃라인은 유지)
        if (hideWaypointUIOnEnable)
            WaypointDirector.HideUIOnly();

        // (선택) 플레이어 유도 표시는 힌트가 켜져 있을 때만
        var target = holdZone ? holdZone : transform;
        if (WaypointDirector.HintsEnabled)
            WaypointDirector.Show(waypointUI, target, message);

        // ★ 수정: 힌트와 무관하게 웨이브 시작 가능
        if (startSwarmOnEnable)
            TryStartSwarmOnce();

        if (warmStartOnEnable) StartCoroutine(Co_WarmStartOverlapCheck());
    }
    System.Collections.IEnumerator Co_WarmStartOverlapCheck()
    {
        // 1프레임 양보 (SetActive(true) 직후 초기화들 먼저 돌도록)
        yield return new WaitForSeconds(warmStartDelay);

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }
        var col = GetComponent<Collider>();
        if (!col || !col.enabled || player == null) yield break;

        // ★ '이미 안에 있는지' 판정: ClosestPoint가 자기 자신이면 내부로 간주
        Vector3 pp = player.position + Vector3.up * 0.2f; // 살짝 띄워 샘플
        bool inside = (col.ClosestPoint(pp) - pp).sqrMagnitude < 0.000001f;

        if (inside)
        {
            Debug.Log("[HoldZoneTrigger] WarmStart: 플레이어가 이미 영역 안 → 강제 진입 처리");

            // 1) (선택) 웨이포인트/아웃라인 정리 규칙이 있다면 여기서 실행
            // if (clearOnEnter) WaypointDirector.Clear();  // 프로젝트 설정에 맞게

            // 2) 미션 게이지 로직 강제 시작
            if (mission) mission.ForceEnter();

            // 3) 웨이브 시작 (한 번만)
            TryStartSwarmOnce();
        }
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[HZT] OnTriggerEnter: {other.tag}");
        if (!other.CompareTag(playerTag)) return;

        if (clearOnEnter)
            WaypointDirector.Clear(); // 표식 정리

        // ★ 추가: 거점 게이지 강제 시작 (미션이 분리 오브젝트여도 OK)
        if (mission) mission.ForceEnter();

        if (!_fired)
        {
            _fired = true;
            if (startSwarmOnEnter)
                TryStartSwarmOnce();

            if (mission && mission.ui) mission.ui.Show();
        }
    }


    void TryStartSwarmOnce()
    {
        GameFlowRunner.Run(Co_StartSwarmNow());
    }

    System.Collections.IEnumerator Co_StartSwarmNow()
    {
        if (!swarm)
        {
            Debug.LogWarning("[HoldZoneTrigger] swarm 미지정");
            yield break;
        }

        // 0) 스웜 오브젝트/컴포넌트 활성 보장
        if (!swarm.gameObject.activeSelf) swarm.gameObject.SetActive(true);
        if (!swarm.enabled) swarm.enabled = true;

        // 1) 플레이어 레퍼런스가 필요한 디렉터라면 자동 주입(필드명/프로퍼티 명 흔한 패턴 지원)
        if (player == null)
        {
            var pGo = GameObject.FindGameObjectWithTag(playerTag);
            if (pGo) player = pGo.transform;
        }
        var t = swarm.GetType();
        var fPlayer = t.GetField("player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var pPlayer = t.GetProperty("player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (fPlayer != null && fPlayer.GetValue(swarm) == null && player) fPlayer.SetValue(swarm, player);
        if (pPlayer != null && pPlayer.CanWrite && pPlayer.GetValue(swarm) == null && player) pPlayer.SetValue(swarm, player, null);

        // 2) 스폰 포인트/웨이브 구성 점검 (0개면 바로 로그로 원인 파악)
        int spCount = -1, waveCount = -1;
        var fSP = t.GetField("spawnPoints", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (fSP != null) { var arr = fSP.GetValue(swarm) as Transform[]; spCount = (arr == null ? -1 : arr.Length); }

        var fWaves = t.GetField("waves", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (fWaves != null) { var list = fWaves.GetValue(swarm) as System.Collections.ICollection; waveCount = (list == null ? -1 : list.Count); }
        Debug.Log($"[HoldZoneTrigger] 스웜 점검: spawnPoints={spCount}, waves={waveCount}");

        if (spCount == 0 || waveCount == 0)
        {
            Debug.LogWarning("[HoldZoneTrigger] 스폰포인트/웨이브가 비었습니다. EnemySwarmDirector 인스펙터 연결을 확인하세요.");
            yield break;
        }

        // 3) 이미 돌았던 스웜이면 안전하게 정지 후 재시작
        if (_swarmStarted && !allowRestart)
        {
            Debug.Log("[HoldZoneTrigger] 이미 시작됨(allowRestart=false) → 재시작 안함");
            yield break;
        }

        // EndWave가 있으면 먼저 호출해 잔류 상태 초기화
        var endWave = t.GetMethod("EndWave",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (endWave != null)
        {
            endWave.Invoke(swarm, null);
            Debug.Log("[HoldZoneTrigger] EndWave() 호출 (재시작 준비)");
            // 한 틱 쉬고 시작(코루틴/상태 정리 시간)
            yield return null;
        }
        else
        {
            // 최소한 모든 코루틴은 중지
            swarm.StopAllCoroutines();
        }

        // 4) 실제 시작: StartWaves() 또는 RunWaves() 중 있는 걸로
        var startWaves = t.GetMethod("StartWaves",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var runWaves = t.GetMethod("RunWaves",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        if (startWaves != null)
        {
            startWaves.Invoke(swarm, null);
            _swarmStarted = true;
            Debug.Log("[HoldZoneTrigger] StartWaves() 호출 → 웨이브 시작");
            yield break;
        }

        if (runWaves != null)
        {
            var routine = runWaves.Invoke(swarm, null) as System.Collections.IEnumerator;
            if (routine != null)
            {
                GameFlowRunner.Run(routine);
                _swarmStarted = true;
                Debug.Log("[HoldZoneTrigger] RunWaves() 코루틴 시작");
                yield break;
            }
        }

        Debug.LogWarning("[HoldZoneTrigger] StartWaves()/RunWaves() 둘 다 없음 → EnemySwarmDirector에 시작 API를 추가하세요.");
    }


}

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossMovementController : MonoBehaviour
{
    public enum MoveMode { Idle, Approach, KeepDistance, Circle, Patrol, DashToPlayer }

    [Header("기본 참조")]
    public NavMeshAgent agent;
    public Transform player;                    // 비워두면 Tag=Player 자동 탐색

    [Header("일반 이동")]
    public MoveMode defaultMode = MoveMode.KeepDistance;
    public float replanInterval = 0.2f;         // 목적지 재계산 주기
    public float stopDistance = 10f;            // Approach 모드 정지 거리

    [Header("거리 유지(링 유지)")]
    public float keepMin = 9f;                  // 너무 가까우면 뒤로 빠짐
    public float keepMax = 13f;                 // 너무 멀면 당김
    public float keepPrefer = 11f;              // 선호 반경

    [Header("원형 선회")]
    public float circleRadius = 12f;            // 원형 선회 반경
    public float circleAngularSpeed = 60f;      // 도/초 (양수=시계, 음수=반시계)

    [Header("대시(짧게 돌진)")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.9f;
    public float dashOvershoot = 2f;            // 플레이어를 살짝 지나치게

    [Header("웨이포인트 순찰")]
    public Transform[] waypoints;
    public bool patrolLoop = true;
    public float waypointReachDist = 1.0f;

    [Header("회전(머리 땅 박힘 방지)")]
    public float faceRotateSpeed = 8f;          // 우리가 직접 수평만 회전
    public bool yawOnly = true;                 // 수평(Yaw)만
    public bool keepUpright = true;             // Pitch/Roll 강제 0

    [Header("안전 옵션/디버그")]
    public bool autoSampleStartOnNavmesh = true;
    public bool enableLogs = false;

    // BossMovementController.cs (필드)
    [Header("회전(정확히 플레이어 보기)")]
    public float faceYawDegPerSec = 720f;   // 초당 회전 최대각(권장 540~1080)
    public float snapEpsilon = 1f;          // 1도 이하이면 목표각으로 스냅


    MoveMode _mode;
    int _patrolIndex;
    float _lastPlanTime;
    float _circleAngle;
    Coroutine _dashCo;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();

        // ★ 중요: Agent는 "이동만" 담당, 회전은 우리가 직접 처리
        agent.updatePosition = true;
        agent.updateRotation = false;

        if (autoSampleStartOnNavmesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                transform.position = hit.position;
        }
    }

    void OnEnable()
    {
        SetMode(defaultMode, true);
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (!agent || !agent.enabled) return;

        // 플레이어 참조 없으면 주기적으로 찾기
        if (!player)
        {
            if (Time.frameCount % 30 == 0)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p) player = p.transform;
            }
            return;
        }

        // 일정 간격으로만 목적지 갱신
        if (Time.time - _lastPlanTime >= replanInterval)
        {
            _lastPlanTime = Time.time;

            switch (_mode)
            {
                case MoveMode.Idle:
                    agent.isStopped = true;
                    break;

                case MoveMode.Approach:
                    DoApproach();
                    break;

                case MoveMode.KeepDistance:
                    DoKeepDistance();
                    break;

                case MoveMode.Circle:
                    DoCircle();
                    break;

                case MoveMode.Patrol:
                    DoPatrol();
                    break;

                case MoveMode.DashToPlayer:
                    // 대시는 코루틴으로 진행, 대시 중이 아니면 접근 비슷하게 유지
                    if (_dashCo == null) DoApproach();
                    break;
            }
        }

        // ★ 항상 "수평만" 플레이어를 보게 회전 (Agent 회전 대신)
        FacePlayerYawOnly();
    }

    // ========== 외부 제어 API ==========
    public void SetMode(MoveMode mode, bool reset = false)
    {
        _mode = mode;
        if (enableLogs) Debug.Log($"[BossMove] Mode → {_mode}");

        if (reset)
        {
            _patrolIndex = 0;
            _circleAngle = 0f;
        }

        if (_mode == MoveMode.DashToPlayer && _dashCo == null)
            _dashCo = StartCoroutine(Co_DashOnce());
    }

    public void DashNow() => SetMode(MoveMode.DashToPlayer);

    // ========== 모드 구현 ==========
    void DoApproach()
    {
        agent.isStopped = false;
        var dst = Vector3.Distance(transform.position, player.position);
        if (dst <= stopDistance)
        {
            agent.isStopped = true;
            return;
        }
        SetDestinationSafe(player.position);
    }

    void DoKeepDistance()
    {
        var pos = transform.position;
        var ppos = player.position;
        var toPlayer = ppos - pos;
        float d = toPlayer.magnitude;

        if (d < keepMin - 0.1f)
        {
            // 너무 가까움 → 플레이어 반대 방향의 링 중심점으로 이동
            var dir = (pos - ppos).normalized;
            var target = ppos + dir * keepPrefer;
            agent.isStopped = false;
            SetDestinationSafe(target);
        }
        else if (d > keepMax + 0.1f)
        {
            // 너무 멀음 → 링으로 당김
            var dir = (ppos - pos).normalized;
            var target = ppos - dir * keepPrefer;
            agent.isStopped = false;
            SetDestinationSafe(target);
        }
        else
        {
            // 범위 내 → 정지(사격 안정)
            agent.isStopped = true;
        }
    }

    void DoCircle()
    {
        // 현재 각도를 증가/감소
        _circleAngle += circleAngularSpeed * Mathf.Deg2Rad * replanInterval;

        // 플레이어 기준 원 위의 목표 지점
        var center = player.position;
        var offset = new Vector3(Mathf.Cos(_circleAngle), 0, Mathf.Sin(_circleAngle)) * circleRadius;
        var target = center + offset;

        agent.isStopped = false;
        SetDestinationSafe(target);
    }

    void DoPatrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        var target = waypoints[_patrolIndex] ? waypoints[_patrolIndex].position : transform.position;
        agent.isStopped = false;
        SetDestinationSafe(target);

        if (Vector3.Distance(transform.position, target) <= waypointReachDist)
        {
            _patrolIndex++;
            if (_patrolIndex >= waypoints.Length)
                _patrolIndex = patrolLoop ? 0 : waypoints.Length - 1;
        }
    }

    IEnumerator Co_DashOnce()
    {
        // 대시 직전 에이전트 파라미터 백업
        float prevSpeed = agent.speed;
        float prevAcc = agent.acceleration;
        bool prevAutoBraking = agent.autoBraking;

        agent.autoBraking = false;
        agent.speed = dashSpeed;
        agent.acceleration = dashSpeed * 2f;

        float t = 0f;
        while (t < dashDuration && player)
        {
            // 플레이어 현재 위치를 따라가되 살짝 오버슈트
            var dir = (player.position - transform.position).normalized;
            var target = player.position + dir * dashOvershoot;
            agent.isStopped = false;
            SetDestinationSafe(target);

            t += Time.deltaTime;
            yield return null;
        }

        // 복귀
        agent.speed = prevSpeed;
        agent.acceleration = prevAcc;
        agent.autoBraking = prevAutoBraking;

        _dashCo = null;

        // 대시 후 기본 모드로 복귀
        SetMode(defaultMode);
    }

    // ========== 유틸리티 ==========
    void SetDestinationSafe(Vector3 worldPos)
    {
        if (NavMesh.SamplePosition(worldPos, out var hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(worldPos); // 그래도 시도
    }

    void FacePlayerYawOnly()
    {
        if (!player) return;

        // 플레이어까지의 수평 방향
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        float targetYaw = Quaternion.LookRotation(dir, Vector3.up).eulerAngles.y;
        float curYaw = transform.eulerAngles.y;
        float delta = Mathf.DeltaAngle(curYaw, targetYaw);

        // 각속도 기반 회전 + 스냅(언더턴 방지)
        float step = faceYawDegPerSec * Time.deltaTime;
        if (Mathf.Abs(delta) <= Mathf.Max(step, snapEpsilon))
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, curYaw + Mathf.Sign(delta) * step, 0f);

        // 피치/롤 고정
        if (keepUpright)
        {
            var e = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }
    }




    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((player ? player.position : transform.position), keepMin);
        Gizmos.DrawWireSphere((player ? player.position : transform.position), keepMax);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere((player ? player.position : transform.position), circleRadius);
    }
}

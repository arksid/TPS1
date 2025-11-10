using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class BossPatternDirector : MonoBehaviour
{
    [Header("필수 참조")]
    public Transform player;
    public NavMeshAgent agent;                  // 있으면 사용
    public Animator animator;                   // 점프 트리거가 있을 때 사용
    public string jumpTriggerName = "JumpBack";

    [Header("연동(있으면 연결)")]
    public BossMonster boss;                    // HP 이벤트 수신(있으면 자동 구독)
    public BossWeaponMinigun minigun;           // 분노 플래그 연동
    public BossWeaponMissile missile;           // 패턴/분노 연동

    [Header("무기 제어 이벤트")]
    public UnityEvent onMinigunStart;
    public UnityEvent onMinigunStop;
    public UnityEvent onMissileBarrage;

    // ---------------- 이동/속도 프로파일 ----------------
    [Header("속도 프로파일")]
    public float farSpeed = 3.2f;        // 멀리 있을 때 속도
    public float nearSpeed = 2.2f;       // 가까워질수록 천천히
    public float nearDistance = 18f;     // 이 거리 이내면 nearSpeed
    public float navAcceleration = 8f;   // 가속도
    public float navAngularSpeed = 540f; // 회전 속도
    public float manualMoveSpeed = 3.0f; // 에이전트 없을 때 기본 속도
    float _curManualSpeed = 0f;

    [Header("정지 임계(너무 붙는 것 방지)")]
    public float stopDistance = 10f;     // 절대 붙지 않을 최소 거리

    // ---------------- 스트레이프(원형 이동) ----------------
    [Header("스트레이프(좌우 원형 이동)")]
    public bool enableStrafe = true;
    public float strafeRadius = 12f;       // 플레이어 기준 유지하고 싶은 반경
    public float strafeTolerance = 2f;     // 반경 허용 오차(±)
    public float strafeAhead = 4f;         // 원 둘레를 따라 목표점을 약간 앞에 두는 거리
    public float strafeSpeedMul = 0.85f;   // 원형 이동 시 속도 배율( farSpeed * 이 값 )
    public float strafeSwitchInterval = 2.5f; // 방향 전환 주기
    public float strafeObstacleCheck = 2.0f;  // 전방 장애물 감지 거리
    int _strafeDir = 1;                    // +1: 오른쪽으로 회전, -1: 왼쪽
    float _strafeTimer;

    // ---------------- 점프/미사일 패턴 ----------------
    [Header("점프 이탈")]
    public float jumpBackDistance = 18f;
    public float jumpAirTime = 0.9f;
    public float jumpArcHeight = 4f;
    public float afterJumpDelay = 0.25f;

    [Header("미사일 연사")]
    public float missileDuration = 2.0f;
    public float afterMissileCooldown = 0.6f;

    [Header("누적 데미지 트리거")]
    public bool usePercent = true;                // true: 퍼센트, false: 절대값
    [Range(0.05f, 0.5f)] public float damageChunkPercent = 0.18f;
    public float damageChunkFlat = 150f;


    [System.Serializable]
    public class Phase
    {
        [Range(0f, 1f)] public float enterAtHpRatio = 0.7f;
        public bool enragedWeapons = false;
        public BossWeaponMissile.Pattern missilePattern = BossWeaponMissile.Pattern.AimedSalvo;
        public float agentSpeed = 3.2f;
        public float newStopDistance = 10f;
        public float damageChunkPercentOverride = -1f; // 음수면 기본값
    }
    public Phase[] phases;

    [Header("디버그")]
    public bool logEnabled = false;

    enum State { ChaseAndSpray, JumpBack, Missile, Cooldown, Stagger }
    State _state = State.ChaseAndSpray;

    float _lastPlan;
    float _snapEps = 1f;

    // HP/누적데미지
    float _prevHp;
    float _accDamage;
    bool _isSequenceRunning, _isStagger;
    int _phaseIndex = -1;

    // ---------------- 라이프사이클 ----------------
    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        _strafeDir = (Random.value < 0.5f) ? 1 : -1;
        _strafeTimer = Random.Range(strafeSwitchInterval * 0.7f, strafeSwitchInterval * 1.3f);

        if (agent)
        {
            agent.speed = farSpeed;
            agent.acceleration = navAcceleration;
            agent.angularSpeed = navAngularSpeed;
        }

        onMinigunStart?.Invoke();

        if (boss)
        {
            _prevHp = boss.currentHP;
            boss.onHpChanged.AddListener(OnBossHpChanged); // (current,max)
        }
    }

    void OnDestroy()
    {
        if (boss) boss.onHpChanged.RemoveListener(OnBossHpChanged);
    }

    void Update()
    {
        if (!player) return;
        if (_state == State.ChaseAndSpray && !_isSequenceRunning && !_isStagger)
            TickChase();
    }

    // ---------------- 메인 로직 ----------------
    void TickChase()
    {
        FacePlayerHard();

        float dist = Vector3.Distance(transform.position, player.position);
        bool inStrafeBand = enableStrafe &&
                            dist > Mathf.Max(stopDistance, strafeRadius - strafeTolerance) &&
                            dist < (strafeRadius + strafeTolerance);

        if (Time.time - _lastPlan < 0.05f) return; // 너무 자주 SetDestination 방지
        _lastPlan = Time.time;

        if (inStrafeBand)
        {
            DoStrafe(dist);
        }
        else
        {
            DoApproach(dist);
        }
    }

    // 가까우면 원형 이동
    void DoStrafe(float dist)
    {
        // 좌우 전환 타이머/장애물 체크
        _strafeTimer -= Time.deltaTime;
        Vector3 toPlayer = (player.position - transform.position); toPlayer.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer.normalized) * _strafeDir;

        bool blocked = Physics.Raycast(transform.position + Vector3.up * 1f, tangent, strafeObstacleCheck, ~0, QueryTriggerInteraction.Ignore);
        if (_strafeTimer <= 0f || blocked)
        {
            _strafeDir *= -1;
            _strafeTimer = Random.Range(strafeSwitchInterval * 0.7f, strafeSwitchInterval * 1.3f);
        }

        // 반경 유지용 목표점(플레이어 주변 원 위의 지점 + 약간 앞쪽)
        Vector3 ringPos = player.position + toPlayer.normalized * Mathf.Clamp(strafeRadius, stopDistance + 0.5f, 999f);
        Vector3 dest = ringPos + tangent.normalized * strafeAhead;

        // 반경 오류 보정(너무 가까우면 바깥으로, 멀면 안쪽으로 살짝)
        float radialErr = dist - strafeRadius;                 // +면 멀다 / -면 가깝다
        dest += (-toPlayer.normalized) * Mathf.Clamp(radialErr, -1.5f, 1.5f);

        float targetSpeed = Mathf.Max(0.5f, farSpeed * strafeSpeedMul);

        if (agent && agent.enabled)
        {
            agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, navAcceleration * Time.deltaTime);
            agent.acceleration = navAcceleration;
            agent.angularSpeed = navAngularSpeed;
            agent.isStopped = false;

            if (NavMesh.SamplePosition(dest, out var hit, 2f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            else
                agent.SetDestination(dest);
        }
        else
        {
            // 수동 이동 (부드럽게)
            float accel = navAcceleration;
            _curManualSpeed = Mathf.MoveTowards(_curManualSpeed, targetSpeed, accel * Time.deltaTime);
            transform.position += (dest - transform.position).normalized * _curManualSpeed * 0.1f; // 0.1은 프레임기반 보정
        }
    }

    // 멀리 있거나 너무 가까우면 접근/이탈
    void DoApproach(float dist)
    {
        float targetSpeed = (dist > nearDistance) ? farSpeed : nearSpeed;

        if (agent && agent.enabled)
        {
            Vector3 dest = player.position;
            if (NavMesh.SamplePosition(dest, out var hit, 2f, NavMesh.AllAreas))
                dest = hit.position;

            agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, navAcceleration * Time.deltaTime);
            agent.acceleration = navAcceleration;
            agent.angularSpeed = navAngularSpeed;

            // 너무 붙지 않도록
            agent.isStopped = (dist <= stopDistance);
            if (!agent.isStopped) agent.SetDestination(dest);
        }
        else
        {
            Vector3 to = player.position - transform.position; to.y = 0f;
            float tgt = (dist > stopDistance) ? targetSpeed : 0f;
            _curManualSpeed = Mathf.MoveTowards(_curManualSpeed, tgt, navAcceleration * Time.deltaTime);
            if (_curManualSpeed > 0.001f)
                transform.position += to.normalized * _curManualSpeed * 0.1f;
        }
    }

    void FacePlayerHard()
    {
        Vector3 dir = player.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;

        float targetYaw = Quaternion.LookRotation(dir, Vector3.up).eulerAngles.y;
        float curYaw = transform.eulerAngles.y;
        float delta = Mathf.DeltaAngle(curYaw, targetYaw);
        float step = navAngularSpeed * Time.deltaTime; // 회전도 Nav 설정과 일치

        if (Mathf.Abs(delta) <= Mathf.Max(step, _snapEps))
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, curYaw + Mathf.Sign(delta) * step, 0f);
    }

    // ---------------- HP 이벤트/페이즈/시퀀스 ----------------
    void OnBossHpChanged(int current, int max)
    {
        float ratio = (max > 0) ? (float)current / max : 0f;
        TryEnterPhaseByRatio(ratio);

        float taken = Mathf.Max(0f, _prevHp - current);
        _prevHp = current;
        if (taken <= 0f) return;

        _accDamage += taken;
        float need = usePercent ? (max * GetActiveChunkPercent()) : damageChunkFlat;

        if (!_isSequenceRunning && !_isStagger && _state == State.ChaseAndSpray && _accDamage >= need)
        {
            _accDamage = 0f;
            StartCoroutine(Co_JumpAndMissile());
        }
    }

    float GetActiveChunkPercent()
    {
        if (_phaseIndex >= 0 && _phaseIndex < phases.Length)
        {
            float ov = phases[_phaseIndex].damageChunkPercentOverride;
            if (ov > 0f) return ov;
        }
        return Mathf.Clamp01(damageChunkPercent);
    }

    void TryEnterPhaseByRatio(float hpRatio)
    {
        int next = _phaseIndex + 1;
        if (next < 0 || next >= phases.Length) return;
        if (hpRatio <= phases[next].enterAtHpRatio) EnterPhase(next);
    }

    void EnterPhase(int idx)
    {
        _phaseIndex = idx;
        var ph = phases[idx];

        stopDistance = ph.newStopDistance;
        farSpeed = ph.agentSpeed; // 프로파일 최댓속도 갱신
        if (agent) agent.speed = farSpeed;

        if (minigun) minigun.SetEnraged(ph.enragedWeapons);
        if (missile)
        {
            missile.currentPattern = ph.missilePattern;
            missile.enraged = ph.enragedWeapons;
        }

        if (logEnabled) Debug.Log($"[Boss] Enter Phase {idx} ({ph.enterAtHpRatio:P0})");
    }

    IEnumerator Co_JumpAndMissile()
    {
        _isSequenceRunning = true;

        onMinigunStop?.Invoke();

        _state = State.JumpBack;
        Vector3 from = transform.position;
        Vector3 awayDir = (transform.position - player.position); awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.001f) awayDir = -transform.forward;
        awayDir.Normalize();
        Vector3 to = from + awayDir * jumpBackDistance;

        if (agent) agent.enabled = false;

        if (animator && !string.IsNullOrEmpty(jumpTriggerName))
            animator.SetTrigger(jumpTriggerName);

        float t = 0f;
        while (t < jumpAirTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / jumpAirTime);
            Vector3 pos = Vector3.Lerp(from, to, u);
            pos.y += Mathf.Sin(u * Mathf.PI) * jumpArcHeight;
            transform.position = pos;
            FacePlayerHard();
            yield return null;
        }

        if (NavMesh.SamplePosition(to, out var hit, 2f, NavMesh.AllAreas))
            transform.position = hit.position;
        else
            transform.position = to;

        yield return new WaitForSeconds(afterJumpDelay);

        _state = State.Missile;
        onMissileBarrage?.Invoke();
        float mt = 0f;
        while (mt < missileDuration)
        {
            mt += Time.deltaTime;
            FacePlayerHard();
            yield return null;
        }
        yield return new WaitForSeconds(afterMissileCooldown);

        if (agent) agent.enabled = true;
        _state = State.ChaseAndSpray;
        onMinigunStart?.Invoke();

        _isSequenceRunning = false;
    }

    // 외부에서 경직 호출 가능(선택)
    public void Stagger(float duration = 1.2f)
    {
        if (gameObject.activeInHierarchy) StartCoroutine(Co_Stagger(duration));
    }
    IEnumerator Co_Stagger(float sec)
    {
        if (_isStagger) yield break;
        _isStagger = true;

        onMinigunStop?.Invoke();
        if (agent) agent.isStopped = true;

        var prev = _state;
        _state = State.Stagger;

        yield return new WaitForSeconds(sec);

        if (agent) agent.isStopped = false;
        _state = State.ChaseAndSpray;
        onMinigunStart?.Invoke();
        _isStagger = false;
    }
}

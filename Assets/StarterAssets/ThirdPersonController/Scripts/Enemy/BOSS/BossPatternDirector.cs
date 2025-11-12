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

    [Header("미사일 페이즈 약점 표시")]
    public string weakPointOnMissileId = "";    // 예: "Core" (비워두면 사용 안 함)
    public bool clearWeakAfterMissile = true;   // 끝나면 전부 끔

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

    // ---------------- 스트레이프(좌우 원형 이동) ----------------
    [Header("스트레이프(좌우 원형 이동)")]
    public bool enableStrafe = true;
    public float strafeRadius = 12f;         // 플레이어 기준 유지 반경
    public float strafeTolerance = 2f;       // 반경 허용 오차(±)
    public float strafeAhead = 4f;           // 원 둘레 방향으로 앞지점 오프셋
    public float strafeSpeedMul = 0.85f;     // 원형 이동 시 속도 배율
    public float strafeSwitchInterval = 2.5f;// 방향 전환 주기
    public float strafeObstacleCheck = 2.0f; // 전방 장애물 감지 거리
    int _strafeDir = 1;
    float _strafeTimer;

    // ---------------- 간격 유지(붙지 않기) ----------------
    [Header("간격 유지(붙지 않기)")]
    public float keepOutDistance = 11.5f;     // 최소 유지 거리( stopDistance보다 약간 큼 )
    public float ringBias = 0.6f;             // 반경으로 밀어낼 때 가중치(0~1)
    public float postJumpKeepOutTime = 1.5f;  // 점프 후 강제 반경 유지 시간
    float _keepOutUntil = 0f;                 // 내부 타이머

    // ---------------- 점프백 거리 자동 조절 ----------------
    [Header("점프백 거리(자동 조절)")]
    public bool autoJumpBack = true;          // true면 상황에 맞춰 필요한 만큼만 뒤로
    public float minJumpBackDistance = 2.5f;  // 최소 물러남
    public float maxJumpBackDistance = 10f;   // 최대 물러남
    public float jumpTargetRange = 12f;       // 점프 후 대략 유지하고 싶은 거리

    // ---------------- 점프/미사일 패턴 ----------------
    [Header("점프 이탈")]
    public float jumpBackDistance = 18f;      // autoJumpBack=false일 때 사용
    public float jumpAirTime = 0.9f;
    public float jumpArcHeight = 4f;
    public float afterJumpDelay = 0.25f;

    // BossPatternDirector.cs 상단 필드 근처에 추가
    [Header("충돌 이동 (Agent OFF일 때)")]
    public bool useCollisionMoveWhenNoAgent = true;
    public CapsuleCollider bodyCapsule;           // 보스 몸통 캡슐(없으면 기본 치수 사용)
    public LayerMask collisionMask = ~0;          // 충돌 고려 레이어
    public float skinWidth = 0.03f;               // 살짝 여유




    [Header("점프 이탈 - 미세 조절")]
    public bool limitJumpBackDistance = true;               // ★ 최대 이동거리 캡(아주 살짝만 뒤로)
    [Range(0f, 2f)] public float tinyJumpBackDistance = 0.7f; // ★ 권장 0.5~0.8m
    public bool jumpInPlace = false;                        // 완전 제자리 점프 원할 때

    [Header("미사일 중 위치 제어")]
    public bool missileLockPosition = true;       // ★ 미사일 쏘는 동안 자리 고정
    public bool disableRootMotionDuringMissile = true; // ★ 루트모션이 뒤로 미는 애니면 끔
    [Range(0f, 2f)] public float missileBackstepMax = 0.6f; // ★ 잠깐 뒤로 가더라도 최대치
    public float missileBackstepSpeed = 0.6f;     // 뒤로 미는 속도(초당 m). 0이면 사실상 이동 없음.

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

    [Header("ULT 슬로우")]
    [Tooltip("보스 전용 로컬 타임스케일 (ULT 중 느려짐)")]
    [Range(0.05f, 1f)] public float _localTimeScale = 1f;

    [Header("디버그")]
    public bool logEnabled = false;

    enum State { ChaseAndSpray, JumpBack, Missile, Cooldown, Stagger }
    State _state = State.ChaseAndSpray;

    float _lastPlan;
    float _snapEps = 1f;

    // HP/누적데미지
    int _prevHp;
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

        // HP 이벤트 구독 + 초기값 동기화
        if (boss)
        {
            boss.onHpChanged.AddListener(OnBossHpChanged); // (current,max)
            int cur = Mathf.RoundToInt(boss.HpRatio * boss.maxHP);
            _prevHp = cur; // 시작 기준
        }
    }

    void OnDestroy()
    {
        if (boss) boss.onHpChanged.RemoveListener(OnBossHpChanged);
    }

    void Update()
    {
        if (!player) return;

        // ★ ULT 슬로우가 SetLocalTimeScale(...)로 들어오지 않는 환경에서도 대응하고 싶다면,
        //    UltimateSkill의 정적 상태를 폴링해 배수 적용하는 코드를 추가해도 됩니다.
        //    (프로젝트마다 다르므로 기본 제공 X)

        if (_state == State.ChaseAndSpray && !_isSequenceRunning && !_isStagger)
            TickChase();
    }

    // --------- 외부에서 호출해 슬로우 적용(ULT 연동용) ---------
    public void SetLocalTimeScale(float scale)
    {
        _localTimeScale = Mathf.Clamp(scale, 0.05f, 1f);
        if (animator) animator.speed = _localTimeScale;
    }
    public void ResetLocalTimeScale() => SetLocalTimeScale(1f);

    // ---------------- 메인 로직 ----------------
    void TickChase()
    {
        FacePlayerHard();

        float dist = Vector3.Distance(transform.position, player.position);
        bool forceKeepOut = Time.time < _keepOutUntil; // 점프 직후 강제 반경 유지
        bool inStrafeBand = enableStrafe &&
                            dist > Mathf.Max(stopDistance, strafeRadius - strafeTolerance) &&
                            dist < (strafeRadius + strafeTolerance);

        if (Time.time - _lastPlan < 0.05f) return; // 너무 자주 계획 변경 방지
        _lastPlan = Time.time;

        if (forceKeepOut)
        {
            Vector3 ring = GetRingPos(Mathf.Max(keepOutDistance, strafeRadius));
            Vector3 toPlayer = (player.position - transform.position); toPlayer.y = 0f;
            Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer.normalized) * _strafeDir;
            Vector3 dest = Vector3.Lerp(transform.position, ring, 0.8f) + tangent * strafeAhead;

            if (agent && agent.enabled)
            {
                if (NavMesh.SamplePosition(dest, out var hit, 2f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
                else agent.SetDestination(dest);
                agent.isStopped = false;
            }
            else
            {
                transform.position += (dest - transform.position).normalized * Mathf.Max(0.1f, manualMoveSpeed) * 0.1f;
            }
            return;
        }

        if (inStrafeBand) DoStrafe(dist);
        else DoApproach(dist);
    }

    void DoStrafe(float dist)
    {
        _strafeTimer -= Time.deltaTime;
        Vector3 toPlayer = (player.position - transform.position); toPlayer.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer.normalized) * _strafeDir;

        bool blocked = Physics.Raycast(transform.position + Vector3.up * 1f, tangent, strafeObstacleCheck, ~0, QueryTriggerInteraction.Ignore);
        if (_strafeTimer <= 0f || blocked)
        {
            _strafeDir *= -1;
            _strafeTimer = Random.Range(strafeSwitchInterval * 0.7f, strafeSwitchInterval * 1.3f);
        }

        Vector3 ringPos = player.position + toPlayer.normalized * Mathf.Clamp(strafeRadius, stopDistance + 0.5f, 999f);
        Vector3 dest = ringPos + tangent.normalized * strafeAhead;

        float radialErr = dist - strafeRadius;
        dest += (-toPlayer.normalized) * Mathf.Clamp(radialErr, -1.5f, 1.5f);

        float targetSpeed = Mathf.Max(0.5f, farSpeed * strafeSpeedMul);
        targetSpeed *= _localTimeScale; // ★ ULT 슬로우 반영

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
            float accel = navAcceleration;
            _curManualSpeed = Mathf.MoveTowards(_curManualSpeed, targetSpeed, accel * Time.deltaTime);
            if (useCollisionMoveWhenNoAgent)
                CollisionMove((dest - transform.position).normalized * _curManualSpeed * 0.1f);
            else
                transform.position += (dest - transform.position).normalized * _curManualSpeed * 0.1f;
        }
    }

    void DoApproach(float dist)
    {
        float targetSpeed = (dist > nearDistance) ? farSpeed : nearSpeed;
        targetSpeed *= _localTimeScale; // ★ ULT 슬로우 반영

        if (dist <= keepOutDistance)
        {
            Vector3 ring = GetRingPos(Mathf.Max(keepOutDistance, strafeRadius));
            Vector3 toPl = (player.position - transform.position); toPl.y = 0f;
            Vector3 tangent = Vector3.Cross(Vector3.up, toPl.normalized) * _strafeDir;
            Vector3 dest = Vector3.Lerp(transform.position, ring, ringBias) + tangent * (strafeAhead * 0.5f);

            if (agent && agent.enabled)
            {
                agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, navAcceleration * Time.deltaTime);
                agent.acceleration = navAcceleration;
                agent.angularSpeed = navAngularSpeed;
                agent.isStopped = false;

                if (NavMesh.SamplePosition(dest, out var hit, 2f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
                else agent.SetDestination(dest);
            }
            else
            {
                _curManualSpeed = Mathf.MoveTowards(_curManualSpeed, targetSpeed, navAcceleration * Time.deltaTime);
                transform.position += (dest - transform.position).normalized * _curManualSpeed * 0.1f;
            }
            return;
        }

        if (agent && agent.enabled)
        {
            Vector3 dest = player.position;
            if (NavMesh.SamplePosition(dest, out var hit, 2f, NavMesh.AllAreas))
                dest = hit.position;

            agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, navAcceleration * Time.deltaTime);
            agent.acceleration = navAcceleration;
            agent.angularSpeed = navAngularSpeed;

            agent.isStopped = false;
            agent.SetDestination(dest);
        }
        else
        {
            Vector3 to = player.position - transform.position; to.y = 0f;
            float tgt = targetSpeed;
            _curManualSpeed = Mathf.MoveTowards(_curManualSpeed, tgt, navAcceleration * Time.deltaTime);
            if (_curManualSpeed > 0.001f)
                if (useCollisionMoveWhenNoAgent)
                    CollisionMove(to.normalized * _curManualSpeed * 0.1f);
                else
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
        float step = navAngularSpeed * Time.deltaTime;

        if (Mathf.Abs(delta) <= Mathf.Max(step, _snapEps))
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, curYaw + Mathf.Sign(delta) * step, 0f);
    }

    Vector3 GetRingPos(float radius)
    {
        Vector3 away = transform.position - player.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
        return player.position + away.normalized * Mathf.Max(radius, stopDistance + 0.5f);
    }

    float GetAutoJumpBackDistance()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float want = Mathf.Max(jumpTargetRange, keepOutDistance);
        float need = Mathf.Max(0f, want - dist);
        return Mathf.Clamp(need, minJumpBackDistance, maxJumpBackDistance);
    }

    void OnBossHpChanged(int current, int max)
    {
        float ratio = (max > 0) ? (float)current / max : 0f;
        TryEnterPhaseByRatio(ratio);

        int taken = Mathf.Max(0, _prevHp - current);
        _prevHp = current;
        if (taken <= 0) return;

        _accDamage += taken;
        float need = usePercent ? (max * GetActiveChunkPercent()) : damageChunkFlat;

        if (!_isSequenceRunning && !_isStagger && _state == State.ChaseAndSpray && _accDamage >= need)
        {
            _accDamage = 0f;
            if (logEnabled) Debug.Log("[Boss] Damage chunk reached → Jump+Missile");
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
        farSpeed = ph.agentSpeed;
        if (agent) agent.speed = farSpeed;

        if (minigun) minigun.SetEnraged(ph.enragedWeapons);
        if (missile)
        {
            missile.currentPattern = ph.missilePattern;
            missile.enraged = ph.enragedWeapons;
        }

        if (logEnabled) Debug.Log($"[Boss] Enter Phase {idx} ({ph.enterAtHpRatio:P0})");
    }

    // --------- 시퀀스: 점프 → 미사일 → 복귀 ---------
    IEnumerator Co_JumpAndMissile()
    {
        _isSequenceRunning = true;

        // 1) Chase 종료: 미니건 잠시 OFF
        onMinigunStop?.Invoke();

        // 2) 점프 이탈
        _state = State.JumpBack;

        // 목적 위치(플레이어 반대 방향)
        Vector3 from = transform.position;
        Vector3 awayDir = (transform.position - player.position);
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.001f) awayDir = -transform.forward;
        awayDir.Normalize();

        // ★ 이동 거리 산출 (auto / 수동 / 제자리) + '아주 살짝' 캡
        float backDist;
        if (jumpInPlace)
            backDist = 0f;
        else if (autoJumpBack)
            backDist = GetAutoJumpBackDistance();
        else
            backDist = jumpBackDistance;

        if (limitJumpBackDistance)
            backDist = Mathf.Min(backDist, tinyJumpBackDistance);

        Vector3 to = from + awayDir * backDist;

        if (agent) agent.enabled = false; // 에이전트 임시 비활성 (충돌 방지)

        // 애니메이션 트리거(있을 때만)
        if (animator && !string.IsNullOrEmpty(jumpTriggerName))
            animator.SetTrigger(jumpTriggerName);

        // 포물선 이동
        float t = 0f;
        while (t < jumpAirTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / jumpAirTime);

            Vector3 pos;
            if (jumpInPlace || backDist <= 0.001f)
            {
                // 제자리 점프(또는 이동 거리 거의 0): XZ 고정, 높이만
                pos = new Vector3(from.x, from.y + Mathf.Sin(u * Mathf.PI) * jumpArcHeight, from.z);
            }
            else
            {
                // 뒤로 점프(아주 짧은 거리 포함)
                pos = Vector3.Lerp(from, to, u);
                pos.y += Mathf.Sin(u * Mathf.PI) * jumpArcHeight;
            }

            transform.position = pos;

            // 공중에서도 플레이어 바라보기
            FacePlayerHard();
            yield return null;
        }

        // 착지 보정(네브메시에 맞춤)
        if (NavMesh.SamplePosition(to, out var hitTo, 2f, NavMesh.AllAreas))
            transform.position = hitTo.position;
        else
            transform.position = to;

        // 점프 직후 일정 시간 반경 유지 강제
        _keepOutUntil = Time.time + postJumpKeepOutTime;

        yield return new WaitForSeconds(afterJumpDelay);

        // 3) 미사일 연사 (여기서 '뒤로 밀림' 제어)
        _state = State.Missile;

        // 미사일 동안 제자리 고정/미세 후퇴 제어 준비
        Vector3 missileStartPos = transform.position;
        float backed = 0f;
        bool prevRootMotion = (animator ? animator.applyRootMotion : false);
        if (animator && disableRootMotionDuringMissile) animator.applyRootMotion = false;

        onMissileBarrage?.Invoke();  // 인스펙터에서 연사 함수 연결

        float missileT = 0f;
        while (missileT < missileDuration)
        {
            missileT += Time.deltaTime;
            FacePlayerHard();

            if (missileLockPosition)
            {
                // ★ 자리 고정: XZ를 착지 지점으로 고정
                Vector3 p = transform.position;
                p.x = missileStartPos.x;
                p.z = missileStartPos.z;
                transform.position = p;
            }
            else
            {
                // ★ 아주 조금만 뒤로: 최대 거리/속도 제한
                float step = missileBackstepSpeed * Time.deltaTime;
                float remain = Mathf.Max(0f, missileBackstepMax - backed);
                float move = Mathf.Min(step, remain);
                if (move > 1e-4f)
                {
                    Vector3 delta = awayDir * move; // 플레이어 반대 방향
                    transform.position += delta;
                    backed += move;
                }
            }

            yield return null;
        }

        // 루트모션 복귀
        if (animator && disableRootMotionDuringMissile) animator.applyRootMotion = prevRootMotion;

        yield return new WaitForSeconds(afterMissileCooldown);

        // 4) Chase 복귀 + 미니건 ON
        if (agent) agent.enabled = true;
        _state = State.ChaseAndSpray;
        onMinigunStart?.Invoke();

        _isSequenceRunning = false;
    }

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

    // BossPatternDirector.cs 맨 아래 쪽에 유틸리티로 추가
    bool CollisionMove(Vector3 delta)
    {
        if (delta.sqrMagnitude < 1e-8f) return false;

        // 캡슐 치수(없으면 기본값)
        float radius = 0.6f;
        float height = 2.0f;
        Vector3 centerLS = Vector3.zero;
        if (bodyCapsule)
        {
            radius = bodyCapsule.radius;
            height = bodyCapsule.height;
            centerLS = bodyCapsule.center;
        }

        Vector3 centerWS = transform.TransformPoint(centerLS);
        Vector3 bottom = centerWS + Vector3.up * (-height * 0.5f + radius);
        Vector3 top = centerWS + Vector3.up * (height * 0.5f - radius);

        float dist = delta.magnitude;
        Vector3 dir = delta / dist;

        if (Physics.CapsuleCast(bottom, top, Mathf.Max(0.01f, radius - skinWidth),
                                 dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            // 1) 막힌 지점까지 전진
            float move = Mathf.Max(0f, hit.distance - 0.001f);
            if (move > 1e-4f) transform.position += dir * move;

            // 2) 슬라이드: 충돌면 법선 방향 성분 제거
            Vector3 remain = delta - dir * move;
            Vector3 slide = Vector3.ProjectOnPlane(remain, hit.normal);
            if (slide.sqrMagnitude > 1e-6f)
                CollisionMove(slide); // 한번 더 시도(짧은 재귀)
            return true;
        }
        else
        {
            transform.position += delta;
            return false;
        }
    }

}

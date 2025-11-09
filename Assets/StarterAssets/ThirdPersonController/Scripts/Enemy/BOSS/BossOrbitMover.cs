using UnityEngine;

public class BossOrbitMover : MonoBehaviour
{
    [Header("타깃")]
    public Transform player;                       // 비워두면 Tag=Player 자동 탐색
    public string playerTag = "Player";

    [Header("궤도(선회)")]
    [Tooltip("플레이어를 기준으로 유지할 반경(거리)")]
    public float orbitRadius = 11f;
    [Tooltip("원형 선회 각속도(도/초). 양수=시계, 음수=반시계")]
    public float orbitAngularSpeed = 60f;
    [Tooltip("반경이 틀어졌을 때 되돌리는 힘(스프링)")]
    public float radiusSpring = 4f;       // 0~8 추천

    [Header("이동 한계")]
    [Tooltip("최대 선형 이동 속도( m/s )")]
    public float maxMoveSpeed = 7f;
    [Tooltip("가속/감속(속도 보간) 세기")]
    public float accelLerp = 12f;         // 값이 클수록 반응성↑

    [Header("지면 스냅(네브메쉬 없이 바닥 붙이기)")]
    public bool snapToGround = true;
    public LayerMask groundMask = ~0;
    [Tooltip("보스를 약간 띄우는 오프셋")]
    public float groundOffsetY = 0.3f;
    [Tooltip("아래로 쏘는 레이 높이/거리")]
    public float groundRayStartHeight = 2.0f;
    public float groundRayDistance = 6.0f;

    [Header("장애물 회피(간단 스티어링)")]
    public bool avoidObstacles = true;
    public LayerMask obstacleMask = ~0;
    public float avoidProbeRadius = 0.8f;
    public float avoidProbeDistance = 2.2f;
    [Range(0f, 1f)] public float avoidSteerWeight = 0.65f;

    [Header("회전(머리 땅 박힘 방지)")]
    public bool facePlayerYawOnly = true;        // 수평만 회전
    public float faceLerpSpeed = 8f;             // 회전 보간 속도
    [Tooltip("메쉬가 X축이 틀어진 경우, 시각(자식)만 X축 보정")]
    public Transform visual;                     // 보스 메쉬 자식(선택)
    public float visualAxisCorrectionX = 0f;     // 필요 시 -90 또는 +90
    public float visualYOffset = 0f;             // 시각만 살짝 띄우기(옵션)

    // 내부 상태
    Vector3 _vel;                                // 현재 속도(단순 물리 느낌)
    Vector3 _lastValidPos;                       // 스냅 실패 시 되돌릴 위치

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }
        _lastValidPos = transform.position;
    }

    void Update()
    {
        if (!player) { TryFindPlayer(); return; }

        // 1) 목표 선회 속도 계산 --------------------------------------------
        // 반지름 방향
        Vector3 toBoss = (transform.position - player.position);
        Vector3 radialDir = toBoss.sqrMagnitude > 0.0001f ? toBoss.normalized : GetFallbackRadial();

        // 원주(접선) 방향: Up과 라디얼의 외적(수평 플레인 기준)
        Vector3 tangent = Vector3.Cross(Vector3.up, radialDir).normalized;
        if (orbitAngularSpeed < 0f) tangent = -tangent;  // 반시계

        // 각속도(도/초) → 선형속도( m/s ) = r * ω(rad/s)
        float linearSpeedFromAngular = orbitRadius * Mathf.Abs(orbitAngularSpeed) * Mathf.Deg2Rad;

        Vector3 desiredVel = tangent * linearSpeedFromAngular;

        // 2) 반경 보정(스프링) ----------------------------------------------
        float currentRadius = toBoss.magnitude;
        float radiusError = currentRadius - orbitRadius;     // +면 바깥으로 멀리 있음
        // 반경 에러를 안쪽(-radial)으로 끌어오는 힘
        Vector3 spring = -radialDir * (radiusError * radiusSpring);
        desiredVel += spring;

        // 3) 장애물 회피(간단) ----------------------------------------------
        if (avoidObstacles)
        {
            Vector3 steer = ComputeAvoidance(desiredVel);
            desiredVel = Vector3.Lerp(desiredVel, steer, avoidSteerWeight);
        }

        // 4) 속도 제한 + 부드러운 가속 --------------------------------------
        desiredVel = Vector3.ClampMagnitude(desiredVel, maxMoveSpeed);
        _vel = Vector3.Lerp(_vel, desiredVel, 1f - Mathf.Exp(-accelLerp * Time.deltaTime));

        // 5) 실제 이동 ------------------------------------------------------
        Vector3 nextPos = transform.position + _vel * Time.deltaTime;

        // (선택) 지면 스냅
        if (snapToGround)
        {
            Vector3 snapped = SnapToGround(nextPos);
            if (snapped.y > -9999f) { nextPos = snapped; _lastValidPos = snapped; }
            else nextPos = _lastValidPos; // 바닥 못 찾으면 이전 안전 위치
        }

        transform.position = nextPos;

        // 6) 수평 회전(머리 땅 박힘 방지) -----------------------------------
        if (facePlayerYawOnly)
        {
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion want = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, faceLerpSpeed * Time.deltaTime);
                // 혹시 기울면 수직 정리
                var e = transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, e.y, 0f);
            }
        }

        // 7) 시각(메쉬) 축/높이 보정(옵션) -----------------------------------
        if (visual)
        {
            var lp = visual.localPosition; lp.y = visualYOffset; visual.localPosition = lp;
            var lr = visual.localEulerAngles; lr.x = visualAxisCorrectionX; lr.z = 0f; visual.localEulerAngles = lr;
        }
    }

    // --- 유틸들 -------------------------------------------------------------

    Vector3 GetFallbackRadial()
    {
        // 플레이어와 겹쳤을 때 임의의 라디얼(오른쪽) 선택
        return Vector3.right;
    }

    Vector3 ComputeAvoidance(Vector3 desiredVel)
    {
        Vector3 dir = desiredVel.sqrMagnitude < 0.0001f ? transform.forward : desiredVel.normalized;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.SphereCast(origin, avoidProbeRadius, dir, out RaycastHit hit, avoidProbeDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // 법선 기반으로 부드럽게 튕겨내기(수평면으로 투영)
            Vector3 away = Vector3.ProjectOnPlane(Vector3.Reflect(dir, hit.normal), Vector3.up).normalized;
            // 전방 성분 유지 + 옆으로 비켜가기
            Vector3 steer = (dir + away).normalized * desiredVel.magnitude;
            return steer;
        }
        return desiredVel;
    }

    Vector3 SnapToGround(Vector3 pos)
    {
        Vector3 rayStart = pos + Vector3.up * groundRayStartHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayDistance + groundRayStartHeight, groundMask, QueryTriggerInteraction.Ignore))
        {
            pos.y = hit.point.y + groundOffsetY;
            return pos;
        }
        // 실패 시 매우 작은 값으로 표시해서 호출부에서 lastValid로 되돌림
        return new Vector3(pos.x, -99999f, pos.z);
    }

    void TryFindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
    }

    void OnDrawGizmosSelected()
    {
        if (player)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, orbitRadius);
        }

        // 전방 회피 프로브 시각화
        Gizmos.color = Color.yellow;
        Vector3 dir = (_vel.sqrMagnitude > 0.01f ? _vel.normalized : transform.forward);
        Vector3 o = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawWireSphere(o + dir * avoidProbeDistance, avoidProbeRadius);
    }
}

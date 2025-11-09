using UnityEngine;

// 애니메이션보다 나중에 실행되어 마지막에 자세를 "고정"합니다.
[DefaultExecutionOrder(1000)]
public class BossUprightLock : MonoBehaviour
{
    [Header("시각(메쉬) 자식")]
    public Transform visual;                 // 보스 모델이 들어있는 자식(없으면 설정)

    [Header("루트 회전 고정")]
    public bool forceRootYawOnly = true;     // 루트는 수평(Yaw)만 허용
    public float rootYawLerp = 12f;          // 루트 회전 보간 속도(부드럽게)

    [Header("시각(메쉬) 축 보정")]
    public bool autoFixAxis = true;          // 메쉬 Forward가 위/아래로 향하면 자동 보정
    public float visualAxisCorrectionX = 0f; // 수동 보정 시 ±90 사용(예: -90 또는 +90)
    public float visualYOffset = 0f;         // 메쉬만 살짝 위로(관통 방지)

    [Header("타깃(수평만 바라봄)")]
    public Transform player;                 // 비워 두면 Player 태그 자동 탐색
    public string playerTag = "Player";

    Quaternion _lockedVisualLocalRot;        // 고정할 로컬 회전
    bool _visualLocked;

    void Awake()
    {
        if (!visual && transform.childCount > 0)
            visual = transform.GetChild(0);  // 가장 흔한 구조: 루트 밑 첫 자식이 메쉬

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }

        // 1) 시각(메쉬) 로컬 회전 기준값 저장
        if (visual)
        {
            // 자동 축 보정: 메쉬 Forward가 위/아래로 너무 가까우면 X로 -90° 회전
            if (autoFixAxis)
            {
                float dotUp = Mathf.Abs(Vector3.Dot(visual.forward, Vector3.up));
                if (dotUp > 0.7f) // Forward가 위/아래로 향함
                {
                    visual.localRotation = Quaternion.Euler(-90f, 0f, 0f) * visual.localRotation;
                }
            }

            // 추가 수동 보정(필요시 ±90)
            if (Mathf.Abs(visualAxisCorrectionX) > 0.01f)
            {
                visual.localRotation = Quaternion.Euler(visualAxisCorrectionX, 0f, 0f) * visual.localRotation;
            }

            _lockedVisualLocalRot = visual.localRotation;
            _visualLocked = true;
        }
    }

    void LateUpdate()
    {
        // 2) 루트는 항상 수평만 유지 (Pitch/Roll 강제 0)
        if (forceRootYawOnly)
        {
            float yaw = transform.eulerAngles.y;

            // 플레이어가 있으면 "수평으로만" 그쪽을 보게 부드럽게 회전
            if (player)
            {
                Vector3 dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    float targetYaw = Quaternion.LookRotation(dir, Vector3.up).eulerAngles.y;
                    yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-rootYawLerp * Time.deltaTime));
                }
            }

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // 3) 시각(메쉬)은 높이/축을 "항상" 고정
        if (_visualLocked && visual)
        {
            // 로컬 회전 고정(애니메이터/다른 스크립트가 비틀어도 마지막에 되돌림)
            visual.localRotation = _lockedVisualLocalRot;

            // 로컬 Y 오프셋(선택): 관통/자세 보정
            if (Mathf.Abs(visualYOffset) > 0.0001f)
            {
                var lp = visual.localPosition;
                lp.y = visualYOffset;
                visual.localPosition = lp;
            }
        }
        else if (visual)
        {
            // 잠금이 아직이라면 현재값을 잠금값으로
            _lockedVisualLocalRot = visual.localRotation;
            _visualLocked = true;
        }
    }
}

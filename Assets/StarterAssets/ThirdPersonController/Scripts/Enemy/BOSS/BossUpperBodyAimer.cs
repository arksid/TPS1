using UnityEngine;

public class BossUpperBodyAimer : MonoBehaviour
{
    [Header("필수 참조")]
    public Transform player;                 // 비워두면 Tag=Player 자동 탐색
    public Transform upperYawPivot;          // 상체 좌우 회전 축(UpperYawPivot)
    public Transform upperPitchPivot;        // 상체 상하 회전 축(UpperPitchPivot)

    [Header("회전 제한(도)")]
    [Tooltip("하체 전방 기준 좌/우 허용 각도(±)")]
    public float maxYaw = 70f;               // 예: 좌우 70도
    [Tooltip("상하 허용 각도(최소~최대)")]
    public float minPitch = -20f;            // 아래쪽
    public float maxPitch = 45f;             // 위쪽

    [Header("회전 속도(도/초)")]
    public float yawSpeed = 360f;
    public float pitchSpeed = 240f;

    [Header("하체 재정렬 옵션")]
    [Tooltip("상체 Yaw가 한계에 가까워지면 하체를 돌려서 부담을 덜어줍니다.")]
    public bool recenterBodyWhenExceeded = true;
    [Tooltip("이 값 이상으로 상체 Yaw 오차가 커지면 하체가 서서히 플레이어 쪽으로 회전하도록 요청")]
    public float recenterYawThreshold = 55f;  // 예: 55~60도부터
    public float bodyRecenterYawPerSec = 180f; // 하체가 초당 이 정도로 추가 회전(선택)

    [Header("디버그")]
    public bool drawDebug = false;

    Transform _root; // 보스 루트(하체 방향 기준)
    float _curYaw;   // 상체 현재 Yaw(로컬)
    float _curPitch; // 상체 현재 Pitch(로컬)

    void Awake()
    {
        _root = transform; // 이 스크립트는 보통 BossRoot에 붙입니다.

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (upperYawPivot) _curYaw = NormalizeSigned(upperYawPivot.localEulerAngles.y);
        if (upperPitchPivot) _curPitch = NormalizeSigned(upperPitchPivot.localEulerAngles.x);
    }

    void LateUpdate()
    {
        if (!player || !upperYawPivot || !upperPitchPivot) return;

        // 1) 월드 기준 목표 방향
        Vector3 aimDirWorld = (player.position - upperYawPivot.position);
        if (aimDirWorld.sqrMagnitude < 0.0001f) return;

        // 2) 하체(루트) 기준 로컬 방향으로 변환
        Vector3 aimDirLocal = _root.InverseTransformDirection(aimDirWorld.normalized);

        // 3) 로컬 좌우(Yaw), 상하(Pitch) 각도 계산
        // 로컬 기준으로 yaw = atan2(x,z), pitch = atan2(-y, sqrt(x^2+z^2))
        float targetYaw = Mathf.Atan2(aimDirLocal.x, aimDirLocal.z) * Mathf.Rad2Deg;
        float flatLen = Mathf.Sqrt(aimDirLocal.x * aimDirLocal.x + aimDirLocal.z * aimDirLocal.z);
        float targetPitch = Mathf.Atan2(-aimDirLocal.y, flatLen) * Mathf.Rad2Deg;

        // 4) 각도 제한(클램프)
        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        // 5) 부드럽게 따라가기
        _curYaw = Mathf.MoveTowardsAngle(_curYaw, targetYaw, yawSpeed * Time.deltaTime);
        _curPitch = Mathf.MoveTowardsAngle(_curPitch, targetPitch, pitchSpeed * Time.deltaTime);

        // 6) 로컬 회전 적용
        Vector3 yawEuler = upperYawPivot.localEulerAngles;
        yawEuler.y = _curYaw;
        upperYawPivot.localEulerAngles = yawEuler;

        Vector3 pitchEuler = upperPitchPivot.localEulerAngles;
        pitchEuler.x = _curPitch;
        upperPitchPivot.localEulerAngles = pitchEuler;

        // 7) 상체가 한계 근처면 하체 재정렬 요청(옵션)
        if (recenterBodyWhenExceeded)
        {
            float absYaw = Mathf.Abs(NormalizeSigned(_curYaw));
            if (absYaw >= recenterYawThreshold)
            {
                // 하체를 조금씩 플레이어 쪽으로 돌려서 상체 부담 완화
                Vector3 rootForward = _root.forward;
                Vector3 toPlayer = (player.position - _root.position);
                toPlayer.y = 0f; rootForward.y = 0f;

                if (toPlayer.sqrMagnitude > 0.0001f && rootForward.sqrMagnitude > 0.0001f)
                {
                    float rootYaw = _root.eulerAngles.y;
                    float wantYaw = Quaternion.LookRotation(toPlayer.normalized, Vector3.up).eulerAngles.y;
                    float newYaw = Mathf.LerpAngle(rootYaw, wantYaw, Mathf.Clamp01(bodyRecenterYawPerSec * Time.deltaTime / 360f));
                    _root.rotation = Quaternion.Euler(0f, newYaw, 0f);
                }
            }
        }

        if (drawDebug)
        {
            Debug.DrawLine(upperYawPivot.position, player.position, Color.yellow);
        }
    }

    float NormalizeSigned(float deg)
    {
        deg %= 360f;
        if (deg > 180f) deg -= 360f;
        if (deg < -180f) deg += 360f;
        return deg;
    }
}

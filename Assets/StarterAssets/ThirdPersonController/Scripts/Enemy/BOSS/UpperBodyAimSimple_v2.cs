using UnityEngine;

// 애니메이터/다른 스크립트보다 "늦게" 실행되어 마지막에 에임을 적용합니다.
[DefaultExecutionOrder(1000)]
public class UpperBodyAimSimple_v2 : MonoBehaviour
{
    [Header("필수 참조")]
    public Transform player;             // 비우면 Tag=Player 자동 탐색
    public Transform upperYawPivot;      // 좌우(Yaw) 피벗
    public Transform upperPitchPivot;    // 상하(Pitch) 피벗

    [Header("기준(하체 기준 로컬로 각도 계산)")]
    public Transform bodyRoot;           // 보스 하체 기준(보통 BossRoot). 비우면 this.transform

    [Header("각도 제한(도)")]
    public float maxYaw = 110f;          // 좌/우 허용 각(±). 필요하면 140까지
    public float minPitch = -25f;        // 아래
    public float maxPitch = 55f;         // 위

    [Header("속도(도/초)")]
    public float yawSpeed = 540f;        // 반응이 느리면 올리세요
    public float pitchSpeed = 360f;

  
    public enum Axis { X, Y, Z }
    public Axis yawAxisLocal = Axis.Y;   // 보통 Y
    public Axis pitchAxisLocal = Axis.X; // 보통 X
    public bool invertYaw = false;       // 축 반전이 필요하면 체크
    public bool invertPitch = false;

    [Header("옵션")]
    public bool aimOn = true;            // 필요 시 외부에서 SetAim으로 On/Off
    public bool drawDebug = false;

    float _curYaw, _curPitch;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!bodyRoot) bodyRoot = transform;

        if (upperYawPivot) _curYaw = NormalizeSigned(upperYawPivot.localEulerAngles.y);
        if (upperPitchPivot) _curPitch = NormalizeSigned(upperPitchPivot.localEulerAngles.x);
    }

    void LateUpdate()
    {
        if (!aimOn) return;

        if (!upperYawPivot || !upperPitchPivot)
        {
            // 필요한 참조가 없으면 눈에 띄게 경고
            if (Time.frameCount % 30 == 0)
                Debug.LogWarning("[UpperBodyAimSimple_v2] upperYawPivot/upperPitchPivot 미지정");
            return;
        }
        if (!player) return;

        // 1) 목표 방향 (월드) → 하체(bodyRoot) 로컬
        Vector3 to = (player.position - upperYawPivot.position).normalized;
        Vector3 toLocal = bodyRoot.InverseTransformDirection(to);

        // 2) 선택한 로컬 축으로 각도 분리
        Vector3 axisYawW = LocalAxisToWorld(bodyRoot, yawAxisLocal);
        Vector3 axisPitchW = LocalAxisToWorld(bodyRoot, pitchAxisLocal);

        // 각도 계산: 선택한 축에 수직인 평면에 투영한 뒤 signed angle
        float targetYaw = SignedAngleOnPlane(bodyRoot.forward, bodyRoot.TransformDirection(toLocal), axisYawW);
        float targetPitch = SignedAngleOnPlane(bodyRoot.forward, bodyRoot.TransformDirection(toLocal), axisPitchW);

        if (invertYaw) targetYaw = -targetYaw;
        if (invertPitch) targetPitch = -targetPitch;

        // 3) 제한 + 부드럽게 추적
        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        _curYaw = Mathf.MoveTowardsAngle(_curYaw, targetYaw, yawSpeed * Time.deltaTime);
        _curPitch = Mathf.MoveTowardsAngle(_curPitch, targetPitch, pitchSpeed * Time.deltaTime);

        // 4) 피벗에 적용 (선택한 로컬 축에만 값 반영)
        upperYawPivot.localRotation = ApplyAxisAngle(upperYawPivot.localRotation, yawAxisLocal, _curYaw);
        upperPitchPivot.localRotation = ApplyAxisAngle(upperPitchPivot.localRotation, pitchAxisLocal, _curPitch);

        // 5) 디버그
        if (drawDebug)
        {
            Debug.DrawLine(upperYawPivot.position, player.position, Color.yellow);
        }
    }

    // --- 유틸 ---
    float NormalizeSigned(float deg)
    {
        deg %= 360f; if (deg > 180f) deg -= 360f; if (deg < -180f) deg += 360f; return deg;
    }

    Vector3 LocalAxisToWorld(Transform t, Axis a)
    {
        switch (a)
        {
            case Axis.X: return t.right;
            case Axis.Y: return t.up;
            default: return t.forward;
        }
    }

    float SignedAngleOnPlane(Vector3 fromDirWorld, Vector3 toDirWorld, Vector3 planeNormalWorld)
    {
        // 평면에 투영
        Vector3 fromP = Vector3.ProjectOnPlane(fromDirWorld, planeNormalWorld).normalized;
        Vector3 toP = Vector3.ProjectOnPlane(toDirWorld, planeNormalWorld).normalized;
        if (fromP.sqrMagnitude < 1e-6f || toP.sqrMagnitude < 1e-6f) return 0f;
        float angle = Vector3.SignedAngle(fromP, toP, planeNormalWorld);
        return angle;
    }

    Quaternion ApplyAxisAngle(Quaternion currentLocalRot, Axis axis, float angleDeg)
    {
        Vector3 e = currentLocalRot.eulerAngles;
        switch (axis)
        {
            case Axis.X: e.x = angleDeg; break;
            case Axis.Y: e.y = angleDeg; break;
            case Axis.Z: e.z = angleDeg; break;
        }
        return Quaternion.Euler(e);
    }

    // 외부 제어
    public void SetAim(bool on) => aimOn = on;
}

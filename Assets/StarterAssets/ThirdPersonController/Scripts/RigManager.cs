// RigManager.cs
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigManager : MonoBehaviour
{
    [Header("Aim / Hand Weights (driven by controller)")]
    public Transform aimTarget;
    public float aimWeight;          // 에임 리그 가중치 (리그 그래프가 이 값을 사용)
    public float leftHandWeight;     // 왼손(그립) 리그 가중치

    [Header("Left Arm IK (TwoBone)")]
    public TwoBoneIKConstraint leftArmIK; // CombatRig/LeftArmIK
    public float leftArmWeight = 0f;      // 왼팔 전체 고정 가중치

    [Header("Auto Elbow Hint (when Weapon has no hint)")]
    public bool enableAutoElbowHint = true;
    [Tooltip("어깨→손 타깃 방향 전진 배수(팔 길이 기준)")]
    public float autoHintForward = 0.75f;
    [Tooltip("옆 방향 오프셋 배수(팔 길이 기준)")]
    public float autoHintSide = 0.5f;
    [Tooltip("자동 힌트의 기준 위벡터")]
    public Vector3 autoHintUp = Vector3.up;

    // 기존 프로젝트 호환용(왼손 리그 데이터)
    private Transform _leftHandTarget;
    private Transform _leftHandRotationRef;

    // 자동 힌트 캐시
    private Transform _autoElbowHint;

    /// <summary>기존 API 호환: 왼손 그립(Transform 2개)</summary>
    public void SetLeftHandGrioData(Transform leftHandTarget, Transform leftHandRotation)
    {
        _leftHandTarget = leftHandTarget;
        _leftHandRotationRef = leftHandRotation;
        // 실제 왼손 리그(멀티 레퍼런스/멀티 에임 등)는 에디터에서 leftHandWeight를 참조하도록 구성
    }

    /// <summary>무기 장착 시 왼팔 IK 타깃/힌트 갱신(힌트 null 허용)</summary>
    public void SetLeftArmTargets(Transform handTarget, Transform elbowHint)
    {
        if (leftArmIK == null) return;

        var data = leftArmIK.data;
        if (handTarget != null) data.target = handTarget;

        if (elbowHint != null)
        {
            data.hint = elbowHint;
        }
        else
        {
            if (enableAutoElbowHint)
            {
                EnsureAutoElbowHintExists();
                UpdateAutoElbowHintPosition(handTarget);
                data.hint = _autoElbowHint;
            }
            else
            {
                data.hint = null;
            }
        }

        leftArmIK.data = data;
    }

    private void LateUpdate()
    {
        if (leftArmIK != null)
            leftArmIK.weight = leftArmWeight;

        // 자동 힌트 활성 시, 손 타깃 이동에 맞춰 힌트 동기화
        if (enableAutoElbowHint && _autoElbowHint != null && leftArmIK != null)
        {
            UpdateAutoElbowHintPosition(leftArmIK.data.target);
        }
    }

    private void EnsureAutoElbowHintExists()
    {
        if (_autoElbowHint != null) return;

        var go = new GameObject("AutoElbowHint_L");
        go.transform.SetParent(leftArmIK != null ? leftArmIK.transform : transform, worldPositionStays: false);
        _autoElbowHint = go.transform;
    }

    private void UpdateAutoElbowHintPosition(Transform handTarget)
    {
        if (leftArmIK == null || handTarget == null) return;

        var data = leftArmIK.data;
        var root = data.root; // UpperArm(L)
        var mid = data.mid;  // LowerArm(L)
        var tip = data.tip;  // Hand(L)
        if (root == null || mid == null || tip == null) return;

        Vector3 rootPos = root.position;
        Vector3 midPos = mid.position;
        Vector3 tipTargetPos = handTarget.position;

        float upperLen = Vector3.Distance(rootPos, midPos);
        float fwdDist = Mathf.Max(0.01f, upperLen * Mathf.Abs(autoHintForward));
        float sideDist = Mathf.Max(0.00f, upperLen * Mathf.Abs(autoHintSide));

        Vector3 dir = (tipTargetPos - rootPos).normalized;
        Vector3 up = autoHintUp.sqrMagnitude > 0.001f ? autoHintUp.normalized : Vector3.up;
        Vector3 side = Vector3.Cross(up, dir).normalized;

        Vector3 hintPos = rootPos + dir * fwdDist + side * sideDist;

        _autoElbowHint.position = hintPos;
        _autoElbowHint.rotation = Quaternion.LookRotation(dir, up);
    }

    // 반동 훅(프로젝트 맞게 구현)
    public void ApplyWeaponKick(float handKick, float bodyKick) { /* Camera recoil etc. */ }
}

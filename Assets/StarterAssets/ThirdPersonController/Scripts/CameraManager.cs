using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    [SerializeField][Range(-5, 5)] private float _deflaultSensitivity = 1.5f; public static float deflaultSensitivity { get { return singleton._deflaultSensitivity; } }
    [SerializeField][Range(-5, 5)] private float _aimingSensitivity = 0.5f; public static float aimingSensitivity { get { return singleton._aimingSensitivity; } }
    [SerializeField] private Camera _camera = null; public static Camera maincamera { get { return singleton._camera; } }
    [SerializeField] private CinemachineVirtualCamera _playerCamera = null; public static CinemachineVirtualCamera playerCamera { get { return singleton._playerCamera; } }
    [SerializeField] private CinemachineVirtualCamera _aimingCamera = null; public static CinemachineVirtualCamera aimingCamera { get { return singleton._aimingCamera; } }
    [SerializeField] private CinemachineBrain _cameraBrain = null;
    [SerializeField] private LayerMask _aimLayer;

    // 🔧 추가: 히트가 없을 때 사용할 수렴거리 / 총구 앞 장애물 보정용 마스크
    [Header("Aim Fix")]
    [SerializeField] private float _convergenceDistance = 50f;
    [SerializeField] private LayerMask _obstructionMask = ~0;

    private static CameraManager _singleton = null;
    public static CameraManager singleton
    {
        get
        {
            if (_singleton == null)
            {
                _singleton = FindObjectOfType<CameraManager>();
            }
            return _singleton;
        }
    }

    private bool _aiming = false; public bool aiming { get { return _aiming; } set { _aiming = value; } }
    private Vector3 _aimTargetPiont = Vector3.zero; public Vector3 aimTargetPiont { get { return _aimTargetPiont; } }

    public float sensitivity { get { return _aiming ? _aimingSensitivity : _deflaultSensitivity; } }

    private void Awake()
    {
        if (_cameraBrain != null)
            _cameraBrain.m_DefaultBlend.m_Time = 0.1f;
    }

    private void Update()
    {
        if (_aimingCamera != null)
            _aimingCamera.gameObject.SetActive(_aiming);
        // ⚠️ 에임 타겟 계산은 LateUpdate에서 수행 (카메라 회전 이후)
    }

    private void LateUpdate()
    {
        SetAimTarget();
    }

    private void SetAimTarget()
    {
        if (_camera == null) return;

        // 카메라 중앙에서 에임 레이어로 레이
        Ray ray = _camera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayer, QueryTriggerInteraction.Ignore))
        {
            _aimTargetPiont = hit.point;
        }
        else
        {
            // 히트가 없으면 일정 수렴거리 지점 사용
            _aimTargetPiont = ray.GetPoint(Mathf.Max(1f, _convergenceDistance));
        }
    }

    /// <summary>
    /// 카메라 기준 조준점에서, 총구-타겟 직선에 장애물이 있으면 그 지점으로 교체해 최종 타겟을 반환.
    /// </summary>
    public Vector3 GetFinalAimPoint(Transform muzzle)
    {
        Vector3 target = _aimTargetPiont;
        if (muzzle == null) return target;

        Vector3 dirFromMuzzle = (target - muzzle.position).normalized;
        float dist = Vector3.Distance(muzzle.position, target);

        if (Physics.Raycast(muzzle.position, dirFromMuzzle, out RaycastHit hit, dist, _obstructionMask, QueryTriggerInteraction.Ignore))
        {
            target = hit.point;
        }
        return target;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_aimTargetPiont, 0.1f);
    }
#endif
}

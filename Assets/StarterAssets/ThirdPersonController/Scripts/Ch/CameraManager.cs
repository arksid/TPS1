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

        if (_camera == null)
        {
            int bulletLayer = LayerMask.NameToLayer("Bullet");
            int shellLayer = LayerMask.NameToLayer("Shell");

            // ❌ Bullet, Shell 레이어를 마스크에서 빼기
            _obstructionMask &= ~(1 << bulletLayer);
            _obstructionMask &= ~(1 << shellLayer);
        }
            _camera = Camera.main;
    }

    private void Update()
    {
        if (_aimingCamera != null)
            _aimingCamera.gameObject.SetActive(_aiming);
    }

    private void LateUpdate()
    {
        // 🛑 증강 UI가 열려있거나 무기가 일시정지 상태라면 조준 갱신 안 함
        if (Weapon.IsPaused ||
            (AugmentUIManager.Instance != null && AugmentUIManager.Instance.augmentPanel.activeSelf))
            return;

        SetAimTarget();
    }

    private void SetAimTarget()
    {
        if (_camera == null || !_camera.isActiveAndEnabled)
        {
            _camera = Camera.main;
            if (_camera == null || !_camera.isActiveAndEnabled)
                return;
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null || !playerObj.activeInHierarchy)
            return;

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = _camera.ScreenPointToRay(center);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayer, QueryTriggerInteraction.Ignore))
        {
            _aimTargetPiont = hit.point;
        }
        else
        {
            _aimTargetPiont = ray.GetPoint(Mathf.Max(1f, _convergenceDistance));
        }
    }

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

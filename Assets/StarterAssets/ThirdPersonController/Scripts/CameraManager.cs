using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{

    [SerializeField][Range(-5,5)]private float _deflaultSensitivity = 1.5f; public static float deflaultSensitivity { get { return singleton._deflaultSensitivity; } }
    [SerializeField][Range(-5, 5)] private float _aimingSensitivity = 0.5f; public static float aimingSensitivity { get { return singleton._aimingSensitivity; } }
    [SerializeField] private Camera _camera = null; public static Camera maincamera { get { return singleton._camera; } }
    [SerializeField] private CinemachineVirtualCamera _playerCamera = null; public static CinemachineVirtualCamera playerCamera { get { return singleton._playerCamera; } }
    [SerializeField] private CinemachineVirtualCamera _aimingCamera = null; public static CinemachineVirtualCamera aimingCamera { get { return singleton._aimingCamera; } }
    [SerializeField] private CinemachineBrain _cameraBrain = null;
    [SerializeField] private LayerMask _aimLayer;

    [SerializeField] private float rotationSpeed = 1.5f;
    [SerializeField] private float recoilRecoverSpeed = 10f;

    private float recoilY = 0f;
    private float _targetPitch = 0f;

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
       _cameraBrain.m_DefaultBlend.m_Time = 0.1f;

    }
    private void Update()
    {
        _aimingCamera.gameObject.SetActive(_aiming);

        float mouseY = Input.GetAxis("Mouse Y");

        if (recoilY > 0)
        {
            mouseY -= recoilY;
            recoilY = Mathf.MoveTowards(recoilY, 0f, recoilRecoverSpeed * Time.deltaTime);
        }

        _targetPitch += mouseY * rotationSpeed;
        _targetPitch = Mathf.Clamp(_targetPitch, -80f, 80f); // 시야 제한

        transform.localEulerAngles = new Vector3(_targetPitch, transform.localEulerAngles.y, 0f);

        SetAimTarget();

    }
    private void SetAimTarget()
    {
        Ray ray = _camera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayer))
        {
            _aimTargetPiont = hit.point;
        }
        else
        {
            _aimTargetPiont = ray.GetPoint(1000);
        }
    }
    public void ApplyRecoil(float amount)
    {
        recoilY += amount;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_aimTargetPiont, 0.1f);
    }
#endif

}


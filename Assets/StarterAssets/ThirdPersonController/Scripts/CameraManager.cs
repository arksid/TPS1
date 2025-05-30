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
    private
}

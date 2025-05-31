using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
public class RigManager : MonoBehaviour
{

    [SerializeField] private MultiAimConstraint _rightHand = null;
    [SerializeField] private TwoBoneIKConstraint _lefthand = null;
    [SerializeField] private MultiAimConstraint _body = null;
    [SerializeField] private Transform _aimTarget = null;
    [SerializeField] private Vector3 _weaponHandKickDirection = new Vector3(0, 0.5f, -1);
    [SerializeField] private Vector3 _weaponBodyKickDirection = new Vector3(0, 0.5f, -1);
    public Vector3 aimTarget { set { _aimTarget.position = value; } }

    public float leftHandWeight { set { _lefthand.weight = value; } }

    public float aimWeight { set { _rightHand.weight = value; _body.weight = value; } }


    private Vector3 _originalRightHandOffsetPosition = Vector3.zero;
    private Vector3 _originalBodyOffsetPosition = Vector3.zero;

    private void Awake()
    {
        _originalRightHandOffsetPosition = _rightHand.data.offset;
        _originalBodyOffsetPosition = _body.data.offset;
    }
    public void SetLeftHandGrioData(Vector3 position, Vector3 rotation)
    {
        if(_lefthand.data.target != null)
        {
            _lefthand.data.target.localPosition = position;
            _lefthand.data.target.localEulerAngles = rotation;
        }
        
    }
    public void ApplyWeaponKick(float hand, float body)
    {
        Transform owner = transform;

        Vector3 localHandKick = owner.TransformDirection(_weaponHandKickDirection * hand);
        Vector3 localBodyKick = owner.TransformDirection(_weaponBodyKickDirection * body);

        _rightHand.data.offset = _originalRightHandOffsetPosition + localHandKick;
        _body.data.offset = _originalBodyOffsetPosition + localBodyKick;
    }
    private void Update()
    {
        if(_rightHand.data.offset != _originalRightHandOffsetPosition)
        {
            _rightHand.data.offset = Vector3.Lerp(_rightHand.data.offset,_originalRightHandOffsetPosition, 10f * Time.deltaTime);
        }
        if (_body.data.offset != _originalBodyOffsetPosition)
        {
            _body.data.offset = Vector3.Lerp(_body.data.offset, _originalBodyOffsetPosition, 10f * Time.deltaTime);
        }
    }
}

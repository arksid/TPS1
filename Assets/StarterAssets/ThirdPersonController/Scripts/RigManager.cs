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

    public Vector3 aimTarget { set { _aimTarget.position = value; } }

    public float leftHandWeight { set { _lefthand.weight = value; } }

    public float aimWeight { set { _rightHand.weight = value; _body.weight = value; } }

    public void SetLeftHandGrioData(Vector3 position, Vector3 rotation)
    {
        _lefthand.data.target.localPosition = position;
        _lefthand.data.target.localEulerAngles = rotation;
    }

}

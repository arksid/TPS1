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

}

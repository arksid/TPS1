using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipStateMachine : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Character controller = animator.GetComponent<Character>();
        if (controller != null)
        {
            controller.EquipFinished();
        }
    }
}

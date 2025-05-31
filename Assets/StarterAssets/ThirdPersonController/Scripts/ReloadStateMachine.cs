using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class ReloadStateMachine : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Character controller = animator.GetComponent<Character>();
        if (controller != null)
        {
            controller.ReloadFinished();
        }
    }
}

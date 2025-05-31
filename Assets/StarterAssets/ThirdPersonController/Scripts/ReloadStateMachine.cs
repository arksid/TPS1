using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class ReloadStateMachine : StateMachineBehaviour
{
     public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ThirdPersonController controller = animator.gameObject.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.ReloadFinished();
        }
    }
}

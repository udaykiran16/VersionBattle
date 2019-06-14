using Invector.vCharacterController.vActions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PUN_LadderAction : vLadderAction
{
    protected override void TriggerEnterLadder()
    {
        if (debugMode) Debug.Log("Enter Ladder");

        OnEnterLadder.Invoke();
        triggerEnterOnce = true;
        isUsingLadder = true;
        tpInput.cc.animator.SetInteger("ActionState", 1);     // set actionState 1 to avoid falling transitions            
        tpInput.enabled = false;                              // disable vThirdPersonInput
        tpInput.cc.enabled = false;                           // disable vThirdPersonController, Animator & Motor 
        tpInput.cc.DisableGravityAndCollision();              // disable gravity & turn collision trigger
        tpInput.cc._rigidbody.velocity = Vector3.zero;
        tpInput.cc.isGrounded = false;
        tpInput.cc.animator.SetBool("IsGrounded", false);
        ladderAction.OnDoAction.Invoke();

        ladderActionTemp = ladderAction;

        if (!string.IsNullOrEmpty(ladderAction.playAnimation))
            tpInput.cc.GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, ladderAction.playAnimation, 0.1f);    // trigger the action animation clip                      
    }
    protected override void ExitLadderInput()
    {
        if (!isUsingLadder) return;
        if (tpInput.cc.baseLayerInfo.IsName("EnterLadderTop") || tpInput.cc.baseLayerInfo.IsName("EnterLadderBottom")) return;

        if (ladderAction == null)
        {
            // exit ladder at any moment by pressing the cancelInput
            if (tpInput.cc.baseLayerInfo.IsName("ClimbLadder") && exitInput.GetButtonDown())
            {
                if (debugMode) Debug.Log("Quick Exit");
                ResetPlayerSettings();
            }
        }
        else
        {
            var animationClip = ladderAction.exitAnimation;
            if (animationClip == "ExitLadderBottom")
            {
                // exit ladder when reach the bottom by pressing the cancelInput or pressing down at
                if (exitInput.GetButtonDown() || (speed <= -0.05f && !triggerExitOnce))
                {
                    if (debugMode) Debug.Log("Exit Bottom");
                    triggerExitOnce = true;
                    tpInput.cc.GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, ladderAction.exitAnimation, 0.1f);     
                }
            }
            else if (animationClip == "ExitLadderTop" && tpInput.cc.baseLayerInfo.IsName("ClimbLadder"))    // exit the ladder from the top
            {
                if ((speed >= 0.05f) && !triggerExitOnce && !tpInput.cc.animator.IsInTransition(0))         // trigger the exit animation by pressing up
                {
                    if (debugMode) Debug.Log("Exit Top");
                    triggerExitOnce = true;
                    tpInput.cc.GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, ladderAction.exitAnimation, 0.1f);
                }
            }
        }
    }
}

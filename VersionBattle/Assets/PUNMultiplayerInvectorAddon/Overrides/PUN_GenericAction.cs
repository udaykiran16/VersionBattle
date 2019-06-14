using Invector.vCharacterController.vActions;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PUN_GenericAction : vGenericAction
{
    protected override void TriggerAnimation()
    {
        if (debugMode) Debug.Log("TriggerAnimation");

        // trigger the animation behaviour & match target
        if (!string.IsNullOrEmpty(triggerAction.playAnimation))
        {
            isPlayingAnimation = true;
            GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, triggerAction.playAnimation, 0.1f); // trigger the action animation clip
        }

        // trigger OnDoAction Event, you can add a delay in the inspector   
        StartCoroutine(triggerAction.OnDoActionDelay(gameObject));

        // bool to limit the autoAction run just once
        if (triggerAction.autoAction || triggerAction.destroyAfter) triggerActionOnce = true;

        // destroy the triggerAction if checked with destroyAfter
        if (triggerAction.destroyAfter)
            StartCoroutine(DestroyDelay(triggerAction));
    }
}

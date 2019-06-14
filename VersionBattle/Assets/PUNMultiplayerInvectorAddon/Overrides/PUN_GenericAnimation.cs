using Invector.vCharacterController.vActions;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PUN_GenericAnimation : vGenericAnimation
{
    public override void PlayAnimation()
    {
        triggerOnce = true;
        OnPlayAnimation.Invoke();
        tpInput.cc.GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, animationClip, 0.1f);
    }
}

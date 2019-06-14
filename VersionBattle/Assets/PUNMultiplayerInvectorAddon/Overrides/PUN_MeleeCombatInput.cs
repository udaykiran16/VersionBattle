using Invector;
using Invector.vCharacterController;
using Invector.vEventSystems;
using Invector.vMelee;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PUN_MeleeCombatInput : vMeleeCombatInput
{
    public override void OnEnableAttack()
    {
        if (GetComponent<PhotonView>().IsMine == false) return;
        base.OnEnableAttack();
    }

    public override void OnDisableAttack()
    {
        if (GetComponent<PhotonView>().IsMine == false) return;
        base.OnDisableAttack();
    }

    public override void ResetAttackTriggers()
    {
        if (GetComponent<PhotonView>().IsMine == false) return;
        base.ResetAttackTriggers();
    }

    public override void BreakAttack(int breakAtkID)
    {
        if (GetComponent<PhotonView>().IsMine == false) return;
        base.BreakAttack(breakAtkID);
    }

    public override void OnRecoil(int recoilID)
    {
        if (GetComponent<PhotonView>().IsMine == false) return;
        base.OnRecoil(recoilID);
    }

    public override void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)
    {
        // character is blocking
        if (cc != null && !damage.ignoreDefense && isBlocking && meleeManager != null && meleeManager.CanBlockAttack(damage.sender.position))
        {
            var damageReduction = meleeManager.GetDefenseRate();
            if (damageReduction > 0)
                damage.ReduceDamage(damageReduction);
            if (attacker != null && meleeManager != null && meleeManager.CanBreakAttack())
                attacker.BreakAttack(meleeManager.GetDefenseRecoilID());
            meleeManager.OnDefense();
            cc.currentStaminaRecoveryDelay = damage.staminaRecoveryDelay;
            cc.currentStamina -= damage.staminaBlockCost;
        }
        if (GetComponent<PhotonView>().IsMine == true)
        {
            // apply damage
            damage.hitReaction = !isBlocking;
            cc.TakeDamage(damage);
        }
        else if (GetComponent<PhotonView>().IsMine == false)
        {
            GetComponent<PhotonView>().RPC("ApplyDamage", RpcTarget.Others, JsonUtility.ToJson(damage));
        }
    }

}

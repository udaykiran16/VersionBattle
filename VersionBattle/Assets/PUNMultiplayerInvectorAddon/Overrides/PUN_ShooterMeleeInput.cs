using UnityEngine;
using Invector.vShooter;                //to access "vShooterMeleeInput"
using Invector;                        //to access "vDamage"
using Invector.vEventSystems;          //to access "vIMeleeFighter"
using Photon.Pun;

public class PUN_ShooterMeleeInput : vShooterMeleeInput
{
    protected override void Start()
    {
        //if (GetComponent<PhotonView>().IsMine == true)
        //{
        //    GameObject.FindObjectOfType<vControlAimCanvas>().GetComponent<vControlAimCanvas>().SetCharacterController(GetComponent<M_ThirdPersonController>());
        //}
        base.Start();
    }

    protected override void UpdateMeleeAnimations() //provided by "pararini" on invector forums, thanks!
    {
        // disable the onlyarms layer and run the melee methods if the character is not using any shooter weapon
        if (!animator) return;

        // update MeleeManager Animator Properties
        if ((shooterManager == null || !CurrentActiveWeapon) && meleeManager)
        {
            base.UpdateMeleeAnimations();
            // set the uppbody id (armsonly layer)
            animator.SetFloat("UpperBody_ID", 0, .2f, Time.deltaTime);
            // turn on the onlyarms layer to aim 
            onlyArmsLayerWeight = Mathf.Lerp(onlyArmsLayerWeight, 0, 6f * Time.deltaTime);
            GetComponent<PhotonView>().RPC("SetLayerWeight", RpcTarget.AllBuffered, onlyArmsLayer, onlyArmsLayerWeight);
            // reset aiming parameter
            animator.SetBool("IsAiming", false);
            isReloading = false;
        }
        // update ShooterManager Animator Properties
        else if (shooterManager && CurrentActiveWeapon)
            UpdateShooterAnimations();
        // reset Animator Properties
        else
        {
            // set the move set id (base layer) 
            animator.SetFloat("MoveSet_ID", 0, .1f, Time.deltaTime);
            // set the uppbody id (armsonly layer)
            animator.SetFloat("UpperBody_ID", 0, .2f, Time.deltaTime);
            // set if the character can aim or not (upperbody layer)
            animator.SetBool("CanAim", false);
            // character is aiming
            animator.SetBool("IsAiming", false);
            // turn on the onlyarms layer to aim 
            onlyArmsLayerWeight = Mathf.Lerp(onlyArmsLayerWeight, 0, 6f * Time.deltaTime);
            GetComponent<PhotonView>().RPC("SetLayerWeight", RpcTarget.AllBuffered, onlyArmsLayer, onlyArmsLayerWeight);
        }
    }
    protected override void UpdateShooterAnimations() //provided by "pararini" on invector forums, thanks!
    {
        if (shooterManager == null) return;

        if ((!isAiming && aimTimming <= 0) && meleeManager)
        {
            // set attack id from the melee weapon (trigger fullbody atk animations)
            animator.SetInteger("AttackID", meleeManager.GetAttackID());
        }
        else
        {
            // set attack id from the shooter weapon (trigger shot layer animations)
            animator.SetFloat("Shot_ID", shooterManager.GetShotID());
        }
        // turn on the onlyarms layer to aim 
        onlyArmsLayerWeight = Mathf.Lerp(onlyArmsLayerWeight, (CurrentActiveWeapon) ? 1f : 0f, 6f * Time.deltaTime);
        GetComponent<PhotonView>().RPC("SetLayerWeight", RpcTarget.AllBuffered, onlyArmsLayer, onlyArmsLayerWeight);

        if (CurrentActiveWeapon != null && !shooterManager.useDefaultMovesetWhenNotAiming || (isAiming || aimTimming > 0))
        {
            // set the move set id (base layer) 
            animator.SetFloat("MoveSet_ID", shooterManager.GetMoveSetID(), .1f, Time.deltaTime);
        }
        else if (shooterManager.useDefaultMovesetWhenNotAiming)
        {
            // set the move set id (base layer) 
            animator.SetFloat("MoveSet_ID", 0, .1f, Time.deltaTime);
        }
        // set the isBlocking false while using shooter weapons
        animator.SetBool("IsBlocking", false);
        // set the uppbody id (armsonly layer)
        animator.SetFloat("UpperBody_ID", shooterManager.GetUpperBodyID(), .2f, Time.deltaTime);
        // set if the character can aim or not (upperbody layer)
        animator.SetBool("CanAim", aimConditions);
        // character is aiming
        animator.SetBool("IsAiming", (isAiming || aimTimming > 0) && !isAttacking);
        // find states with the Reload tag
        isReloading = cc.IsAnimatorTag("IsReloading") || shooterManager.isReloadingWeapon;
        // find states with the IsEquipping tag
        isEquipping = cc.IsAnimatorTag("IsEquipping");
    }
    public override void OnEnableAttack()
    {
        if (transform.root.GetComponent<PhotonView>().IsMine == true)
        {
            base.OnEnableAttack();
        }
    }
    public override void OnDisableAttack()
    {
        if (transform.root.GetComponent<PhotonView>().IsMine == true)
        {
            base.OnDisableAttack();
        }
    }
    public override void ResetAttackTriggers()
    {
        if (GetComponent<PhotonView>().IsMine == false) return;
        GetComponent<PhotonView>().RPC("ResetTrigger", RpcTarget.All, "WeakAttack");
        GetComponent<PhotonView>().RPC("ResetTrigger", RpcTarget.All, "StrongAttack");
    }
    public override void OnRecoil(int recoilID)
    {
        cc.animator.SetInteger("RecoilID", recoilID);
        if (GetComponent<PhotonView>().IsMine == false) return;
        GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "TriggerRecoil");
        GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "ResetState");
        GetComponent<PhotonView>().RPC("ResetTrigger", RpcTarget.All, "WeakAttack");
        GetComponent<PhotonView>().RPC("ResetTrigger", RpcTarget.All, "StrongAttack");
    }
    public override void TriggerWeakAttack()
    {
        cc.animator.SetInteger("AttackID", meleeManager.GetAttackID());
        if (GetComponent<PhotonView>().IsMine == true)
        {
            GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "WeakAttack");
        }
    }
    public override void TriggerStrongAttack()
    {
        cc.animator.SetInteger("AttackID", meleeManager.GetAttackID());
        if (GetComponent<PhotonView>().IsMine == true)
        {
            GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "StrongAttack");
        }
    }
    
    public override void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)
    {
        //character is blocking
        if (!damage.ignoreDefense && isBlocking && meleeManager != null && meleeManager.CanBlockAttack(damage.sender.position))
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
        //apply damage
        damage.hitReaction = !isBlocking;
        if (GetComponent<PhotonView>().IsMine == true)
        {
            cc.TakeDamage(damage);
        }
        else
        {   
            GetComponent<PhotonView>().RPC("ApplyDamage", RpcTarget.Others, JsonUtility.ToJson(damage));
        }
    }

    protected override void UpdateAimHud()
    {
        if (cc == null)
        {
            cc = GetComponent<PUN_ThirdPersonController>();
        }
        if (GetComponent<PhotonView>().IsMine == false) return;
        base.UpdateAimHud();
    }
}

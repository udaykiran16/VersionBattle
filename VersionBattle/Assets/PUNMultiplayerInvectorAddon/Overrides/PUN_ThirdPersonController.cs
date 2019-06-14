using UnityEngine;
using Invector.vCharacterController;
using Invector;
using Photon.Pun;

public class PUN_ThirdPersonController : vThirdPersonController
{
    private float idleCount;

    public override void TakeDamage(vDamage damage)
    {

        if (GetComponent<PhotonView>().IsMine == true)
        {
            base.TakeDamage(damage);
        }
        else
        {
            GetComponent<PhotonView>().RPC("ApplyDamage", RpcTarget.Others, JsonUtility.ToJson(damage));
        }
    }

    protected override void TriggerDamageReaction(vDamage damage)
    {
        if (animator != null && animator.enabled && !damage.activeRagdoll && currentHealth > 0)
        {
            if (damage.sender && hitDirectionHash.isValid) animator.SetInteger(hitDirectionHash, (int)transform.HitAngle(damage.sender.position));
            // trigger hitReaction animation
            if (damage.hitReaction)
            {
                // set the ID of the reaction based on the attack animation state of the attacker - Check the MeleeAttackBehaviour script
                if (reactionIDHash.isValid) animator.SetInteger(reactionIDHash, damage.reaction_id);
                //Debug.Log(triggerReactionHash);
                if (triggerReactionHash.isValid) GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "TriggerReaction");
                //Debug.Log(triggerResetStateHash);
                if (triggerResetStateHash.isValid) GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "ResetState");
            }
            else
            {
                if (recoilIDHash.isValid) animator.SetInteger(recoilIDHash, damage.recoil_id);
                //triggerRecoilHash
                if (triggerRecoilHash.isValid) GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "TriggerRecoil");
                //triggerResetStateHash
                if (triggerResetStateHash.isValid) GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "ResetState");
            }
        }
        if (damage.activeRagdoll)
            onActiveRagdoll.Invoke();
    }

    protected override void TriggerRandomIdle()
    {
        if (input != Vector2.zero || actions) return;

        if (randomIdleTime > 0)
        {
            if (input.sqrMagnitude == 0 && !isCrouching && _capsuleCollider.enabled && isGrounded)
            {
                idleCount += Time.fixedDeltaTime;
                if (idleCount > 6)
                {
                    idleCount = 0;
                    animator.SetInteger("IdleRandom", Random.Range(1, 4));
                    if (GetComponent<PhotonView>().IsMine == true)
                    {
                        GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "IdleRandomTrigger");
                    }
                }
            }
            else
            {
                idleCount = 0;
                animator.SetInteger("IdleRandom", 0);
            }
        }
    }

    public override void Roll()
    {
        bool staminaCondition = currentStamina > rollStamina;
        // can roll even if it's on a quickturn or quickstop animation
        bool actionsRoll = !actions || (actions && (quickStop));
        // general conditions to roll
        bool rollConditions = (input != Vector2.zero || speed > 0.25f) && actionsRoll && isGrounded && staminaCondition && !isJumping;

        if (!rollConditions || isRolling) return;

        if (GetComponent<PhotonView>().IsMine == true)
        {
            GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, "Roll", 0.1f);
        }
        ReduceStamina(rollStamina, false);
        currentStaminaRecoveryDelay = 2f;
    }

    public override void TriggerAnimationState(string animationClip, float transition)
    {
        if (GetComponent<PhotonView>().IsMine == true)
        {
            GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, animationClip, transition);
        }
    }

    public override void Jump(bool consumeStamina = false)
    {
        if (customAction || GroundAngle() > slopeLimit) return;

        bool staminaConditions = currentStamina > jumpStamina;
        bool jumpConditions = !isCrouching && isGrounded && !actions && staminaConditions && !isJumping;
        if (!jumpConditions) return;
        jumpCounter = jumpTimer;
        isJumping = true;
        if (input.sqrMagnitude < 0.1f && GetComponent<PhotonView>().IsMine == true)
        {
            GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, "Jump", 0.1f);
        }
        else if (GetComponent<PhotonView>().IsMine == true)
        {
            GetComponent<PhotonView>().RPC("CrossFadeInFixedTime", RpcTarget.All, "JumpMove", 0.2f);
        }
        if (consumeStamina)
        {
            ReduceStamina(jumpStamina, false);
            currentStaminaRecoveryDelay = 1f;
        }
    }

    public override void MatchTarget(Vector3 matchPosition, Quaternion matchRotation, AvatarTarget target, MatchTargetWeightMask weightMask, float normalisedStartTime, float normalisedEndTime)
    {
        if (animator.isMatchingTarget || animator.IsInTransition(0))
            return;

        float normalizeTime = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);

        if (normalizeTime > normalisedEndTime)
            return;

        if (GetComponent<PhotonView>().IsMine == true)
        {
            animator.MatchTarget(matchPosition, matchRotation, target, weightMask, normalisedStartTime, normalisedEndTime);
            GetComponent<PhotonView>().RPC("AnimMatchTarget", RpcTarget.Others, matchPosition, matchRotation, target, weightMask, normalisedStartTime, normalisedEndTime);
        }
    }
}

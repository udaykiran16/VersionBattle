using UnityEngine;
using Photon.Pun;
using Invector.vCharacterController;            
using Invector.vShooter;
using Invector.vMelee;
using Invector.vItemManager;
using Invector.vCharacterController.vActions;
using Invector;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonRigidbodyView))]
public class PUN_SyncPlayer : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Sync Components
    private Transform local_head, local_neck, local_spine, local_chest = null;
    private Quaternion correctBoneHeadRot, correctBoneNeckRot, correctBoneSpineRot, correctBoneChestRot = Quaternion.identity;
    private Vector3 correctPlayerPos = Vector3.zero;
    private Quaternion correctPlayerRot = Quaternion.identity;
    private Dictionary<string, AnimatorControllerParameterType> animParams = new Dictionary<string, AnimatorControllerParameterType>();
    private int currentBoneRate = 0;

    PhotonView view;
    Animator animator;
    //private float lag = 0.0f;
    #endregion

    #region Modifiables
    [Tooltip("This will sync the bone positions. Makes it so players on the network can see where this player is looking.")]
    [SerializeField] private bool _syncBones = true;
    [Tooltip("How fast to send new bone updates. The lower numbers = faster it will send. (0 = fastest possible). Higher numbers = slower but less network traffic.")]
    [SerializeField] private int _syncBonesRate = 5;
    [Tooltip("How fast to move bones of network player version when it receives an update from the server.")]
    [SerializeField] private float _boneLerpRate = 90.0f;
    [Space(10)]
    [Tooltip("This will sync the bone positions. Makes it so players on the network can see where this player is looking.")]
    [SerializeField] private bool _syncAnimations = true;
    [Tooltip("How fast to move to new position when the networked player receives and update from the server.")]
    [SerializeField] private float _positionLerpRate = 5.0f;
    [Tooltip("How fast to move to new rotation when the networked player receives and update from the server.")]
    [SerializeField] private float _rotationLerpRate = 5.0f;
    [Tooltip("If this is not a locally controller version of this player change the objects tag to be this.")]
    public string noneLocalTag = "Enemy";
    [Tooltip("If this is not a locally controller version of this player change the objects layer to be this. (ONLY SELECT ONE!)")]
    public int _nonAuthoritativeLayer = 9;
    #endregion

    #region Initializations 
    void Start()
    {
        animator = GetComponent<Animator>();
        view = GetComponent<PhotonView>();

        if (GetComponent<PUN_ThirdPersonController>()) GetComponent<PUN_ThirdPersonController>().enabled = true;
        if (GetComponent<vHitDamageParticle>()) GetComponent<vHitDamageParticle>().enabled = true;

        if (view.IsMine == true && PhotonNetwork.IsConnected == true)
        {
            if (GetComponent<PUN_MeleeManager>()) GetComponent<PUN_MeleeManager>().enabled = true;
            if (GetComponent<PUN_MeleeCombatInput>()) GetComponent<PUN_MeleeCombatInput>().enabled = true;
            if (GetComponent<vMeleeManager>()) GetComponent<vMeleeManager>().enabled = true;
            if (GetComponent<PUN_ShooterMeleeInput>()) GetComponent<PUN_ShooterMeleeInput>().enabled = true;
            if (GetComponent<vShooterManager>()) GetComponent<vShooterManager>().enabled = true;
            if (GetComponent<vAmmoManager>()) GetComponent<vAmmoManager>().enabled = true;
            if (GetComponent<vHeadTrack>()) GetComponent<vHeadTrack>().enabled = true;
            if (GetComponent<vItemManager>()) GetComponent<vItemManager>().enabled = true;
            if (GetComponent<vWeaponHolderManager>()) GetComponent<vWeaponHolderManager>().enabled = true;
            if (GetComponent<vGenericAction>()) GetComponent<vGenericAction>().enabled = true;
            if (GetComponent<vLadderAction>()) GetComponent<vLadderAction>().enabled = true;
            //if (GetComponent<vThrowObject>()) GetComponent<vThrowObject>().enabled = true;
            if (GetComponent<vItemManager>()) GetComponent<vItemManager>().enabled = true;
            if (GetComponent<vLockOn>()) GetComponent<vLockOn>().enabled = true;
        }
        else
        {
            if (!string.IsNullOrEmpty(noneLocalTag))
            {
                this.tag = noneLocalTag;
            }
            SetLayer();
            SetTags(animator.GetBoneTransform(HumanBodyBones.Hips).transform);
        }
        if (_syncBones == true)
        {
            SetBones();
        }
        BuildAnimatorParamsDict();
    }
    void SetBones()
    {
        if (local_head == null)
        {
            try
            {
                local_head = animator.GetBoneTransform(HumanBodyBones.Head).transform;
                correctBoneHeadRot = local_head.localRotation;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }
        if (local_neck == null)
        {
            try
            {
                local_neck = animator.GetBoneTransform(HumanBodyBones.Neck).transform;
                correctBoneNeckRot = local_neck.localRotation;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }
        if (local_spine == null)
        {
            try
            {
                local_spine = animator.GetBoneTransform(HumanBodyBones.Spine).transform;
                correctBoneSpineRot = local_spine.localRotation;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }
        if (local_chest == null)
        {
            try
            {
                local_chest = animator.GetBoneTransform(HumanBodyBones.Chest).transform;
                correctBoneChestRot = local_chest.localRotation;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
    void SetLayer()
    {
        gameObject.layer = _nonAuthoritativeLayer;
        animator.GetBoneTransform(HumanBodyBones.Hips).transform.parent.gameObject.layer = _nonAuthoritativeLayer;
    }
    void SetTags(Transform target)
    {
        target.tag = noneLocalTag;
        foreach(Transform child in target)
        {
            if (child.tag == "Untagged" || child.tag == "Player")
            {
                child.tag = noneLocalTag;
            }
            SetTags(child);
        }
    }
    void BuildAnimatorParamsDict()
    {
        if (GetComponent<Animator>())
        {
            foreach (var param in GetComponent<Animator>().parameters)
            {
                if (param.type != AnimatorControllerParameterType.Trigger) //Syncing triggers this way is unreliable, send trigger events via RPC
                {
                    animParams.Add(param.name, param.type);
                }
            }
        }
    }
    #endregion

    #region Server Sync Logic
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) //this function called by Photon View component
    {
        if (stream.IsWriting)   //Authoritative player sending data to server
        {
            //Send Player Position and rotation
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            if (_syncAnimations == true)
            {
                //Send Player Animations
                foreach (var item in animParams)
                {
                    switch (item.Value)
                    {
                        case AnimatorControllerParameterType.Bool:
                            stream.SendNext(animator.GetBool(item.Key));
                            break;
                        case AnimatorControllerParameterType.Float:
                            stream.SendNext(animator.GetFloat(item.Key));
                            break;
                        case AnimatorControllerParameterType.Int:
                            stream.SendNext(animator.GetInteger(item.Key));
                            break;
                    }
                }
            }
        }
        else  //Network player copies receiving data from server
        {
            //Receive Player Position and rotation
            this.correctPlayerPos = (Vector3)stream.ReceiveNext();
            this.correctPlayerRot = (Quaternion)stream.ReceiveNext();

            if (_syncAnimations == true)
            {
                //Receive Player Animations
                foreach (var item in animParams)
                {
                    switch (item.Value)
                    {
                        case AnimatorControllerParameterType.Bool:
                            animator.SetBool(item.Key, (bool)stream.ReceiveNext());
                            break;
                        case AnimatorControllerParameterType.Float:
                            animator.SetFloat(item.Key, (float)stream.ReceiveNext());
                            break;
                        case AnimatorControllerParameterType.Int:
                            animator.SetInteger(item.Key, (int)stream.ReceiveNext());
                            break;
                    }
                }
            }
        }
        //lag = Mathf.Abs((float)(PhotonNetwork.Time - info.timestamp));
    }

    public void SendTrigger(string name)
    {
        GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, name);
    }

    [PunRPC]
    public void AnimMatchTarget(Vector3 matchPosition, Quaternion matchRotation, AvatarTarget target, MatchTargetWeightMask weightMask, float normalisedStartTime, float normalisedEndTime)
    {
        animator.MatchTarget(matchPosition, matchRotation, target, weightMask, normalisedStartTime, normalisedEndTime);
    }
    [PunRPC]
    public void WeaponHolderSetActiveWeapon(bool value, int id)
    {
        foreach (vWeaponHolder holder in GetComponentsInChildren<vWeaponHolder>())
        {
            if (holder.itemID == id)
            {
                holder.SetActiveWeapon(value);
                break;
            }
        }
    }
    [PunRPC]
    public void WeaponHolderSetActiveHolder(bool value, int id)
    {
        foreach(vWeaponHolder holder in GetComponentsInChildren<vWeaponHolder>())
        {
            if (holder.itemID == id)
            {
                holder.SetActiveHolder(value);
                break;
            }
        }
    }
    [PunRPC]
    public void OnDestroyWeapon(string weaponName, PUN_ItemManager.EquipSide side)
    {
        FindObjectOfType<PUN_ItemManager>().DestroyWeapon(gameObject, weaponName, side);
    }
    [PunRPC]
    public void SetRightWeapon(string weapon)
    {
        if (photonView.IsMine == false)
        {
            FindObjectOfType<PUN_ItemManager>().createItem(weapon, PUN_ItemManager.EquipSide.Right, gameObject);
        }
    }
    [PunRPC]
    public void SetLeftWeapon(string weapon)
    {
        if (photonView.IsMine == false)
        {
            FindObjectOfType<PUN_ItemManager>().createItem(weapon, PUN_ItemManager.EquipSide.Left, gameObject);
        }
    }
    [PunRPC]
    public void ApplyDamage(string amount)
    {
        if (GetComponent<PhotonView>().IsMine == true)
        {
            vDamage damage = JsonUtility.FromJson<vDamage>(amount);
            GetComponent<vThirdPersonController>().TakeDamage(damage);
        }
    }
    [PunRPC]
    public void ResetTrigger(string name)
    {
        if (GetComponent<Animator>())
        {
            GetComponent<Animator>().ResetTrigger(name);
        }
    }
    [PunRPC]
    public void SetTrigger(string name)
    {
        if (GetComponent<Animator>())
        {
            GetComponent<Animator>().SetTrigger(name);
        }
    }
    [PunRPC]
    public void CrossFadeInFixedTime(string name, float time)
    {
        if (GetComponent<Animator>())
        {
            GetComponent<Animator>().CrossFadeInFixedTime(name, time);
        }
    }
    [PunRPC]
    public void SyncRotations(Quaternion head, Quaternion neck, Quaternion spine, Quaternion chest)
    {
        correctBoneHeadRot = head;
        correctBoneNeckRot = neck;
        correctBoneSpineRot = spine;
        correctBoneChestRot = chest;
    }
    [PunRPC]
    public void SetLayerWeight(int Layer, float weight) //provided by "pararini" on invector forums, thanks!
    {
        if (GetComponent<Animator>())
        {
            GetComponent<Animator>().SetLayerWeight(Layer, weight);
        }
    }
    //vShooterWeapon Functions
    [PunRPC]
    public void SendShootEffect(string handler, string weaponName)
    {
       foreach(vShooterWeapon weapon in GetComponentsInChildren<vShooterWeapon>(true))
        {
            if (weapon.transform.parent.transform.name == handler && weapon.transform.name == weaponName)
            {
                weapon.ShootEffect(transform);
            }
        }
    }
    [PunRPC]
    public void SendShootEffect(string aimPos, string handler, string weaponName)
    {
        foreach (vShooterWeapon weapon in GetComponentsInChildren<vShooterWeapon>(true))
        {
            if (weapon.transform.parent.transform.name == handler && weapon.transform.name == weaponName)
            {
                weapon.ShootEffect(JsonUtility.FromJson<Vector3>(aimPos), transform);
            }
        }
    }
    [PunRPC]
    public void SendReloadEffect(string handler, string weaponName)
    {
        foreach (vShooterWeapon weapon in GetComponentsInChildren<vShooterWeapon>(true))
        {
            if (weapon.transform.parent.transform.name == handler && weapon.transform.name == weaponName)
            {
                weapon.ReloadEffect();
            }
        }
    }
    [PunRPC]
    public void SendEmptyClipEffect(string handler, string weaponName)
    {
        foreach (vShooterWeapon weapon in GetComponentsInChildren<vShooterWeapon>(true))
        {
            if (weapon.transform.parent.transform.name == handler && weapon.transform.name == weaponName)
            {
                weapon.EmptyClipEffect();
            }
        }
    }
    [PunRPC]
    public void SendStopSound(string handler, string weaponName)
    {
        foreach (vShooterWeapon weapon in GetComponentsInChildren<vShooterWeapon>(true))
        {
            if (weapon.transform.parent.transform.name == handler && weapon.transform.name == weaponName)
            {
                weapon.StopSound();
            }
        }
    }
    #endregion

    #region Local Actions Based on Server Changes
    void Update()
    {
        if (GetComponent<PhotonView>().IsMine == false)
        {
            float distance = Vector3.Distance(transform.position, this.correctPlayerPos);
            if (distance < 2f)
            {
                transform.position = Vector3.Lerp(transform.position, this.correctPlayerPos, Time.deltaTime * _positionLerpRate);
                transform.rotation = Quaternion.Lerp(transform.rotation, this.correctPlayerRot, Time.deltaTime * _rotationLerpRate);
            }
            else
            {
                transform.position = this.correctPlayerPos;
                transform.rotation = this.correctPlayerRot;
            }
        }
    }
    void LateUpdate()
    {
        SyncBoneRotation();
    }
    void FixedUpdate()
    {
        if (_syncBones == true && GetComponent<PhotonView>().IsMine == true)
        {
            if (currentBoneRate == _syncBonesRate)
            {
                currentBoneRate = 0;
                photonView.RPC("SyncRotations", RpcTarget.Others, local_head.localRotation, local_neck.localRotation, local_spine.localRotation, local_chest.localRotation);
            }
            else
            {
                currentBoneRate += 1;
            }
        }
    }
    void SyncBoneRotation()
    {
        if (_syncBones == true && GetComponent<PhotonView>().IsMine == false)
        {
            local_head.localRotation = Quaternion.Lerp(local_head.localRotation, correctBoneHeadRot, Time.deltaTime * _boneLerpRate);
            local_neck.localRotation = Quaternion.Lerp(local_neck.localRotation, correctBoneNeckRot, Time.deltaTime * _boneLerpRate);
            local_spine.localRotation = Quaternion.Lerp(local_spine.localRotation, correctBoneSpineRot, Time.deltaTime * _boneLerpRate);
            local_chest.localRotation = Quaternion.Lerp(local_chest.localRotation, correctBoneChestRot, Time.deltaTime * _boneLerpRate);
        }
    }
    bool notNan(Quaternion value)
    {
        if (!float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) && !float.IsNaN(value.w))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    bool isQuaternionIdentity(Quaternion value)
    {
        if (value.ToString() == "(0.0, 0.0, 0.0, 1.0)")
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion
}

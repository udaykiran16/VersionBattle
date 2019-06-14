using Invector.vShooter;
using UnityEngine;
using Photon.Pun;

public class PUN_ShooterWeapon : vShooterWeapon {

    public override void ShootEffect(Transform sender = null)
    {
        base.ShootEffect(sender);
        if (PhotonNetwork.IsConnected == true && transform.root.GetComponent<PUN_SyncPlayer>() && transform.root.GetComponent<PhotonView>().IsMine == true)
        {
            sender.GetComponent<PhotonView>().RPC("SendShootEffect", RpcTarget.Others, transform.parent.name, transform.name);
        }
    }
    public override void ShootEffect(Vector3 aimPosition, Transform sender = null)
    {
        base.ShootEffect(aimPosition, sender);
        if (PhotonNetwork.IsConnected == true && transform.root.GetComponent<PUN_SyncPlayer>() && transform.root.GetComponent<PhotonView>().IsMine == true)
        {
            sender.GetComponent<PhotonView>().RPC("SendShootEffect", RpcTarget.Others, JsonUtility.ToJson(aimPosition), transform.parent.name, transform.name);
        }
    }
    public override void ReloadEffect()
    {
        base.ReloadEffect();
        if (PhotonNetwork.IsConnected == true && transform.root.GetComponent<PhotonView>() && transform.root.GetComponent<PhotonView>().IsMine == true)
        {
            transform.root.GetComponent<PhotonView>().RPC("SendReloadEffect", RpcTarget.Others, transform.parent.name, transform.name);
        }
    }
    public override void EmptyClipEffect()
    {
        base.EmptyClipEffect();
        if (PhotonNetwork.IsConnected == true && transform.root.GetComponent<PhotonView>() && transform.root.GetComponent<PhotonView>().IsMine == true)
        {
            transform.root.GetComponent<PhotonView>().RPC("SendEmptyClipEffect",RpcTarget.Others, transform.parent.name, transform.name);
        }
    }
    public override void StopSound()
    {
        base.StopSound();
        if (PhotonNetwork.IsConnected == true && transform.root.GetComponent<PhotonView>() && transform.root.GetComponent<PhotonView>().IsMine == true)
        {
            transform.root.GetComponent<PhotonView>().RPC("SendStopSound", RpcTarget.Others, transform.parent.name, transform.name);
        }
    }
}

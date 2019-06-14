using Invector;
using Photon.Pun;

public class PUN_WeaponHolder : vWeaponHolder {

    public override void SetActiveHolder(bool active)
    {
        base.SetActiveHolder(active);
        if (transform.root.gameObject.GetComponent<PhotonView>().IsMine == true)
        {
            transform.root.gameObject.GetComponent<PhotonView>().RPC("WeaponHolderSetActiveHolder",RpcTarget.OthersBuffered, active, itemID);
        }
    }

    public override void SetActiveWeapon(bool active)
    {
        base.SetActiveWeapon(active);
        if (transform.root.gameObject.GetComponent<PhotonView>().IsMine == true)
        {
            transform.root.gameObject.GetComponent<PhotonView>().RPC("WeaponHolderSetActiveWeapon", RpcTarget.OthersBuffered, active, itemID);
        }
    }
}

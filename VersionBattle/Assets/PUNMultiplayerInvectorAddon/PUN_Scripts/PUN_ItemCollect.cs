using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PUN_ItemCollect : MonoBehaviour {

    public void NetworkDestory()
    {
        if (PhotonNetwork.IsConnected == true)
        {
            GetComponent<PhotonView>().RPC("ItemDestroy", RpcTarget.AllBuffered, gameObject.GetComponent<PhotonView>().ViewID);
        }
    }
    [PunRPC]
    public void ItemDestroy(int viewId)
    {
        foreach (PhotonView view in FindObjectsOfType<PhotonView>())
        {
            if (view.ViewID == viewId)
            {
                Destroy(view.gameObject);
                break;
            }
        }
    }
}

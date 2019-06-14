using UnityEngine;
using Invector.vCamera;
using Photon.Pun;

public class PUN_ThirdPersonCameraVerify : MonoBehaviour
{
    private void Start()
    {
        if (GetComponent<PhotonView>().IsMine == false && PhotonNetwork.IsConnected == true)
        {
            FindObjectOfType<vThirdPersonCamera>().target = transform;
        }
    }
}

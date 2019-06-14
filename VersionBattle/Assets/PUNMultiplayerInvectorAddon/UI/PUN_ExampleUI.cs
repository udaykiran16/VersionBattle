using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PUN_ExampleUI : MonoBehaviour
{
    public InputField RoomNameInput;
    public GameObject AvailableRooms;
    public GameObject roomButton;
    public Text[] connectionStatus;

    private PUN_NetworkManager nm;
    private string prev_status = "";
    private Dictionary<string, RoomInfo> roomList = new Dictionary<string, RoomInfo>();

    public void Start()
    {
        nm = FindObjectOfType<PUN_NetworkManager>();
    }
    private void Update()
    {
        if (prev_status != nm._connectStatus)
        {
            prev_status = nm._connectStatus;
            SetConnectionStatus(prev_status);
        }
    }

    public void HostAGame()
    {
        nm.CreateRoom(RoomNameInput.text);
    }

    public void JoinDefaultLobby()
    {
        nm.JoinLobby();
    }
    public void LeaveDefaultLobby()
    {
        nm.LeaveLobby();
    }
    void SetConnectionStatus(string status)
    {
        foreach(Text conn in connectionStatus)
        {
            conn.text = status;
        }
    }
    public void RefreshRoomList()
    {
        roomList = nm.cachedRoomList;
        foreach (Transform child in AvailableRooms.transform)
        {
            Destroy(child.gameObject);
        }
        GameObject room = null;
        foreach (RoomInfo roomInfo in roomList.Values)
        {
            room = Instantiate(roomButton) as GameObject;
            room.transform.SetParent(AvailableRooms.transform);
            room.GetComponentInChildren<Text>().text = "ROOM NAME: " + roomInfo.Name + ", PLAYERS: " + roomInfo.PlayerCount + ", JOINABLE: " + roomInfo.IsOpen;
            room.GetComponent<Button>().onClick.AddListener(() => nm.JoinRoom(roomInfo.Name));
        }
    }
}

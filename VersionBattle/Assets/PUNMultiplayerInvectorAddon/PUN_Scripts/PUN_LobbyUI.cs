﻿using UnityEngine;
using UnityEngine.UI;

public class PUN_LobbyUI : MonoBehaviour {

    private string _playerName = "MyPlayerName";
    private bool showConnectionStatus = false;
    private bool connected = false;
    // Use this for initialization
    void JoinLobby()
    {
        PUN_NetworkManager nm = GameObject.FindObjectOfType<PUN_NetworkManager>();
        nm.SetPlayerName(_playerName);
        nm.JoinLobby();
    }
    private void OnGUI()
    {
        if (connected == false)
        {
            if (showConnectionStatus == false)
            {
                _playerName = GUI.TextField(new Rect(50, 50, 200, 20), _playerName);
                if (GUI.Button(new Rect(50, 90, 200, 30), "Join Lobby"))
                {
                    showConnectionStatus = true;
                    JoinLobby();
                }
            }
            else
            {
                GUI.TextArea(new Rect(50, 50, 300, 100), GetComponent<PUN_NetworkManager>()._connectStatus);
                if (GetComponent<PUN_NetworkManager>()._connecting == false)
                {
                    connected = true;
                    showConnectionStatus = false;
                }
            }
        }
        else
        {
            if (GUI.Button(new Rect(Screen.width-150, Screen.height-50 , 100, 30), "Leave Room"))
            {
                connected = false;
                GetComponent<PUN_NetworkManager>().LeaveRoom();
            }
        }
    }
}

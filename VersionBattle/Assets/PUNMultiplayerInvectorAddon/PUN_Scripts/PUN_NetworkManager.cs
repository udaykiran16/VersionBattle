using UnityEngine;
using Photon.Pun;               //to acces Photon features
using Photon.Realtime;          //to access Photon callbacks
using UnityEngine.Events;       //to call actions on various states
using System.Collections.Generic;

[System.Serializable]
public class PlayerEvent : UnityEvent<Player> { }

[System.Serializable]
public class NetworkEvents
{
    public LobbyEvents _lobbyEvents;
    public RoomEvents _roomEvents;
    public PlayerEvents _playerEvents;
    public OtherEvents _otherEvents;
}
[System.Serializable]
public class LobbyEvents
{
    public UnityEvent _onJoinedLobby;
}
[System.Serializable]
public class RoomEvents
{
    public UnityEvent _onJoinedRoom;
    public UnityEvent _onLeftRoom;
    public UnityEvent _OnCreatedRoom;
    public UnityEvent _onJoinRoomFailed;
    public UnityEvent _onCreateRoomFailed;
}
[System.Serializable]
public class PlayerEvents
{
    public PlayerEvent _onPlayerEnteredRoom;
    public PlayerEvent _onPlayerLeftRoom;
}
[System.Serializable]
public class OtherEvents
{
    public UnityEvent _onMasterClientSwitched;
    public UnityEvent _onDisconnected;
    public UnityEvent _onConnectedToMaster;
    public UnityEvent _onFailedToConnectToPhoton;
    public UnityEvent _onConnectionFail;
}
public class PUN_NetworkManager : MonoBehaviourPunCallbacks
{

    #region Modifiables
    [Tooltip("The version to connect with. Incompatible versions will not connect with each other.")]
    public string _gameVersion = "1.0";
    [Tooltip("The max number of player per room. When a room is full, it can't be joined by new players, and so a new room will be created.")]
    [SerializeField] private byte maxPlayerPerRoom = 4;
    [Tooltip("The _prefab that will be spawned in when a player successfully connects.")]
    public GameObject _playerPrefab = null;
    [Tooltip("The point where the player will start when they have successfully connected.")]
    public Transform _spawnPoint = null;
    [Tooltip("Shows the current connection process. This is great for UI to reference and use.")]
    public string _connectStatus = "";
    [Tooltip("Automatically sync all connected clients scenes. Make sure everyone is always on the same scene together.")]
    public bool _syncScenes = true;
    public bool debugging = false;
    [Tooltip("Custom Events based on network actions.")]
    public NetworkEvents _customNetworkEvents;

    [HideInInspector] public bool _connecting = false;
    #endregion

    #region Internal Use Variables
    private PUN_NetworkManager nm = null;

    private string _hostRoomName;
    public Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private bool _joinLobby = false;
    private bool _joinRoom = false;
    private bool _createRoom = false;
    private string _roomName = "";
    #endregion

    #region LocalMethods
    private void Awake()
    {
        if (nm == null)
        {
            nm = this;
            DontDestroyOnLoad(this.gameObject);
            this.gameObject.name = gameObject.name + " Instance";
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
        PhotonNetwork.AutomaticallySyncScene = _syncScenes; //Automatically load scenes together (make sure everyone is always on the same scene)
    }
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // Remove room from cached room list if it got closed, became invisible or was marked as removed
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }

                continue;
            }

            // Update cached room info
            if (cachedRoomList.ContainsKey(info.Name))
            {
                cachedRoomList[info.Name] = info;
            }
            // Add new room info to cache
            else
            {
                cachedRoomList.Add(info.Name, info);
            }
        }
    }
    private void ResetJoinValues()
    {
        _joinLobby = false;
        _joinRoom = false;
        _roomName = "";
        _createRoom = false;
    }
    #endregion

    #region Callable Methods
    /// <summary>
    /// Set the players network name
    /// </summary>
    /// <param name="name"></param>
    public void SetPlayerName(string name = "")
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
        PhotonNetwork.NickName = (name == "") ? "Un-named Player" : name;
    }

    /// <summary>
    /// Load a target level across the network. Only MasterClient can call this.
    /// </summary>
    /// <param name="level"></param>
    public void NetworkLoadLevel(int level)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            _connectStatus = "Starting loading a new level...";
            if (debugging == true)
            {
                Debug.Log(_connectStatus);
            }
            PhotonNetwork.LoadLevel(level);
        }
    }

    /// <summary>
    /// Disconnect from the PUN master server. Will be dropped from any lobby or room.
    /// </summary>
    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    /// <summary>
    /// Create a room with the target name for other players to join in the default lobby. Will connect to master server and default lobby if not already connected.
    /// </summary>
    /// <param name="roomName"></param>
    public void CreateRoom(string roomName)
    {
        _connecting = true;
        _roomName = roomName;
        if (PhotonNetwork.IsConnected == false || PhotonNetwork.InLobby == false)
        {
            _createRoom = true;
            JoinLobby();
        }
        else
        {
            _connectStatus = "Creating a new room...";
            if (debugging == true)
            {
                Debug.Log(_connectStatus);
            }
            RoomOptions options = new RoomOptions() { MaxPlayers = maxPlayerPerRoom, PublishUserId = true, IsVisible = true, IsOpen = true };
            PhotonNetwork.CreateRoom(_roomName, options, TypedLobby.Default);
        }
    }

    /// <summary>
    /// Makes the caller leave the room they are connected to but stay connected to default lobby and master server.
    /// </summary>
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    /// <summary>
    /// Attempt to join a random room in your connected lobby. Will join master server and defaul lobby if not already connected prior to joining.
    /// </summary>
    public void JoinRandomRoom()
    {
        _connecting = true;
        _roomName = "";
        if (PhotonNetwork.IsConnected == false || PhotonNetwork.InLobby == false)
        {
            _joinRoom = true; //will join the room after connecting to master server and joining lobby.
            JoinLobby(); // will connect to server and join lobby.
        }
        else
        {
            _connectStatus = "Attempting to join a random room... ";
            if (debugging == true)
            {
                Debug.Log(_connectStatus);
            }
            PhotonNetwork.JoinRandomRoom();
        }
    }

    /// <summary>
    /// Join a room with name in your connected lobby. Will join master server and defaul lobby if not already connected prior to joining.
    /// </summary>
    /// <param name="roomName"></param>
    public void JoinRoom(string roomName)
    {
        _connecting = true;
        _roomName = roomName;
        if (PhotonNetwork.IsConnected == false || PhotonNetwork.InLobby == false)
        {
            _joinRoom = true;
            JoinLobby();
            
        }
        else
        {
            _connectStatus = "Attempting to join room: " + roomName;
            if (debugging == true)
            {
                Debug.Log(_connectStatus);
            }
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    /// <summary>
    /// Leave your currently connected lobby but stay connect to the master server.
    /// </summary>
    public void LeaveLobby()
    {
        if (PhotonNetwork.IsConnected == true)
        {
            if (PhotonNetwork.InLobby == true)
            {
                PhotonNetwork.LeaveLobby();
            }
        }
    }

    /// <summary>
    /// Join the default lobby. If not connected will connect to master server first. If already in lobby, will do nothing.
    /// </summary>
    public void JoinLobby()
    {
        _connecting = true;
        _connectStatus = "Attempting to join the lobby...";
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        if (PhotonNetwork.IsConnected == true)
        {
            if (PhotonNetwork.InLobby == false)
            {
                PhotonNetwork.JoinLobby(TypedLobby.Default);
            }
        }
        else
        {
            _joinLobby = true;
            ConnectToMasterServer();
        }
    }

    /// <summary>
    /// Connect to the PUN master server.
    /// </summary>
    public void ConnectToMasterServer()
    {
        _connecting = true;
        _connectStatus = "Attempting to join the master server...";
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        PhotonNetwork.GameVersion = _gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>
    /// Returns the numbers of players connected to this room
    /// </summary>
    /// <returns>int PlayerCount</returns>
    public int GetPlayerCount()
    {
        return PhotonNetwork.CurrentRoom.PlayerCount;
    }
    #endregion

    #region Photon Callback Methods
    //PlayerEvents
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        _customNetworkEvents._playerEvents._onPlayerEnteredRoom.Invoke(newPlayer);
        base.OnPlayerEnteredRoom(newPlayer);
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        _customNetworkEvents._playerEvents._onPlayerLeftRoom.Invoke(otherPlayer);
        base.OnPlayerLeftRoom(otherPlayer);
    }

    //MasterClient Events
    public override void OnConnectedToMaster()
    {
        _connecting = false;
        _connectStatus = "Connected to the master server.";
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        base.OnConnectedToMaster();
        _customNetworkEvents._otherEvents._onConnectedToMaster.Invoke();
        if (_joinLobby == true)
        {
            JoinLobby();
        }
        else if (_joinRoom == true)
        {
            if (PhotonNetwork.InLobby == false)
            {
                JoinLobby();
            }
            else if (_roomName == "")
            {
                JoinRandomRoom();
            }
            else
            {
                JoinRoom(_roomName);
            }
        }
        else if (_createRoom == true)
        {
            CreateRoom(_roomName);
        }
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        if (debugging == true)
        {
            Debug.Log("Switched Master Client");
        }
        _customNetworkEvents._otherEvents._onMasterClientSwitched.Invoke();
    }

    //Fails/Disconnects
    public override void OnDisconnected(DisconnectCause cause)
    {
        _connecting = false;
        ResetJoinValues();
        _connectStatus = "Disconnected: " + cause;
        base.OnDisconnected(cause);
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        _customNetworkEvents._otherEvents._onDisconnected.Invoke();
    }
    public void OnConnectionFail(DisconnectCause cause)
    {
        _connecting = false;
        ResetJoinValues();
        _connectStatus = "Failed to connect, reason: " + cause;
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        _customNetworkEvents._otherEvents._onConnectionFail.Invoke();
    }
    public void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        _connecting = false;
        ResetJoinValues();
        _connectStatus = "Failed to connect to the master server: " + cause;
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        _customNetworkEvents._otherEvents._onFailedToConnectToPhoton.Invoke();
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        _connecting = false;
        ResetJoinValues();
        _connectStatus = "Failed to find a room, with error: ("+returnCode+") "+message;
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        base.OnJoinRandomFailed(returnCode, message);
        _customNetworkEvents._roomEvents._onJoinRoomFailed.Invoke();
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        _connecting = false;
        ResetJoinValues();
        _connectStatus = "Failed to join room: (" + returnCode + ") " + message;
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        base.OnJoinRoomFailed(returnCode, message);
        _customNetworkEvents._roomEvents._onJoinRoomFailed.Invoke();
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        _connecting = false;
        ResetJoinValues();
        _connectStatus = "Failed to create a room: (" + returnCode + ") " + message;
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        base.OnCreateRoomFailed(returnCode, message);
        _customNetworkEvents._roomEvents._onCreateRoomFailed.Invoke();
    }

    //Room Events
    public override void OnJoinedRoom()
    {
        _connecting = false;
        ResetJoinValues();
        _connectStatus = "Successfully joined a room";
        if (_playerPrefab != null)
        {
            PhotonNetwork.Instantiate(_playerPrefab.name, _spawnPoint.position, _spawnPoint.rotation, 0);
        }
        _customNetworkEvents._roomEvents._onJoinedRoom.Invoke();
        base.OnJoinedRoom();
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
    }
    public override void OnLeftRoom()
    {
        _connecting = false;
        _connectStatus = "Left Room.";
        ResetJoinValues();
        _customNetworkEvents._roomEvents._onLeftRoom.Invoke();
        base.OnLeftRoom();
        if (debugging == true)
        {
            Debug.Log("Left Room.");
        }
    }
    public override void OnCreatedRoom()
    {
        _connecting = false;
        ResetJoinValues();
        _connectStatus = "Successfully created room: " + _hostRoomName;
        base.OnCreatedRoom();
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        _customNetworkEvents._roomEvents._OnCreatedRoom.Invoke();
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        if (debugging == true)
        {
            Debug.Log("Updated room list, count: " + roomList.Count);
        }
        UpdateCachedRoomList(roomList);
    }

    //Lobby Events 
    public override void OnJoinedLobby()
    {
        _connecting = false;
        _joinLobby = false;
        _connectStatus = "Succesfully joined the server lobby: DefaultLobby=" + PhotonNetwork.CurrentLobby.IsDefault;
        if (debugging == true)
        {
            Debug.Log(_connectStatus);
        }
        base.OnJoinedLobby();
        _customNetworkEvents._lobbyEvents._onJoinedLobby.Invoke();
        if (_joinRoom == true)
        {
            if (_roomName == "")
            {
                JoinRandomRoom();
            }
            else
            {
                JoinRoom(_roomName);
            }
        }
        else if (_createRoom == true)
        {
            CreateRoom(_roomName);
        }
    }
    #endregion
}

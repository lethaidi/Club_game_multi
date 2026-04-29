using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomList : MonoBehaviourPunCallbacks
{
    public static RoomList instance;

    public GameObject roomMana;
    public RoomManager roomManager;

    [Header("UI")]
    public Transform contain;
    public GameObject roomListPrefab;

    public List<RoomInfo> list = new List<RoomInfo>();

    [Header("Create Room")]
    private string roomName = "Map1";
    private string roomMapName = "Map1";
    public int maxPlayerSet = 0;

    public bool cursorLocked = false;

    void Awake()
    {
        //Mở cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        maxPlayerSet = 0;
        instance = this;
        PhotonNetwork.AutomaticallySyncScene = true; 
    }

    IEnumerator Start()
    {
        // 1. Nếu vô tình vẫn đang kẹt trong phòng, thì thoát ra trước
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            yield return new WaitUntil(() => !PhotonNetwork.InRoom);
        }

        // 2. KHÔNG ngắt kết nối (Disconnect). Chỉ kết nối nếu thực sự chưa kết nối.
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            // 3. Nếu đã kết nối sẵn (do đi từ Scene Game về Menu), 
            // thì chỉ việc xin vào lại Lobby để lấy danh sách phòng.
            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ChangeCursorState();
        }
    }

    public void ChangeCursorState()
    {
        cursorLocked = !cursorLocked;
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        Debug.Log("Connected to Sever");
        PhotonNetwork.JoinLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            int index = list.FindIndex(x => x.Name == room.Name);

            if (room.RemovedFromList)
            {
                if (index != -1)
                    list.RemoveAt(index);
            }
            else
            {
                if (index != -1)
                {
                    list[index] = room; // update
                }
                else
                {
                    list.Add(room);
                }
            }
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        // 1. Kiểm tra xem đã gán contain và prefab trong Inspector chưa
        if (contain == null)
        {
            Debug.LogError("LỖI: Biến 'contain' chưa được gán trong Inspector!");
            return;
        }
        if (roomListPrefab == null)
        {
            Debug.LogError("LỖI: Biến 'roomListPrefab' chưa được gán trong Inspector!");
            return;
        }

        // Xóa các room item cũ
        foreach (Transform child in contain)
        {
            Destroy(child.gameObject);
        }

        // Tạo danh sách room mới
        foreach (var room in list)
        {
            GameObject _room = Instantiate(roomListPrefab, contain);

            // 2. Lấy các component một lần (Tránh gọi GetComponent nhiều lần gây nặng máy)
            RoomItemButton roomItemBtn = _room.GetComponent<RoomItemButton>();
            Button btn = _room.GetComponent<Button>();
            Image img = _room.GetComponent<Image>();

            // 3. Kiểm tra xem Prefab có thiếu script RoomItemButton không
            if (roomItemBtn == null)
            {
                Debug.LogError("LỖI: Prefab 'roomListPrefab' đang bị thiếu script RoomItemButton!");
                continue;
            }

            // Lấy Custom Properties an toàn
            string roomMapName = " ";
            if (room.CustomProperties.TryGetValue("roomMapName", out object nameMapObj))
            {
                roomMapName = (string)nameMapObj;
            }

            string roomName = " ";
            if (room.CustomProperties.TryGetValue("mapName", out object nameObj))
            {
                roomName = (string)nameObj;
            }

            // 4. Kiểm tra an toàn cho TextMeshPro con
            if (_room.transform.childCount >= 2)
            {
                var textName = _room.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                var textPlayers = _room.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

                if (textName != null) textName.text = roomName + " (" + roomMapName + ")";
                if (textPlayers != null) textPlayers.text = room.PlayerCount + "/" + room.MaxPlayers;
            }
            else
            {
                Debug.LogWarning("CẢNH BÁO: Prefab không có đủ 2 object con (Child) để hiển thị Text!");
            }

            // Gán thông số cho RoomItemButton
            roomItemBtn.maxPlayer = room.MaxPlayers;
            roomItemBtn.currentPlayer = room.PlayerCount;
            roomItemBtn.roomName = room.Name;

            int mapIndex = 0;
            if (room.CustomProperties.TryGetValue("mapSceneIndex", out object mapIndexObj))
            {
                mapIndex = (int)mapIndexObj;
            }
            roomItemBtn.roomIndex = mapIndex;

            // Xử lý khi phòng đầy
            if (room.PlayerCount >= room.MaxPlayers || !room.IsOpen)
            {
                if (btn != null) btn.interactable = false;
                else Debug.LogWarning("CẢNH BÁO: Prefab thiếu component Button!");

                if (img != null) img.color = Color.gray;
                else Debug.LogWarning("CẢNH BÁO: Prefab thiếu component Image!");

                if (roomItemBtn != null) roomItemBtn.mapKeyNotice2.SetActive(true);
            }

            string mapKey = " ";
            if (room.CustomProperties.TryGetValue("mapKey", out object keyObj))
            {
                mapKey = (string)keyObj;
            }
            roomItemBtn.mapKey = mapKey;
        }
    }

    public void ChangeRoomToCreate(string name)
    {
        roomName = name;
    }

    public void JoinRoomByName(string name)
    {
        Debug.Log("Đang kết nối vào phòng...");
        PhotonNetwork.JoinRoom(name);
    }

    //CreatRoom sẽ gọi hàm này để tạo phòng với map index tương ứng
    public void JoinButtonPressed(int mapIndex)
    {
        if (maxPlayerSet == 0)
        {
            Debug.LogError("LỖI: Vui lòng chọn số người chơi trước khi tạo phòng!");
            return;
        }

        Debug.Log("Creating Room...");

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = maxPlayerSet;

        if (mapIndex == 1) roomMapName = "Map1";
        else if (mapIndex == 2) roomMapName = "Map2";
        else if (mapIndex == 3) roomMapName = "Map3";

        string randomKey = RandomMapKey();

        options.CustomRoomProperties = new Hashtable()
        {
            {"mapSceneIndex", mapIndex},
            {"mapName", roomName},
            {"mapKey", randomKey},
            {"roomMapName", roomMapName}
        };

        options.CustomRoomPropertiesForLobby = new[]
        {
            "mapSceneIndex",
            "mapName",
            "mapKey",
            "roomMapName"
        };

        PhotonNetwork.CreateRoom(roomName, options); 
        ClockCursor();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Created Room SUCCESS");

        int mapIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["mapSceneIndex"];

        PhotonNetwork.LoadLevel(mapIndex);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient) return; //tránh host load 2 lần

        Debug.Log("Joined Room (Client)");

        int mapIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["mapSceneIndex"];
    }

    string RandomMapKey()
    {
        int length = 6;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        }

        return new string(result);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ChangeMaxPlayer(string index)
    {
        // kiểm tra index phải là 2, 3 hoặc 4
        if (index != "2" && index != "3" && index != "4")
        {
            Debug.LogError("LỖI: Số người chơi phải là 2, 3 hoặc 4!");
            maxPlayerSet = 0;
            return;
        }
        maxPlayerSet = int.Parse(index);
    }

    public void ClockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

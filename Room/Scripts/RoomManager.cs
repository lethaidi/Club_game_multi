using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager instance;

    [Header("Player")]
    public GameObject[] player;
    public Transform[] spawnPoint;
    public Material[] playerSkins; 

    [Header("UI")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI keyText;
    public GameObject losePanel, winPanel;

    private string roomName;
    private string mapKey;

    public bool isWin = false, ready = false;
    public int playerCount = 0;
    public TextMeshProUGUI playerCountText;
    
    public bool cursorLocked = true;


    [Header("Time Limit")]
    public float timeLimit = 180f; // 3 phút
    public GameObject clubItem;
    public Transform pivot;
    public float radius = 5f;
    private bool startTiming = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Debug.Log("RoomManager Start - Đang đợi Photon đồng bộ...");

        // Gọi Coroutine bắt đầu đợi
        StartCoroutine(WaitForRoomAndSetup());
    }

    void Update()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        playerCountText.text = playerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;

        // KIỂM TRA ĐỦ NGƯỜI ĐỂ SẴN SÀNG
        if (playerCount == PhotonNetwork.CurrentRoom.MaxPlayers && !ready)
        {
            RoomGroundCheck.instance.isReady = true;
            ready = true;

            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.IsOpen)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false; // Đóng cửa! Không ai được vào nữa.
                Debug.Log("Phòng đã đầy! Khóa phòng thành công.");
            }
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1 && ready && !isWin)
        {
            //Win
            Win();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ChangeCursorState();
        }

        if (!ready) return;
        if (!startTiming)
        {
            startTiming = true;
            StartCoroutine(OverTime());
        }
    }

    IEnumerator WaitForRoomAndSetup()
    {
        // Đợi vào phòng
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        Debug.Log("RoomManager: In Room (Đồng bộ thành công!)");

        // NẾU LÀ MASTER CLIENT VÀ PHÒNG CHƯA CÓ LIST RANDOM NHÂN VẬT
        if (PhotonNetwork.IsMasterClient && !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CharOrder"))
        {
            // Tạo mảng thứ tự dựa trên số lượng prefab (ví dụ bạn có 4 prefab thì mảng là 0, 1, 2, 3)
            int[] order = new int[player.Length];
            for (int i = 0; i < player.Length; i++) order[i] = i;

            // Xáo trộn mảng (Shuffle)
            for (int i = 0; i < order.Length; i++)
            {
                int temp = order[i];
                int randomIndex = Random.Range(i, order.Length);
                order[i] = order[randomIndex];
                order[randomIndex] = temp;
            }

            // Lưu mảng đã xáo trộn lên Room Properties của Photon
            Hashtable roomProps = new Hashtable();
            roomProps.Add("CharOrder", order);
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }

        // BẮT BUỘC ĐỢI: Chờ cho đến khi Room Properties đã có "CharOrder" (dành cho client vào sau không bị lỗi)
        yield return new WaitUntil(() => PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CharOrder"));

        GetRoomInfo();
        UpdateUI();
        SpawnPlayer();
    }

    // ================= GET DATA =================
    void GetRoomInfo()
    {
        // Tên room
        roomName = PhotonNetwork.CurrentRoom.Name;

        // Key
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("mapKey"))
        {
            mapKey = PhotonNetwork.CurrentRoom.CustomProperties["mapKey"].ToString();
        }
        else
        {
            mapKey = "----";
        }
    }

    void UpdateUI()
    {
        if (roomNameText != null)
            roomNameText.text = "Room: " + roomName;

        if (keyText != null)
            keyText.text = "Key: " + mapKey;
    }

    // ================= SPAWN =================
    void SpawnPlayer()
    {
        if (PhotonNetwork.LocalPlayer.TagObject != null) return;

        // Lấy số thứ tự của người chơi hiện tại (Bắt đầu từ 1, 2, 3, 4...)
        int playerID = PhotonNetwork.LocalPlayer.ActorNumber;

        // Tính index an toàn bằng phép chia lấy dư
        int safeIndex = (playerID - 1) % player.Length;

        // 1. Lấy vị trí SpawnPoint
        Transform safeSpawnPoint = spawnPoint[safeIndex % spawnPoint.Length];

        // 2. Lấy mảng nhân vật đã xáo trộn trên mạng về
        int[] charOrder = (int[])PhotonNetwork.CurrentRoom.CustomProperties["CharOrder"];

        // 3. Chỉ định index nhân vật từ mảng đó (Đảm bảo 100% random và không trùng)
        int prefabIndex = charOrder[safeIndex];

        // 4. Instantiate nhân vật qua mạng (Bạn phải để Prefab trong thư mục Resources)
        GameObject _player = PhotonNetwork.Instantiate(player[prefabIndex].name, safeSpawnPoint.position, safeSpawnPoint.rotation);

        PhotonNetwork.LocalPlayer.TagObject = _player;
        _player.GetComponent<PlayerSetUp>().IsLocalPlayer();
    }

    // ================= LEAVE ROOM =================
    public void LeaveRoom()
    {
        // 1. Reset TagObject về null để lần sau vào phòng nó chịu Spawn
        PhotonNetwork.LocalPlayer.TagObject = null;

        // 2. Chỉ gọi rời phòng, tuyệt đối KHÔNG LoadScene ở đây
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene(0); // Nếu chơi offline thì load thẳng
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }

    public void Win()
    {
        isWin = true;
        cursorLocked = false;
        ChangeCursorState();
        winPanel.SetActive(true);
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

    IEnumerator OverTime()
    {
        yield return new WaitForSeconds(timeLimit);
        if (!isWin)
        {
            SpawnClubItem();
        }
    }

    void SpawnClubItem()
    {
        if (!PhotonNetwork.IsMasterClient) return; // Chỉ Master Client mới được spawn
        if (clubItem != null && pivot != null)
        {
            Vector3 randomPos = pivot.position + Random.insideUnitSphere * radius;
            randomPos.y = pivot.position.y; // Giữ nguyên chiều cao
            PhotonNetwork.Instantiate(clubItem.name, randomPos, Quaternion.identity);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (pivot != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pivot.position, radius);
        }
    }
}
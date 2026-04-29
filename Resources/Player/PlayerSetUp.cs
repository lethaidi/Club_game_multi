using UnityEngine;
using Photon.Pun;

public class PlayerSetUp : MonoBehaviourPun
{
    public PlayerMove move;
    public Renderer _playerSkin;

    void Start()
    {
        //SetPlayerSkin();
    }

    void SetPlayerSkin()
    {
        // 1. Lấy ID của người sở hữu nhân vật này (sẽ chuẩn trên mọi máy)
        int playerID = photonView.Owner.ActorNumber;

        // 2. Tính toán Index y hệt như bên RoomManager
        // Lưu ý: Đảm bảo spawnPoint.Length không bằng 0 để tránh lỗi chia cho 0
        int spawnIndex = (playerID - 1) % RoomManager.instance.spawnPoint.Length;

        // 3. Đổi skin bằng cách lấy mảng playerSkins từ RoomManager.instance
        if (_playerSkin != null && RoomManager.instance != null)
        {
            _playerSkin.material = RoomManager.instance.playerSkins[spawnIndex];
        }
    }

    public void IsLocalPlayer()
    {
        move.enabled = true;
    }
}

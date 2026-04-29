using UnityEngine;
using Photon.Pun;

public class RoomGroundCheck : MonoBehaviour
{
    public static RoomGroundCheck instance;

    public int count = 0, total = 0;
    public bool isWin = false, isReady = false;
    public PlayerMove pl;

    void Awake()
    {
        instance = this;
        total = PhotonNetwork.CurrentRoom.MaxPlayers;
    }

    void Update()
    {
        if ((total - count) == 1 && !isWin && isReady)
        {
            if (!pl.inPlane) return;
            isWin = true;
            RoomManager.instance.Win();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            count++;
        }
    }
}

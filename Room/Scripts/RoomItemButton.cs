using UnityEngine;
using System.Collections;

public class RoomItemButton : MonoBehaviour
{
    public int maxPlayer, currentPlayer;    
    public string roomName;
    public int roomIndex;
    public string mapKey = "", inputKey = "";

    public GameObject mapKeyNotice;
    public GameObject mapKeyNotice1;
    public GameObject mapKeyNotice2;

    private Coroutine noticeCoroutine;
    private Coroutine noticeCoroutine1;

    public bool isFulled = false;

    public void OnClick()
    {
        if (isFulled) return;

        if (maxPlayer == currentPlayer)
        {
            if (noticeCoroutine1 != null)
                StopCoroutine(noticeCoroutine1);

            noticeCoroutine1 = StartCoroutine(Notice(2f, mapKeyNotice1));

            Debug.Log("Room is full.");
            return;
        }

        if (string.IsNullOrEmpty(mapKey) ||
            string.IsNullOrEmpty(inputKey) ||
            mapKey != inputKey)
        {
            Debug.Log("Map key does not match input key.");

            if (noticeCoroutine != null)
                StopCoroutine(noticeCoroutine);

            noticeCoroutine = StartCoroutine(Notice(2f, mapKeyNotice));
            return;
        }

        RoomList.instance.JoinRoomByName(roomName); 
        RoomList.instance.ClockCursor();
    }

    IEnumerator Notice(float time, GameObject obj)
    {
        obj.SetActive(true);

        yield return new WaitForSeconds(time);

        obj.SetActive(false);

        noticeCoroutine = null;
    }

    public void ChangeInputKey(string key)
    {
        inputKey = key;
    }
}

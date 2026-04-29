using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public string targetTag = "Player";

    public float distance = 7f;
    public float height = 3f;

    public float mouseSensitivity = 100f;
    public float minY = -30f;
    public float maxY = 60f;

    float yaw = 0f;
    float pitch = 20f;

    void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
        pitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minY, maxY);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 offset = rotation * new Vector3(0, height, -distance);

        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(targetTag);

        foreach (GameObject player in players)
        {
            Photon.Pun.PhotonView pv = player.GetComponent<Photon.Pun.PhotonView>();

            if (pv != null && pv.IsMine)
            {
                PlayerMove pl = player.GetComponent<PlayerMove>();

                if (pl != null)
                {
                    pl.cameraTransform = transform;
                    target = player.transform;
                    break;
                }
            }
        }
    }
}
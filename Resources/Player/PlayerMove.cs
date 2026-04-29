using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using System.Collections; // BẮT BUỘC PHẢI CÓ ĐỂ DÙNG IEnumerator

public class PlayerMove : MonoBehaviour
{
    public static PlayerMove instance;
    [Header("Collision/Bounce")]
    public float bounceForce = 10f;
    private float lastBounceTime = 0f;
    private float bounceCooldown = 0.2f;

    [Header("Movement")]
    public float moveForce = 20f;
    public float maxSpeed = 6f;
    public float drag = 2f;

    [Header("Rotation")]
    public float rotateInPlaceSpeed = 100f;
    public bool canRotatingInPlace = true;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Lose System")]
    public GameObject losePanel;
    public bool inPlane = true;
    public bool eatItem = false;

    PhotonView pv;
    Rigidbody rb;
    bool isMoving;

    [Header("Sounds")]
    public AudioSource interactSound;

    [Header("VFX")]
    public ParticleSystem hitVFX;

    void Awake()
    {
        instance = this;

        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (rb != null)
        {
            rb.linearDamping = drag;
            rb.angularDamping = 5f;
        }
    }

    void Update()
    {
        if (!pv.IsMine) return;
        if (cameraTransform == null) return;

        RotateInPlace();

        // Nếu đã rớt khỏi bàn (thua), không cho phép thao tác input nữa
        if (!inPlane) return;
        //Kiem tra roomManager co ready chua
        if (!RoomManager.instance.ready) return;

        // Input
        isMoving = Keyboard.current.wKey.isPressed;
        canRotatingInPlace = !isMoving;

        if (RoomManager.instance.isWin)
        {
            inPlane = false;
        }
    }

    void FixedUpdate()
    {
        //Kiem tra roomManager co ready chua
        if (!RoomManager.instance.ready) return;

        if (!pv.IsMine) return;

        // Nếu đã thua thì không add lực nữa
        if (!inPlane) return;

        if (isMoving)
        {
            rb.AddForce(transform.forward * moveForce, ForceMode.Acceleration);
        }

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);

        if (horizontalVel.magnitude > maxSpeed && (Time.time - lastBounceTime > 0.5f))
        {
            horizontalVel = horizontalVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVel.x, velocity.y, horizontalVel.z);
        }
    }

    void RotateInPlace()
    {
        if (!canRotatingInPlace) return;
        transform.Rotate(Vector3.up, rotateInPlaceSpeed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (pv == null || rb == null) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // CHỈ xử lý cho nhân vật của người chơi hiện tại (Local)
            // Vì máy người kia cũng sẽ tự động chạy đoạn code này cho nhân vật của họ
            if (pv.IsMine)
            {
                // --- 1. LẤY ĐIỂM VA CHẠM VÀ PHÁT VFX/ÂM THANH TỨC THỜI ---
                Vector3 hitPoint = collision.GetContact(0).point;

                if (hitVFX != null)
                {
                    hitVFX.transform.position = hitPoint;
                    hitVFX.Play();
                }

                if (interactSound != null)
                {
                    interactSound.Play();
                }

                // --- 2. TÍNH TOÁN VÀ ÁP DỤNG LỰC NẢY MƯỢT MÀ ---
                Vector3 bounceDirection = transform.position - collision.transform.position;
                bounceDirection.y = 0;
                bounceDirection = bounceDirection.normalized;

                if (bounceDirection == Vector3.zero)
                {
                    bounceDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                }

                ApplyBounceLocal(bounceDirection);
            }
        }
    }

    void ApplyBounceLocal(Vector3 dir)
    {
        if (Time.time - lastBounceTime < bounceCooldown) return;
        lastBounceTime = Time.time;

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        rb.AddForce(dir * bounceForce, ForceMode.Impulse);
    }

    // ---LOGIC THUA ---

    void OnTriggerEnter(Collider other)
    {
        if (!pv.IsMine) return;
        if (RoomManager.instance.isWin) return;
        if (RoomGroundCheck.instance.isWin) return;

        if (other.CompareTag("Water"))
        {
            if (RoomManager.instance.isWin)
            {
                return;
            }

            if (losePanel == null)
            {
                losePanel = RoomManager.instance.losePanel;
            }
            if (inPlane)
            {
                inPlane = false;
                StartCoroutine(Lose());
            }
        }

        if (other.CompareTag("Item"))
        {
            other.gameObject.SetActive(false);
            eatItem = true;
            CallAllCheckItemFunction();
        }
    }

    IEnumerator Lose()
    {
        // Hiện bảng Lose
        if (losePanel != null)
        {
            losePanel.SetActive(true);
        }
        // Mở Cursor 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Chờ 3 giây
        yield return new WaitForSeconds(3f);

        // Rời phòng về Menu
        RoomManager.instance.LeaveRoom();
    }

    public void CallAllCheckItemFunction()
    {
        // 1. Tìm TẤT CẢ các nhân vật (PlayerMove) đang có mặt trong cảnh
        PlayerMove[] allPlayers = FindObjectsOfType<PlayerMove>();

        // 2. Bắt từng nhân vật một chạy lệnh RPC kiểm tra
        foreach (PlayerMove player in allPlayers)
        {
            player.GetComponent<PhotonView>().RPC("RPC_CheckItemAndLose", RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPC_CheckItemAndLose()
    {
        if (pv.IsMine)
        {
            // Nếu người này chưa ăn item (false) VÀ chưa bị rớt khỏi bàn
            if (eatItem == false && inPlane)
            {
                inPlane = false; // Đánh dấu là đã thua để không chạy lại lần nữa
                StartCoroutine(Lose());
            }
        }
    }
}
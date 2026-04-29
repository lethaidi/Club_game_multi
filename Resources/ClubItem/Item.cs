using UnityEngine;

public class Item : MonoBehaviour
{
    public float rotateSpeed = 50f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }
}

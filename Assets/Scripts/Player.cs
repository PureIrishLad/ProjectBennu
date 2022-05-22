using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float maxSpeed = 30f;
    public float minspeed = 15f;
    public float sensitivity = 5f;

    [HideInInspector]
    public Vector2Int chunkPos = Vector2Int.zero;

    private float speed = 5f;
    private World world;
    private Vector3 rotation = new Vector3();
    private int chunkSize;

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("World").GetComponent<World>();
        Cursor.lockState = CursorLockMode.Locked;
        
        chunkSize = world.chunkSize.x;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
            transform.position += transform.forward * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.S))
            transform.position -= transform.forward * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.D))
            transform.position += transform.right * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.A))
            transform.position -= transform.right * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Space))
            transform.position += Vector3.up * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftControl))
            transform.position -= Vector3.up * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftShift))
            speed = maxSpeed;
        else if (Input.GetKeyUp(KeyCode.LeftShift))
            speed = minspeed;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        rotation += new Vector3(-mouseY, mouseX, 0) * sensitivity;
        
        rotation.x = Mathf.Clamp(rotation.x, -90, 90);
        transform.rotation = Quaternion.Euler(rotation);

        chunkPos = new Vector2Int((int)transform.position.x / chunkSize, (int)transform.position.z / chunkSize);
    }
}
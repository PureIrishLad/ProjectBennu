using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float maxSpeed = 30f;
    public float minspeed = 15f;
    public float sensitivity = 5f;

    public GameObject cursor;
    public GameObject breakCursor;

    [HideInInspector]
    public Vector2Int chunkPos = Vector2Int.zero;
    [HideInInspector]
    public Vector2Int sectorPos = Vector2Int.zero;

    public Vector3 position;

    private float speed = 5f;
    private World world;
    private Vector3 rotation = new Vector3();
    private int chunkSize;

    public float step = 0.005f;
    public float range = 7f;

    public Vector3Int cursorPos = new Vector3Int();
    public Vector3Int breakPos = new Vector3Int();

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("World").GetComponent<World>();
        Cursor.lockState = CursorLockMode.Locked;

        chunkSize = World.chunkSize.x;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
            position += transform.forward * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.S))
            position -= transform.forward * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.D))
            position += transform.right * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.A))
            position -= transform.right * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Space))
            position += Vector3.up * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftControl))
            position -= Vector3.up * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftShift))
            speed = maxSpeed;
        else if (Input.GetKeyUp(KeyCode.LeftShift))
            speed = minspeed;

        if (Input.GetMouseButtonDown(1))
            PlaceBlock();
        if (Input.GetMouseButtonDown(0))
            BreakBlock();

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        rotation += new Vector3(-mouseY, mouseX, 0) * sensitivity;
        
        rotation.x = Mathf.Clamp(rotation.x, -90, 90);
        transform.rotation = Quaternion.Euler(rotation);

        chunkPos = new Vector2Int((int)position.x / chunkSize, (int)position.z / chunkSize);
        Raycast();
        cursor.transform.position = cursorPos - position + new Vector3(2000, -1, 2000);
        breakCursor.transform.position = breakPos - position + new Vector3(2000, -1, 2000);
    }

    private void Raycast()
    {
        float current = 0;
        byte b = 0;
        Vector3Int prevPos = new Vector3Int(-1, -1, -1);
        Vector3Int pos = new Vector3Int(-1, -1, -1);

        while (current < range && b == 0)
        {
            prevPos = pos;

            current += step;
            pos = new Vector3Int((int)(position.x + transform.forward.x * current), (int)(position.y + transform.forward.y * current + 1), (int)(position.z + transform.forward.z * current));

            b = world.GetBlock(pos);

        }

        cursorPos = b != 0 ? pos : new Vector3Int(-1, -1, -1);
        breakPos = b != 0 ? prevPos : new Vector3Int(-1, -1, -1);
    }

    private void PlaceBlock()
    {
        world.SetBlock(breakPos, 1);
    }

    private void BreakBlock()
    {
        world.SetBlock(cursorPos, 0);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkWorldPosUpdater : MonoBehaviour
{

    private Player player;
    private Vector3 worldPos;
    // Start is called before the first frame update
    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    private void Start()
    {
        worldPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = worldPos - player.position + new Vector3(2000, 0, 2000);
    }
}

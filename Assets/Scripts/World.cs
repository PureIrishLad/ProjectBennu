using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
    public static int worldSizeSectors = 229;
    public static int sectorSize = 64; // The size of each sector in chunks
    public static int worldSizeChunks = worldSizeSectors * sectorSize;
    public static int worldSizeBlocks = worldSizeChunks * chunkSize.x;
    public static Vector2Int chunkSize = new Vector2Int(16, 256); // The size of a chunk
    public int renderDistance; // The players render distance

    public float noiseScale; // The noise scale
    public GameObject chunkPrefab; // Chunk prefab
    public static GameObject chunkPrefabS;

    public float asteroidYBias = 32;

    // Chunks in the world
    //[HideInInspector]
    public List<Sector> sectors = new List<Sector>();
    private List<Sector> deleting = new List<Sector>();
    private List<Sector> loading = new List<Sector>();

    private Player player; // Reference to the player
    private Vector2Int playerPrevPos; // Players previous position
    private Vector2Int playerPos; // Players position

    // Contains all active chunks
    private List<Chunk> activeChunks = new List<Chunk>();
    [HideInInspector]

    private int targetFrameRate = 300;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        SimplexNoise.Noise.Seed = Random.Range(0, 1000000);
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        player.sectorPos = new Vector2Int(worldSizeSectors / 2, worldSizeSectors / 2);
        player.chunkPos = player.sectorPos * sectorSize;
        //playerPrevPos = player.chunkPos;
        player.transform.position = new Vector3(2000, 0, 2000);
        player.GetComponent<Player>().position = new Vector3(player.chunkPos.x * chunkSize.x, chunkSize.y / 2, player.chunkPos.y * chunkSize.x);

        int threads = System.Environment.ProcessorCount;
        chunkPrefabS = chunkPrefab;
        ThreadPool.SetMaxThreads(threads, threads);

        //InitialGenerateWorld();
    }

    private void Update()
    {
        for (int i = 0; i < deleting.Count; i++)
        {
            if (deleting[i].deleted)
            {
                foreach (Chunk chunk in deleting[i].chunks)
                    if (chunk != null)
                        Destroy(chunk.chunkObject);
                deleting.RemoveAt(i);
            }
        }

        for (int i = 0; i < loading.Count; i++)
        {
            if (loading[i].loaded)
            {
                sectors.Add(loading[i]);
                loading[i].LoadChunks();
                loading.RemoveAt(i);
            }
        }

        playerPos = player.chunkPos;

        // Generating chunks when player steps over chunk border
        if (playerPos != playerPrevPos)
            GenerateChunks();

        playerPrevPos = playerPos;
    }

    // Called when generating chunks
    private void GenerateChunks()
    {
        // All chunks that were previously active
        List<Chunk> previouslyActive = new List<Chunk>();
        previouslyActive.AddRange(activeChunks);
        activeChunks.Clear();

        // The list of chunks that need their meshes generated
        Queue<Chunk> toGenerate = new Queue<Chunk>();
        Queue<Chunk> generatingChunks = new Queue<Chunk>();

        for (int i = 0; i < sectors.Count; i++)
            sectors[i].numActive = 0;

        // Looping for each chunk in render distance
        for (int x = -renderDistance + 1; x < renderDistance; x++)
        {
            for (int z = -renderDistance + 1; z < renderDistance; z++)
            {
                Vector2Int chunkPos = new Vector2Int(x, z) + playerPos;
                Vector2Int sectorPos = GetSectorPos(chunkPos);

                // Making sure this chunk is in bounds and in the render distance
                if (Vector2.Distance(playerPos, chunkPos) > renderDistance || sectorPos.x < 0 || sectorPos.x >= worldSizeSectors || sectorPos.y < 0 || sectorPos.y >= worldSizeSectors) continue;

                // Getting this sector this chunk belongs to
                Sector thisSector = GetSector(sectorPos);

                if (thisSector == null)
                {
                    bool loading = ContainsSector(this.loading, sectorPos, out thisSector);
                    if (!SaveSector.FileExists(sectorPos))
                    {
                        thisSector = new Sector(sectorPos, sectorSize);
                        sectors.Add(thisSector);
                    }
                    else if (!loading)
                    {
                        thisSector = new Sector(sectorPos, sectorSize);
                        this.loading.Add(thisSector);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(thisSector.Load));
                        thisSector.toLoad.Add(chunkPos);
                        continue;
                    }
                    else
                    {
                        thisSector.toLoad.Add(chunkPos);
                        continue;
                    }

                    if (deleting.Contains(thisSector)) continue;
                }

                // If the list of previously active chunks contains this chunk, move it from previously active to active
                if (ContainsChunk(previouslyActive, chunkPos, out Chunk chunk))
                {
                    activeChunks.Add(chunk);
                    thisSector.numActive++;
                    previouslyActive.Remove(chunk);
                    continue;
                }

                // Retrieve the chunk from the sector
                chunk = GetChunkFromSector(thisSector, chunkPos);

                // If the chunk is loaded
                if (chunk != null)
                {
                    // Add the chunk to the list of active chunks
                    activeChunks.Add(chunk);
                    thisSector.numActive++;

                    if (chunk.chunkObject == null)
                    {
                        chunk.chunkObject = Instantiate(chunkPrefab);
                        generatingChunks.Enqueue(chunk);
                        chunk.InitChunk();
                        continue;
                    }

                    chunk.chunkObject.SetActive(true);
                    continue;
                }
                // If the chunk was not loaded, create a new chunk
                chunk = new Chunk();
                chunk.chunkObject = Instantiate(chunkPrefab);
                            
                activeChunks.Add(chunk);
                thisSector.numActive++;
                toGenerate.Enqueue(chunk);

                chunk.chunkPos = chunkPos;
                chunk.sectorPos = sectorPos;
                chunk.chunkSectorPos = thisSector.ChunkSectorPos(chunkPos);
                chunk.sector = thisSector;

                thisSector.chunks[chunk.chunkSectorPos.x, chunk.chunkSectorPos.y] = chunk;

                chunk.InitChunk();
                ThreadPool.QueueUserWorkItem(new WaitCallback(chunk.GenerateChunk));
            } 
        }

        // All chunks left in previously active are disabled
        for (int i = 0; i < previouslyActive.Count; i++)
            previouslyActive[i].chunkObject.SetActive(false);

        for (int i = 0; i < sectors.Count; i++)
        {
            Sector s = sectors[i];
            if (s.numActive == 0)
            {
                sectors.Remove(s);
                deleting.Add(s);
                ThreadPool.QueueUserWorkItem(new WaitCallback(s.Unload));
            }
        }

        // Generating the meshes for all chunks
        while (toGenerate.Count > 0)
            if (toGenerate.Peek().isGenerated)
                generatingChunks.Enqueue(toGenerate.Dequeue());

        while (generatingChunks.Count > 0)
        {
            Chunk chunk = generatingChunks.Dequeue();
            StartCoroutine(chunk.GenerateMesh());
            //StartCoroutine(chunk.UpdateSurroundingChunks());
        }
    }

    public byte GetBlock(Vector3Int pos)
    {
        if (pos.x < 0 || pos.z < 0 || pos.y < 0 || pos.x == worldSizeBlocks || pos.z == worldSizeBlocks || pos.y == chunkSize.y) return 0;

        Sector sector = GetSector(pos);
        Chunk chunk = sector == null ? null : sector.GetChunk(pos);

        return chunk == null ? (byte)0 : chunk.GetBlock(pos);
    }
    public void SetBlock(Vector3Int pos, byte blockID)
    {
        if (pos.x < 0 || pos.z < 0 || pos.y < 0 || pos.x == worldSizeBlocks || pos.z == worldSizeBlocks || pos.y == chunkSize.y) return;

        Sector sector = GetSector(pos);
        Chunk chunk = sector == null ? null : sector.GetChunk(pos);

        if (chunk != null) chunk.SetBlock(pos, blockID);
    }
    private Sector GetSector(Vector3Int pos)
    {
        Vector2Int sectorPos = new Vector2Int(pos.x / chunkSize.x / sectorSize, pos.z / chunkSize.x / sectorSize);

        for (int i = 0; i < sectors.Count; i++)
            if (sectors[i].sectorPos == sectorPos)
                return sectors[i];

        return null;
    }

    // Returns true and the chunk object if the chunk with the coordinates exists in the list
    private static bool ContainsChunk(List<Chunk> chunks, Vector2Int chunkPos, out Chunk outChunk)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            if (chunks[i].chunkPos == chunkPos)
            {
                outChunk = chunks[i];
                return true;
            }
        }
        outChunk = null;
        return false;
    }
    public static Chunk GetChunkFromSector(Sector sector, Vector2Int chunkPos)
    {
        Vector2Int csp = sector.ChunkSectorPos(chunkPos);
        return sector.chunks[csp.x, csp.y];
    }
    public static Vector2Int GetSectorPos(Vector2Int chunkPos)
    {
        return chunkPos / sectorSize;
    }
    public Sector GetSector(Vector2Int sectorPos)
    {
        for (int i = 0; i < sectors.Count; i++)
        {
            if (sectors[i].sectorPos == sectorPos)
            {
                return sectors[i];
            }
        }
        return null;
    }
    public static bool ContainsSector(List<Sector> sectors, Vector2Int sectorPos, out Sector sector)
    {
        sector = null;
        for (int i = 0; i < sectors.Count; i++)
        {
            if (sectors[i].sectorPos == sectorPos)
            {
                sector = sectors[i];
                return true;
            }
        }
        return false;
    }

    public Sector GetSectorFromChunkPos(Vector2Int chunkPos)
    {
        Vector2Int sectorPos = chunkPos / sectorSize;

        for (int i = 0; i < sectors.Count; i++)
            if (sectors[i].sectorPos == sectorPos)
                return sectors[i];
        return null;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
    public static int worldSizeSectors = 12;
    public static int sectorSize = 64; // The size of each sector in chunks
    public static int worldSizeChunks = worldSizeSectors * sectorSize;
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
    public List<Chunk> generatingChunks = new List<Chunk>();

    private void Awake()
    {
        SimplexNoise.Noise.Seed = Random.Range(0, 1000000);
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        player.sectorPos = new Vector2Int(worldSizeSectors / 2, worldSizeSectors / 2);
        player.chunkPos = player.sectorPos * sectorSize;
        playerPrevPos = player.chunkPos;
        player.transform.position = new Vector3(player.chunkPos.x * chunkSize.x, chunkSize.y / 2, player.chunkPos.y * chunkSize.x);

        int threads = System.Environment.ProcessorCount;
        chunkPrefabS = chunkPrefab;
        ThreadPool.SetMaxThreads(threads, threads);

        InitialGenerateWorld();
    }

    private void Update()
    {
        for (int i = 0; i < deleting.Count; i++)
        {
            if (deleting[i].deleted)
                deleting.RemoveAt(i);
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

    // The initial call when generating the world
    private void InitialGenerateWorld()
    {
        playerPos = player.chunkPos;

        // Generating chunk data for all chunks in players render distance
        for (int x = -renderDistance + 1; x < renderDistance; x++)
        {
            for (int z = -renderDistance + 1; z < renderDistance; z++)
            {
                Vector2Int chunkPos = new Vector2Int(x, z) + playerPos;
                Vector2Int sectorPos = GetSectorPos(chunkPos);

                if (sectorPos.x < 0 || sectorPos.x >= worldSizeSectors || sectorPos.y < 0 || sectorPos.y >= worldSizeSectors || Vector2Int.Distance(chunkPos, playerPos) > renderDistance) continue;

                Sector thisSector = GetSector(sectorPos);

                if (thisSector == null)
                {
                    if (!SaveSector.FileExists(sectorPos))
                    {
                        thisSector = new Sector(sectorPos, sectorSize);
                        sectors.Add(thisSector);
                    }
                    else if (!ContainsSector(loading, sectorPos))
                    {
                        thisSector = new Sector(sectorPos, sectorSize);
                        thisSector.toLoad.Add(chunkPos);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(thisSector.Load));
                        loading.Add(thisSector);
                        continue;
                    }
                }

                if (ContainsSector(loading, sectorPos, out Sector s))
                {
                    s.toLoad.Add(chunkPos);
                    continue;
                }

                Chunk chunk = GetChunkFromSector(thisSector, chunkPos);
                GameObject go = GameObject.Instantiate(chunkPrefab);

                chunk = new Chunk();
                chunk.chunkObject = go;

                activeChunks.Add(chunk);
                thisSector.activeChunks.Add(chunk);

                chunk.chunkPos = chunkPos;
                chunk.sectorPos = sectorPos;
                chunk.chunkSectorPos = thisSector.ChunkSectorPos(chunkPos);
                chunk.sector = thisSector;

                thisSector.chunks[chunk.chunkSectorPos.x, chunk.chunkSectorPos.y] = chunk;

                chunk.InitChunk();
                StartCoroutine(chunk.GenerateChunk());
            }
        }

        generatingChunks.Clear();

        // When all chunks have finished generating, we want to generate their meshes
        while (generatingChunks.Count < activeChunks.Count)
        {
            foreach (Chunk chunk in activeChunks)
            {
                if (!generatingChunks.Contains(chunk) && chunk.isGenerated)
                {
                    generatingChunks.Add(chunk);
                }
            }
        }

        foreach (Chunk chunk in generatingChunks)
            //StartCoroutine(chunk.GenerateMesh());
            chunk.GenerateMesh();

        generatingChunks.Clear();
    }

    // Called when generating chunks
    private void GenerateChunks()
    {
        // All chunks that were previously active
        List<Chunk> previouslyActive = new List<Chunk>();
        previouslyActive.AddRange(activeChunks);
        activeChunks.Clear();

        // The list of chunks that need their meshes generated
        List<Chunk> toGenerate = new List<Chunk>();
        
        foreach (Sector s in sectors)
            s.activeChunks.Clear();

        // Looping for each chunk in render distance
        for (int x = -renderDistance + 1; x < renderDistance; x++)
        {
            for (int z = -renderDistance + 1; z < renderDistance; z++)
            {
                Vector2Int chunkPos = new Vector2Int(x, z) + playerPos;
                Vector2Int sectorPos = GetSectorPos(chunkPos);

                // Making sure this chunk is in bounds and in the render distance
                if (sectorPos.x < 0 || sectorPos.x >= worldSizeSectors || sectorPos.y < 0 || sectorPos.y >= worldSizeSectors || Vector2.Distance(playerPos, chunkPos) > renderDistance) continue;

                // Getting this sector this chunk belongs to
                Sector thisSector = GetSector(sectorPos);

                if (thisSector == null)
                {
                    if (!SaveSector.FileExists(sectorPos))
                    {
                        thisSector = new Sector(sectorPos, sectorSize);
                        sectors.Add(thisSector);
                    }
                    else if (!ContainsSector(loading, sectorPos))
                    {
                        thisSector = new Sector(sectorPos, sectorSize);
                        loading.Add(thisSector);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(thisSector.Load));
                        thisSector.toLoad.Add(chunkPos);
                        continue;
                    }
                }

                if (ContainsSector(loading, sectorPos, out Sector s))
                {
                    s.toLoad.Add(chunkPos);
                    continue;
                }

                if (deleting.Contains(thisSector)) continue;

                // If the list of previously active chunks contains this chunk, move it from previously active to active
                if (ContainsChunk(previouslyActive, chunkPos, out Chunk chunk))
                {
                    activeChunks.Add(chunk);
                    thisSector.activeChunks.Add(chunk);
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
                    thisSector.activeChunks.Add(chunk);

                    if (chunk.chunkObject == null)
                    {
                        chunk.chunkObject = Instantiate(chunkPrefab);
                        generatingChunks.Add(chunk);
                        chunk.InitChunk();
                    }

                    chunk.chunkObject.SetActive(true);
                }
                // If the chunk was not loaded, create a new chunk
                else
                {
                    chunk = new Chunk();
                    chunk.chunkObject = Instantiate(chunkPrefab);
                            
                    activeChunks.Add(chunk);
                    thisSector.activeChunks.Add(chunk);
                    toGenerate.Add(chunk);

                    chunk.chunkPos = chunkPos;
                    chunk.sectorPos = sectorPos;
                    chunk.chunkSectorPos = thisSector.ChunkSectorPos(chunkPos);
                    chunk.sector = thisSector;

                    thisSector.chunks[chunk.chunkSectorPos.x, chunk.chunkSectorPos.y] = chunk;

                    chunk.InitChunk();
                    StartCoroutine(chunk.GenerateChunk());
                }
            } 
        }

        // All chunks left in previously active are disabled
        foreach (Chunk chunk in previouslyActive)
            chunk.chunkObject.SetActive(false);

        for (int i = 0; i < sectors.Count; i++)
        {
            Sector s = sectors[i];
            if (s.activeChunks.Count == 0)
            {
                sectors.Remove(s);
                deleting.Add(s);
                ThreadPool.QueueUserWorkItem(new WaitCallback(s.Unload));
            }
        }

        // Generating the meshes for all chunks
        while (toGenerate.Count > 0)
        {
            for (int i = 0; i < toGenerate.Count; i++)
            {
                if (toGenerate[i].isGenerated)
                {
                    generatingChunks.Add(toGenerate[i]);
                    toGenerate.RemoveAt(i);
                }
            }
        }

        foreach (Chunk chunk in generatingChunks)
        {
            chunk.meshGenerated = true;
            chunk.GenerateMesh();
            chunk.UpdateSurroundingChunks();
        }

        generatingChunks.Clear();
    }

    // Returns true and the chunk object if the chunk with the coordinates exists in the list
    private static bool ContainsChunk(List<Chunk> chunks, Vector2Int chunkPos, out Chunk outChunk)
    {
        foreach (Chunk chunk in chunks)
            if (chunk.chunkPos == chunkPos)
            {
                outChunk = chunk;
                return true;
            }
        outChunk = null;
        return false;
    }

    public Chunk GetChunkFromSector(Sector sector, Vector2Int chunkPos)
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
        foreach (Sector s in sectors)
            if (s.sectorPos == sectorPos)
                return s;
        return null;
    }

    public bool ContainsSector(List<Sector> sectors, Vector2Int sectorPos)
    {
        foreach (Sector s in sectors)
            if (s.sectorPos == sectorPos)
                return true;
        return false;
    }

    public bool ContainsSector(List<Sector> sectors, Vector2Int sectorPos, out Sector sector)
    {
        sector = null;
        foreach (Sector s in sectors)
            if (s.sectorPos == sectorPos)
            {
                sector = s;
                return true;
            }
        return false;
    }
}
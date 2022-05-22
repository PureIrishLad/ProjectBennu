using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
    public static int worldSizeSectors = 128;
    public static int sectorSize = 64; // The size of each sector in chunks
    public static int worldSizeChunks = worldSizeSectors * sectorSize;
    public static Vector2Int chunkSize = new Vector2Int(16, 256); // The size of a chunk
    public int renderDistance; // The players render distance

    public float noiseScale; // The noise scale
    public GameObject chunkPrefab; // Chunk prefab
    public static GameObject chunkPrefabS;

    public float asteroidYBias = 32;

    // Chunks in the world
    [HideInInspector]
    public List<Sector> sectors = new List<Sector>();
    //public Chunk[,] chunks = new Chunk[,] { };

    private Player player; // Reference to the player
    private Vector2Int playerPrevPos; // Players previous position
    private Vector2Int playerPos; // Players position
    private Vector2Int playerSectorPos;

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
        player.transform.position = new Vector3(player.chunkPos.x * chunkSize.x, chunkSize.y / 2, player.chunkPos.y * chunkSize.x);

        playerSectorPos = player.sectorPos;

        Sector sector = new Sector(playerSectorPos, sectorSize);
        sectors.Add(sector);

        int threads = System.Environment.ProcessorCount;
        chunkPrefabS = chunkPrefab;
        ThreadPool.SetMaxThreads(threads, threads);

        InitialGenerateWorld();
    }

    private void Update()
    {
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

                if (sectorPos.x < 0 || sectorPos.x >= worldSizeSectors || sectorPos.y < 0 || sectorPos.y >= worldSizeSectors) continue;

                Sector thisSector = null;
                foreach (Sector sector in sectors)
                {
                    if (sector.sectorPos == sectorPos)
                    {
                        thisSector = sector;
                    }
                }

                if (thisSector == null)
                {
                    thisSector = new Sector(sectorPos, sectorSize);
                    sectors.Add(thisSector);
                }

                if (Vector2Int.Distance(chunkPos, playerPos) < renderDistance)
                {
                    Chunk chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
                    chunk.chunkPos = chunkPos;
                    chunk.sectorPos = sectorPos;
                    chunk.sector = thisSector;

                    chunk.chunkSectorPos = thisSector.ChunkSectorPos(chunkPos);

                    thisSector.chunks[chunk.chunkSectorPos.x, chunk.chunkSectorPos.y] = chunk;
                    activeChunks.Add(chunk);
                    thisSector.activeChunks.Add(chunk);
                    StartCoroutine(chunk.GenerateChunk());
                }
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
            StartCoroutine(chunk.GenerateMesh());

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

        for (int x = -renderDistance + 1; x < renderDistance; x++)
        {
            for (int z = -renderDistance + 1; z < renderDistance; z++)
            {
                Vector2Int chunkPos = new Vector2Int(x, z) + playerPos;
                Vector2Int sectorPos = GetSectorPos(chunkPos);

                if (sectorPos.x < 0 || sectorPos.x >= worldSizeSectors || sectorPos.y < 0 || sectorPos.y >= worldSizeSectors) continue;

                Sector thisSector = null;
                foreach (Sector sector in sectors)
                {
                    if (sector.sectorPos == sectorPos)
                    {
                        thisSector = sector;
                    }
                }

                if (thisSector == null)
                {
                    thisSector = SaveSector.Load(sectorPos);
                    if (thisSector == null)
                        thisSector = new Sector(sectorPos, sectorSize);

                    sectors.Add(thisSector);
                }
                thisSector.activeChunks.Clear();
                if (Vector2Int.Distance(chunkPos, playerPos) < renderDistance)
                {
                    // If this chunk was not previously active then...
                    if (!ContainsChunk(previouslyActive, chunkPos, out Chunk chunk))
                    {
                        // If it has been generated before, just set it to active again
                        if (ContainsChunk(sectorPos, chunkPos, out chunk))
                        {
                            activeChunks.Add(chunk);
                            chunk.gameObject.SetActive(true);
                        }
                        // Otherwise generate a new chunk
                        else
                        {
                            chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
                            chunk.chunkPos = chunkPos;
                            chunk.sectorPos = sectorPos;
                            chunk.sector = thisSector;

                            chunk.chunkSectorPos = thisSector.ChunkSectorPos(chunkPos);

                            thisSector.chunks[chunk.chunkSectorPos.x, chunk.chunkSectorPos.y] = chunk;
                            activeChunks.Add(chunk);
                            thisSector.activeChunks.Add(chunk);
                            toGenerate.Add(chunk);
                            StartCoroutine(chunk.GenerateChunk());
                        }
                    }
                    else
                    {
                        activeChunks.Add(chunk);
                        thisSector.activeChunks.Add(chunk);
                        previouslyActive.Remove(chunk);
                    }
                }

                if (thisSector.activeChunks.Count == 0)
                {
                    sectors.Remove(thisSector);
                    thisSector.Unload();
                }
            }
        }

        // All chunks left in previously active are disabled
        foreach (Chunk chunk in previouslyActive)
            chunk.gameObject.SetActive(false);

        // Generating the meshes for all chunks
        while (generatingChunks.Count < toGenerate.Count)
        {
            foreach (Chunk chunk in toGenerate)
            {
                if (!generatingChunks.Contains(chunk) && chunk.isGenerated)
                {
                    generatingChunks.Add(chunk);
                }
            }
        }

        foreach (Chunk chunk in generatingChunks)
        {
            StartCoroutine(chunk.GenerateMesh());
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

    public bool ContainsChunk(Vector2Int sectorPos, Vector2Int chunkPos, out Chunk outChunk)
    {
        foreach (Sector sector in sectors)
        {
            if (sector.sectorPos != sectorPos) continue;
            foreach (Chunk chunk in sector.chunks)
                if (chunk && chunk.chunkPos == chunkPos)
                {
                    outChunk = chunk;
                    return true;
                }
        }

        outChunk = null;
        return false;
    }

    public Chunk GetChunk(Vector2Int sectorPos, Vector2Int chunkSectorPos)
    {
        foreach(Sector sector in sectors)
            if (sector.sectorPos == sectorPos)
                return sector.chunks[chunkSectorPos.x, chunkSectorPos.y];
        return null;
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
}
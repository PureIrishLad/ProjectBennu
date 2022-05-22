using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
    public int worldSize = 12; // The size of the world in chunks
    public Vector2Int chunkSize = new Vector2Int(16, 64); // The size of a chunk
    public int renderDistance; // The players render distance

    public float noiseScale; // The noise scale
    public GameObject chunkPrefab; // Chunk prefab

    public float asteroidYBias = 32;

    // Chunks in the world
    [HideInInspector]
    public Chunk[,] chunks = new Chunk[,] { };

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

        chunks = new Chunk[worldSize, worldSize];

        player.chunkPos = new Vector2Int(worldSize / 2, worldSize / 2);
        player.transform.position = new Vector3(player.chunkPos.x * chunkSize.x, chunkSize.y / 2, player.chunkPos.y * chunkSize.x);

        int threads = System.Environment.ProcessorCount;

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

                if (chunkPos.x < 0 || chunkPos.x >= worldSize || chunkPos.y < 0 || chunkPos.y >= worldSize) continue;

                if (Vector2Int.Distance(chunkPos, playerPos) < renderDistance)
                {
                    Chunk chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
                    chunk.chunkPos = chunkPos;
                    chunks[chunkPos.x, chunkPos.y] = chunk;
                    activeChunks.Add(chunk);
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

                if (chunkPos.x < 0 || chunkPos.x >= worldSize || chunkPos.y < 0 || chunkPos.y >= worldSize) continue;

                if (Vector2Int.Distance(chunkPos, playerPos) < renderDistance)
                {
                    // If this chunk was not previously active then...
                    if (!ContainsChunk(previouslyActive, chunkPos, out Chunk chunk))
                    {
                        // If it has been generated before, just set it to active again
                        if (ContainsChunk(chunkPos, out chunk))
                        {
                            activeChunks.Add(chunk);
                            chunk.gameObject.SetActive(true);
                        }
                        // Otherwise generate a new chunk
                        else
                        {
                            chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
                            chunk.chunkPos = chunkPos;
                            chunks[chunkPos.x, chunkPos.y] = chunk;
                            activeChunks.Add(chunk);
                            toGenerate.Add(chunk);
                            StartCoroutine(chunk.GenerateChunk());
                        }
                    }
                    else
                    {
                        activeChunks.Add(chunk);
                        previouslyActive.Remove(chunk);
                    }
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

    public bool ContainsChunk(Vector2Int chunkPos, out Chunk outChunk)
    {
        foreach(Chunk chunk in chunks)
            if (chunk && chunk.chunkPos == chunkPos)
            {
                outChunk = chunk;
                return true;
            }

        outChunk = null;
        return false;
    }

    public Chunk GetChunk(Vector2Int chunkPos)
    {
        return chunks[chunkPos.x, chunkPos.y];
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sector
{
    public Chunk[,] chunks;
    public Vector2Int sectorPos;
    private int sectorSize;

    public List<Chunk> activeChunks;
    public List<Vector2Int> toLoad;
    public bool deleted = false;
    public bool loaded = false;

    public Sector(Vector2Int sectorPos, int sectorSize)
    {
        chunks = new Chunk[sectorSize, sectorSize];
        this.sectorPos = sectorPos;
        this.sectorSize = sectorSize;
        activeChunks = new List<Chunk>();
        toLoad = new List<Vector2Int>();
    }

    public Vector2Int ChunkSectorPos(Vector2Int chunkPos)
    {
        return new Vector2Int(Utility.Modulo(chunkPos.x, sectorSize), Utility.Modulo(chunkPos.y, sectorSize));
    }

    public void LoadChunks()
    {
        foreach (Vector2Int chunkPos in toLoad)
        {
            GameObject go = GameObject.Instantiate(World.chunkPrefabS);

            Vector2Int csp = ChunkSectorPos(chunkPos);
            Chunk chunk = chunks[csp.x, csp.y];
            
            chunk.chunkObject = go;
            chunk.InitChunk();
            chunk.GenerateMesh();
        }

        toLoad.Clear();
    }

    public void Load(object obj)
    {
        Sector s = SaveSector.Load(sectorPos);
        chunks = s.chunks;
        loaded = true;
    }

    public void Unload(object obj)
    {
        SaveSector.Save(this);

        foreach (Chunk chunk in chunks)
            if (chunk != null)
                Object.Destroy(chunk.chunkObject);

        chunks = new Chunk[0, 0];

        deleted = true;
    }
}

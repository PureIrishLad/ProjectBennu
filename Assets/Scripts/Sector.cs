using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sector
{
    public Chunk[,] chunks;
    public Vector2Int sectorPos;
    private int sectorSize;

    public int numActive;
    public List<Vector2Int> toLoad;
    public bool deleted = false;
    public bool loaded = false;

    public Sector(Vector2Int sectorPos, int sectorSize)
    {
        chunks = new Chunk[sectorSize, sectorSize];
        this.sectorPos = sectorPos;
        this.sectorSize = sectorSize;
        numActive = 0;
        toLoad = new List<Vector2Int>();
    }

    public Vector2Int ChunkSectorPos(Vector2Int chunkPos)
    {
        return new Vector2Int(Utility.Modulo(chunkPos.x, sectorSize), Utility.Modulo(chunkPos.y, sectorSize));
    }

    public Chunk GetChunk(Vector3Int pos)
    {
        Vector2Int posV2 = new Vector2Int(pos.x, pos.z);
        Vector2Int chunkPos = new Vector2Int(posV2.x / World.chunkSize.x, posV2.y / World.chunkSize.x);

        if (chunkPos.x == World.worldSizeChunks || chunkPos.y == World.worldSizeChunks) return null;

        Vector2Int chunkSectorPos = new Vector2Int(chunkPos.x % World.sectorSize, chunkPos.y % World.sectorSize);

        return chunks[chunkSectorPos.x, chunkSectorPos.y];
    }
    public Chunk GetChunk(Vector2Int chunkPos)
    {
        if (chunkPos.x < 0 || chunkPos.y < 0) return null;
        Vector2Int csp = new Vector2Int(chunkPos.x % sectorSize, chunkPos.y % sectorSize);
        return chunks[csp.x, csp.y];
    }

    public void LoadChunks()
    {
        foreach (Vector2Int chunkPos in toLoad)
        {
            GameObject go = GameObject.Instantiate(World.chunkPrefabS);

            Vector2Int csp = ChunkSectorPos(chunkPos);
            Chunk chunk = chunks[csp.x, csp.y];

            if (chunk == null) continue;

            chunk.chunkObject = go;
            chunk.InitChunk();
            chunk.GenerateMeshNC();
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

        deleted = true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector
{
    public Chunk[,] chunks;
    public Vector2Int sectorPos;
    private int sectorSize;

    public List<Chunk> activeChunks;

    public Sector(Vector2Int sectorPos, int sectorSize)
    {
        chunks = new Chunk[sectorSize, sectorSize];
        this.sectorPos = sectorPos;
        this.sectorSize = sectorSize;
        activeChunks = new List<Chunk>();
    }

    public Vector2Int ChunkSectorPos(Vector2Int chunkPos)
    {
        return new Vector2Int(Utility.Modulo(chunkPos.x, sectorSize), Utility.Modulo(chunkPos.y, sectorSize));
    }

    public void Unload()
    {
        SaveSector.Save(this);

        foreach(var chunk in activeChunks)
            Object.Destroy(chunk.gameObject);
    }
}

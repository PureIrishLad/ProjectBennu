using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SectorData 
{
    public ChunkData[,] chunks;
    public int sectorPosX;
    public int sectorPosY;

    public SectorData(Chunk[,] chunks, Vector2Int sectorPos)
    {
        this.chunks = GetChunkData(chunks);
        this.sectorPosX = sectorPos.x;
        this.sectorPosY = sectorPos.y;
    }

    public static ChunkData[,] GetChunkData(Chunk[,] chunks)
    {
        ChunkData[,] cd = new ChunkData[World.sectorSize, World.sectorSize];

        for (int x = 0; x < World.sectorSize; x++)
        {
            for (int y = 0; y < World.sectorSize; y++)
            {
                if (chunks[x, y] == null) continue;
                cd[x, y] = new ChunkData(chunks[x, y].data, chunks[x, y].chunkPos);
            }
        }

        return cd;
    }

    public static Chunk[,] GetChunkFromData(ChunkData[,] cs, Sector sector)
    {
        Chunk[,] chunks = new Chunk[World.sectorSize, World.sectorSize];
        for (int x = 0; x < World.sectorSize; x++)
        {
            for (int y = 0; y < World.sectorSize; y++)
            {
                if (cs[x, y] == null) continue;
                Chunk c = Object.Instantiate(World.chunkPrefabS).GetComponent<Chunk>();
                c.generate = false;
                c.chunkPos = new Vector2Int(cs[x, y].chunkPosX, cs[x, y].chunkPosY);
                c.sectorPos = sector.sectorPos;
                c.sector = sector;

                chunks[x, y] = c;
            }
        }

        return chunks;
    }
}

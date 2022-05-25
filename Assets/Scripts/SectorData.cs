using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SectorData 
{
    [SerializeField] public string[] chunks;
    [SerializeField] public int sectorPosX;
    [SerializeField] public int sectorPosY;

    public SectorData(Chunk[,] chunks, Vector2Int sectorPos)
    {
        GetChunkData(chunks);
        sectorPosX = sectorPos.x;
        sectorPosY = sectorPos.y;
    }

    public void GetChunkData(Chunk[,] chunksRaw)
    {
        int width = World.sectorSize;
        
        chunks = new string[width * width];

        for (int x = 0; x < width; x++)
        {
            int i = x * width;
            for (int y = 0; y < width; y++)
            {
                Chunk raw = chunksRaw[x, y];
                if (raw == null) continue;
                chunks[i + y] = ChunkData.Compress(raw.data);
            }
        }
    }

    public void Decompress(Sector s)
    {
        int width = World.sectorSize;
        int height = World.sectorSize;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                string cdc = this.chunks[x * height + y];
                if (cdc == null) continue;
                Chunk c = new Chunk();

                c.data = ChunkData.Decompress(cdc);

                c.chunkPos = new Vector2Int(x + s.sectorPos.x * World.sectorSize, y + s.sectorPos.y * World.sectorSize);
                c.sectorPos = new Vector2Int(sectorPosX, sectorPosY);
                c.chunkSectorPos = s.ChunkSectorPos(c.chunkPos);
                c.isGenerated = true;
                c.sector = s;
                c.meshGenerated = false;

                s.chunks[x, y] = c;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplexNoise;
using System.Threading;
using System;

public class Chunk
{
    [HideInInspector]
    public Mesh mesh; // Chunks mesh
    [HideInInspector]
    public Vector2Int chunkPos; // Chunks position
    public Vector2Int sectorPos;
    public Vector2Int chunkSectorPos;
    public Sector sector;

    public GameObject chunkObject;

    private World world; // Reference to world
    public byte[,,] data; // Chunk data
    private MeshFilter meshFilter; // Reference to mesh filter
    private int worldSize; // The size of the world in chunks
    private int width; // The width of a chunk
    private int height; // The height of a chunk
    private float scale; // Noise scale
    private Vector3Int offset; // This chunks offset in blocks
    private Vector3Int[] localVertsPos = new Vector3Int[]
    {
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(1,  0, 0),
            new Vector3Int(0,  0, 0),

            new Vector3Int(1, -1, 1),
            new Vector3Int(0, -1, 1),
            new Vector3Int(0,  0, 1),
            new Vector3Int(1,  0, 1),
    }; // The local location of vertices for each block
    private Vector2Int[] uvsA = new Vector2Int[4]
    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
    }; // Uvs for each block

    private List<Vector3> verts = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> indices = new List<int>();

    private float yBias;

    public bool meshGenerated = true;

    [HideInInspector]
    public bool isGenerated; // True if the chunk data has been generated

    public void InitChunk()
    {
        world = GameObject.FindGameObjectWithTag("World").GetComponent<World>();
        width = World.chunkSize.x;
        height = World.chunkSize.y;
        worldSize = World.worldSizeChunks;
        offset = new Vector3Int(chunkPos.x, 0, chunkPos.y) * width;
        scale = world.noiseScale;
        mesh = new Mesh();
        meshFilter = chunkObject.GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        chunkObject.transform.position = offset;
        yBias = world.asteroidYBias;
    }

    // Generates the chunk data
    public void GenerateChunk(object obj)
    {
        data = new byte[width, height, width];

        GenerateChunkData(obj);

        isGenerated = true;
    }

    // Generates chunk mesh
    public void GenerateMesh()
    {
        mesh.Clear();

        GenerateChunkMesh();

        mesh.vertices = verts.ToArray();
        mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        mesh.SetUVs(0, uvs.ToArray());

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        //yield return null;
    }

    // Tests the given block position to see which faces can be drawn
    private byte TestPos(Vector3Int pos)
    {
        // We use a flag to represent which faces need to be drawn
        byte flags = 0x00;

        // Coordinates of the 6 adjacent blocks
        Vector3Int up = pos + Vector3Int.up;
        Vector3Int down = pos + Vector3Int.down;
        Vector3Int left = pos + Vector3Int.left;
        Vector3Int right = pos + Vector3Int.right;
        Vector3Int forward = pos + Vector3Int.forward;
        Vector3Int back = pos + Vector3Int.back;

        Vector2Int lSectorPos = World.GetSectorPos(new Vector2Int(chunkPos.x - 1, chunkPos.y));
        Vector2Int rSectorPos = World.GetSectorPos(new Vector2Int(chunkPos.x + 1, chunkPos.y));
        Vector2Int fSectorPos = World.GetSectorPos(new Vector2Int(chunkPos.x, chunkPos.y + 1));
        Vector2Int bSectorPos = World.GetSectorPos(new Vector2Int(chunkPos.x, chunkPos.y - 1));

        Sector lSector = world.GetSector(lSectorPos);
        Sector rSector = world.GetSector(rSectorPos);
        Sector fSector = world.GetSector(fSectorPos);
        Sector bSector = world.GetSector(bSectorPos);

        Vector2Int lChunkPos = lSector != null ? lSector.ChunkSectorPos(new Vector2Int(chunkPos.x - 1, chunkPos.y)) : Vector2Int.zero;
        Vector2Int rChunkPos = rSector != null ? rSector.ChunkSectorPos(new Vector2Int(chunkPos.x + 1, chunkPos.y)) : Vector2Int.zero;
        Vector2Int fChunkPos = fSector != null ? fSector.ChunkSectorPos(new Vector2Int(chunkPos.x, chunkPos.y + 1)) : Vector2Int.zero;
        Vector2Int bChunkPos = bSector != null ? bSector.ChunkSectorPos(new Vector2Int(chunkPos.x, chunkPos.y - 1)) : Vector2Int.zero;

        Chunk lChunk = lSector != null ? lSector.chunks[lChunkPos.x, lChunkPos.y] : null;
        Chunk rChunk = rSector != null ? rSector.chunks[rChunkPos.x, rChunkPos.y] : null;
        Chunk fChunk = fSector != null ? fSector.chunks[fChunkPos.x, fChunkPos.y] : null;
        Chunk bChunk = bSector != null ? bSector.chunks[bChunkPos.x, bChunkPos.y] : null;

        byte bBlock = back.z >= 0 && data[back.x, back.y, back.z] == 0 || back.z < 0 && bChunk != null && bChunk.TestChunk(back) ? (byte)0x01 : (byte)0;
        byte fBlock = forward.z < width && data[forward.x, forward.y, forward.z] == 0|| forward.z == width && fChunk != null && fChunk.TestChunk(forward) ? (byte)0x02 : (byte)0;
        byte lBlock = left.x >= 0 && data[left.x, left.y, left.z] == 0 || left.x < 0 && lChunk != null && lChunk.TestChunk(left) ? (byte)0x04 : (byte)0;
        byte rBlock = right.x < width && data[right.x, right.y, right.z] == 0 || right.x == width && rChunk != null && rChunk.TestChunk(right) ? (byte)0x08 : (byte)0;
        byte uBlock = up.y < height && data[up.x, up.y, up.z] == 0 || up.y == height ? (byte)0x10 : (byte)0;
        byte dBlock = down.y >= 0 && data[down.x, down.y, down.z] == 0 || down.y < 0 ? (byte)0x20 : (byte)0;

        flags |= (byte)(bBlock | fBlock | lBlock | rBlock | uBlock | dBlock);

        return flags;
    }
    public bool TestChunk(Vector3Int pos)
    {
        Vector3Int localPos = new Vector3Int(Utility.Modulo(pos.x, World.chunkSize.x), pos.y, Utility.Modulo(pos.z, World.chunkSize.x));

        return data[localPos.x, localPos.y, localPos.z] == 0;
    }

    public void UpdateSurroundingChunks()
    {
        for (int x = -1; x < 2; x++)
        {
            for (int z = -1; z < 2; z++)
            {
                if ((x == -1 || x == 1) && (z == -1 || z == 1) || x == z) continue;
                Vector2Int chunkPos = new Vector2Int(x, z) + this.chunkPos;
                Vector2Int sectorPos = World.GetSectorPos(chunkPos);
                if (chunkPos.x == -1 || chunkPos.y == -1 || chunkPos.x == worldSize || chunkPos.y == worldSize) continue;

                Sector sector = world.GetSector(sectorPos);
                if (sector == null) continue;

                Vector2Int chunkSectorPos = sector.ChunkSectorPos(chunkPos);
                Chunk chunk = sector.chunks[chunkSectorPos.x, chunkSectorPos.y];

                if (world.generatingChunks.Contains(chunk)) continue;

                if (chunk != null && chunk.chunkObject != null)
                    chunk.GenerateMesh();
            }
        }
    }

    private void GenerateChunkData(object thread)
    {
        float bval = 241f / 255f;
        float bval2 = 14f / 255f;
        for (int y = 0; y < height; y++)
        {
            float bias = 1 - (Mathf.Abs(yBias - y) / yBias);
            float bias2 = bval + bval2 * bias;
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    float value = Noise.CalcPixel3D(x + offset.x, y, z + offset.z, scale);

                    value *= bias2;

                    data[x, y, z] = value >= 241 ? (byte)1 : (byte)0;
                }

            }
        }
    }

    private void GenerateChunkMesh()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    // We ignore this block if it is void
                    if (data[x, y, z] == 0) continue;

                    // Testing the position of this block to see which faces can be drawn
                    Vector3Int localPos = new Vector3Int(x, y, z);
                    byte faces = TestPos(localPos);

                    // If no faces can be drawn, we continue to the next block
                    if (faces == 0) continue;

                    // Calculating the position of each vertex of this block in the chunk
                    Vector3Int[] vertices = new Vector3Int[8]
                    {
                        localVertsPos[0] + localPos,
                        localVertsPos[1] + localPos,
                        localVertsPos[2] + localPos,
                        localVertsPos[3] + localPos,

                        localVertsPos[4] + localPos,
                        localVertsPos[5] + localPos,
                        localVertsPos[6] + localPos,
                        localVertsPos[7] + localPos
                    };
                    int i = verts.Count;

                    /*bool front = (faces & 0x01) > 0;
                    bool back = (faces & 0x02) > 0;
                    bool left = (faces & 0x04) > 0;
                    bool right = (faces & 0x08) > 0;
                    bool top = (faces & 0x10) > 0;
                    bool bottom = (faces & 0x20) > 0;*/

                    // Drawing each face that we need to draw
                    if ((faces & 0x01) > 0)
                    {
                        verts.Add(vertices[0]);
                        verts.Add(vertices[1]);
                        verts.Add(vertices[2]);
                        verts.Add(vertices[3]);

                        uvs.Add(uvsA[2]);
                        uvs.Add(uvsA[3]);
                        uvs.Add(uvsA[1]);
                        uvs.Add(uvsA[0]);

                        indices.Add(i);
                        indices.Add(i + 3);
                        indices.Add(i + 1);
                        indices.Add(i + 1);
                        indices.Add(i + 3);
                        indices.Add(i + 2);

                        i += 4;
                    }
                    if ((faces & 0x02) > 0)
                    {
                        verts.Add(vertices[4]);
                        verts.Add(vertices[5]);
                        verts.Add(vertices[6]);
                        verts.Add(vertices[7]);

                        uvs.Add(uvsA[2]);
                        uvs.Add(uvsA[3]);
                        uvs.Add(uvsA[1]);
                        uvs.Add(uvsA[0]);

                        indices.Add(i);
                        indices.Add(i + 3);
                        indices.Add(i + 1);
                        indices.Add(i + 1);
                        indices.Add(i + 3);
                        indices.Add(i + 2);

                        i += 4;

                        // If we draw a face here, we know the next block is a void block so we skip it
                        z++;
                    }
                    if ((faces & 0x04) > 0)
                    {
                        verts.Add(vertices[5]);
                        verts.Add(vertices[0]);
                        verts.Add(vertices[3]);
                        verts.Add(vertices[6]);

                        uvs.Add(uvsA[2]);
                        uvs.Add(uvsA[3]);
                        uvs.Add(uvsA[1]);
                        uvs.Add(uvsA[0]);

                        indices.Add(i);
                        indices.Add(i + 3);
                        indices.Add(i + 1);
                        indices.Add(i + 1);
                        indices.Add(i + 3);
                        indices.Add(i + 2);

                        i += 4;
                    }
                    if ((faces & 0x08) > 0)
                    {
                        verts.Add(vertices[1]);
                        verts.Add(vertices[4]);
                        verts.Add(vertices[7]);
                        verts.Add(vertices[2]);

                        uvs.Add(uvsA[2]);
                        uvs.Add(uvsA[3]);
                        uvs.Add(uvsA[1]);
                        uvs.Add(uvsA[0]);

                        indices.Add(i);
                        indices.Add(i + 3);
                        indices.Add(i + 1);
                        indices.Add(i + 1);
                        indices.Add(i + 3);
                        indices.Add(i + 2);

                        i += 4;
                    }
                    if ((faces & 0x10) > 0)
                    {
                        verts.Add(vertices[3]);
                        verts.Add(vertices[2]);
                        verts.Add(vertices[7]);
                        verts.Add(vertices[6]);

                        uvs.Add(uvsA[2]);
                        uvs.Add(uvsA[3]);
                        uvs.Add(uvsA[1]);
                        uvs.Add(uvsA[0]);

                        indices.Add(i);
                        indices.Add(i + 3);
                        indices.Add(i + 1);
                        indices.Add(i + 1);
                        indices.Add(i + 3);
                        indices.Add(i + 2);

                        i += 4;
                    }
                    if ((faces & 0x20) > 0)
                    {
                        verts.Add(vertices[5]);
                        verts.Add(vertices[4]);
                        verts.Add(vertices[1]);
                        verts.Add(vertices[0]);

                        uvs.Add(uvsA[2]);
                        uvs.Add(uvsA[3]);
                        uvs.Add(uvsA[1]);
                        uvs.Add(uvsA[0]);

                        indices.Add(i);
                        indices.Add(i + 3);
                        indices.Add(i + 1);
                        indices.Add(i + 1);
                        indices.Add(i + 3);
                        indices.Add(i + 2);
                    }

                }
            }
        }
    }
}

[System.Serializable]
public class ChunkData
{
    public string compressed;

    public ChunkData(byte[,,] data)
    {
        compressed = Compress(data);
    }

    public static string Compress(byte[,,] data)
    {
        byte current = data[0, 0, 0];
        int num = 0;
        string compressed = "";

        int width = World.chunkSize.x;
        int height = World.chunkSize.y;

        int w1 = width - 1;
        int h1 = height - 1;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    byte blockID = data[x, y, z];
                    if (blockID != current)
                    {
                        compressed += current + "," + num.ToString("X") + ",";
                        current = blockID;
                        num = 1;
                        continue;
                    }

                    num++;

                    if (x == w1 && y == h1 && z == w1)
                        compressed += blockID + "," + num.ToString("X");
                }
            }
        }

        return compressed;
    }

    public static byte[,,] Decompress(string compressed)
    {
        byte width = (byte)World.chunkSize.x;
        int height = World.chunkSize.y;
        byte[,,] data = new byte[width, height, width];

        string[] csv = compressed.Split(',');
        byte blockID = byte.Parse(csv[0]);
        int num = int.Parse(csv[1], System.Globalization.NumberStyles.HexNumber);

        int iter = 0;
        int wh = width * height;
        int end = width * wh;
        for (int i = 0; i < end; i++)
        {
            if (num == 0)
            {
                iter += 2;

                if (iter >= csv.Length - 1) break;

                blockID = byte.Parse(csv[iter]);
                num = int.Parse(csv[iter + 1], System.Globalization.NumberStyles.HexNumber);

                if (blockID == 0)
                {
                    i += num - 1;
                    num = 0;
                    continue;
                }
            }

            float xr = i / (float)wh;
            byte x = (byte)xr;
            byte z = Utility.ModuloB(i, width);
            byte y = (byte)((xr - x) * height);

            data[x, y, z] = blockID;
            num--;
        }
        return data;
    }
}
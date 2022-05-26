using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplexNoise;

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
    private Vector2[] uvsA = new Vector2[4]
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, 0)

    }; // Uvs for each block

    private List<Vector3> verts = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> indices = new List<int>();

    private float yBias;

    public bool meshGenerated = false;

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

        float bval = 241f / 255f;
        float bval2 = 14f / 255f;
        for (int y = 0; y < height; y++)
        {
            float bias = 1 - (Mathf.Abs(yBias - y) / yBias);
            float bias2 = bval + bval2 * bias;
            for (int x = 0; x < width; x++)
            {
                int offX = x + offset.x;
                for (int z = 0; z < width; z++)
                {
                    float value = Noise.CalcPixel3D(offX, y, z + offset.z, scale) * bias2;

                    data[x, y, z] = value >= 241 ? (byte)1 : (byte)0;
                }

            }
        }

        isGenerated = true;
    }

    // Generates chunk mesh
    public void GenerateMeshNC()
    {
        mesh.Clear();

        Vector3Int localPos = new Vector3Int();
        Vector3Int worldPos = new Vector3Int();
        byte faces;
        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3()
        };
        int i = 0;

        for (int x = 0; x < width; x++)
        {
            localPos.x = x;
            worldPos.x = x + chunkPos.x * width;
            for (int z = 0; z < width; z++)
            {
                localPos.z = z;
                worldPos.z = z + chunkPos.y * width;
                for (int y = 0; y < height; y++)
                {
                    // We ignore this block if it is void
                    if (data[x, y, z] == 0) continue;

                    // Testing the position of this block to see which faces can be drawn
                    localPos.y = y;
                    worldPos.y = y;

                    faces = TestPos(worldPos);

                    // If no faces can be drawn, we continue to the next block
                    if (faces == 0) continue;

                    // Calculating the position of each vertex of this block in the chunk
                    vertices[0] = localVertsPos[0] + localPos;
                    vertices[1] = localVertsPos[1] + localPos;
                    vertices[2] = localVertsPos[2] + localPos;
                    vertices[3] = localVertsPos[3] + localPos;
                    vertices[4] = localVertsPos[4] + localPos;
                    vertices[5] = localVertsPos[5] + localPos;
                    vertices[6] = localVertsPos[6] + localPos;
                    vertices[7] = localVertsPos[7] + localPos;

                    // Drawing each face that we need to draw
                    if ((faces & 0x01) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[0], vertices[1], vertices[2], vertices[3] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }
                    if ((faces & 0x02) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[4], vertices[5], vertices[6], vertices[7] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }
                    if ((faces & 0x04) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[5], vertices[0], vertices[3], vertices[6] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }
                    if ((faces & 0x08) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[1], vertices[4], vertices[7], vertices[2] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }
                    if ((faces & 0x10) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[3], vertices[2], vertices[7], vertices[6] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                        y++;
                    }
                    if ((faces & 0x20) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[5], vertices[4], vertices[1], vertices[0] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }

                }
            }
        }

        mesh.vertices = verts.ToArray();
        mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        mesh.SetUVs(0, uvs.ToArray());

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public IEnumerator GenerateMesh()
    {
        mesh.Clear();

        Vector3Int localPos = new Vector3Int();
        Vector3Int worldPos = new Vector3Int();
        byte faces;
        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3(),
            new Vector3()
        };
        int i = 0;

        for (int x = 0; x < width; x++)
        {
            localPos.x = x;
            worldPos.x = x + chunkPos.x * width;
            for (int z = 0; z < width; z++)
            {
                localPos.z = z;
                worldPos.z = z + chunkPos.y * width;
                for (int y = 0; y < height; y++)
                {
                    // We ignore this block if it is void
                    if (data[x, y, z] == 0) continue;

                    // Testing the position of this block to see which faces can be drawn
                    localPos.y = y;
                    worldPos.y = y;

                    faces = TestPos(worldPos);

                    // If no faces can be drawn, we continue to the next block
                    if (faces == 0) continue;

                    // Calculating the position of each vertex of this block in the chunk
                    vertices[0] = localVertsPos[0] + localPos;
                    vertices[1] = localVertsPos[1] + localPos;
                    vertices[2] = localVertsPos[2] + localPos;
                    vertices[3] = localVertsPos[3] + localPos;
                    vertices[4] = localVertsPos[4] + localPos;
                    vertices[5] = localVertsPos[5] + localPos;
                    vertices[6] = localVertsPos[6] + localPos;
                    vertices[7] = localVertsPos[7] + localPos;

                    // Drawing each face that we need to draw
                    if ((faces & 0x01) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[0], vertices[1], vertices[2], vertices[3] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }
                    if ((faces & 0x02) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[4], vertices[5], vertices[6], vertices[7] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }
                    if ((faces & 0x04) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[5], vertices[0], vertices[3], vertices[6] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }
                    if ((faces & 0x08) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[1], vertices[4], vertices[7], vertices[2] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }
                    if ((faces & 0x10) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[3], vertices[2], vertices[7], vertices[6] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                        y++;
                    }
                    if ((faces & 0x20) > 0)
                    {
                        verts.AddRange(new Vector3[4] { vertices[5], vertices[4], vertices[1], vertices[0] });

                        uvs.AddRange(uvsA);

                        indices.AddRange(new int[6] { i, i + 3, i + 1, i + 1, i + 3, i + 2 });

                        i += 4;
                    }

                }
            }
        }

        meshGenerated = true;

        mesh.vertices = verts.ToArray();
        mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        mesh.SetUVs(0, uvs.ToArray());

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        yield return null;
    }

    // Tests the given block position to see which faces can be drawn

    public byte TestPos(Vector3Int pos)
    {
        byte flags = 0x00;

        Vector3Int up = pos + Vector3Int.up;
        Vector3Int down = pos + Vector3Int.down;
        Vector3Int left = pos + Vector3Int.left;
        Vector3Int right = pos + Vector3Int.right;
        Vector3Int forward = pos + Vector3Int.forward;
        Vector3Int back = pos + Vector3Int.back;

        flags += world.GetBlock(back) == 0 ? (byte)0x01 : (byte)0;
        flags += world.GetBlock(forward) == 0 ? (byte)0x02 : (byte)0;
        flags += world.GetBlock(left) == 0 ? (byte)0x04 : (byte)0;
        flags += world.GetBlock(right) == 0 ? (byte)0x08 : (byte)0;
        flags += world.GetBlock(up) == 0 ? (byte)0x10 : (byte)0;
        flags += world.GetBlock(down) == 0 ? (byte)0x20 : (byte)0;

        return flags;
    }

    public IEnumerator UpdateSurroundingChunks()
    {
        for (int x = -1; x < 2; x++)
        {
            for (int z = -1; z < 2; z++)
            {
                if (Mathf.Abs(x) == Mathf.Abs(z)) continue;

                Vector2Int chunkPos = new Vector2Int(x, z) + this.chunkPos;
                Sector sector = world.GetSectorFromChunkPos(chunkPos);

                if (sector == null) continue;

                Chunk chunk = sector.GetChunk(chunkPos);

                if (chunk != null && chunk.meshGenerated)
                    chunk.GenerateMeshNC();
            }
        }

        yield return null;
    }

    public byte GetBlock(Vector3Int pos)
    {
        if (pos.x < 0 || pos.z < 0) return 0;
        Vector3Int relative = new Vector3Int(pos.x % World.chunkSize.x, pos.y, pos.z % World.chunkSize.x);

        return data[relative.x, relative.y, relative.z];
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
        byte blockID;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    blockID = data[x, y, z];
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
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSector
{
    private static string p = Application.persistentDataPath;

    public static void Save(Sector sector)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = p + "/sector" + sector.sectorPos.x + "-" + sector.sectorPos.y + ".pb";

        FileStream stream = new FileStream(path, FileMode.Create);

        SectorData sd = new SectorData(sector.chunks, sector.sectorPos);
        formatter.Serialize(stream, sd);
        stream.Close();
    }

    public static Sector Load(Vector2Int sectorPos)
    {
        string path = p + "/sector" + sectorPos.x + "-" + sectorPos.y + ".pb";

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Open);

        SectorData data = (SectorData)formatter.Deserialize(stream);
        stream.Close();

        Sector s = new Sector(new Vector2Int(data.sectorPosX, data.sectorPosY), World.sectorSize);
        data.Decompress(s);
        return s;
    }

    public static bool FileExists(Vector2Int sectorPos)
    {
        string path = p + "/sector" + sectorPos.x + "-" + sectorPos.y + ".pb";

        return File.Exists(path);
    }
}

using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSector
{
    public static void Save(Sector sector)
    {
        BinaryFormatter formatter = new BinaryFormatter();

        string path = Application.persistentDataPath + "/sector" + sector.sectorPos.x + "-" + sector.sectorPos.y + ".pb";
        FileStream stream = new FileStream(path, FileMode.Create);

        SectorData sd = new SectorData(sector.chunks, sector.sectorPos);
        formatter.Serialize(stream, sd);
        stream.Close();
    }

    public static Sector Load(Vector2Int sectorPos)
    {
        string path = Application.persistentDataPath + "/sector" + sectorPos.x + "-" + sectorPos.y + ".pb";

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            SectorData data = (SectorData)formatter.Deserialize(stream);
            stream.Close();

            Sector s = new Sector(new Vector2Int(data.sectorPosX, data.sectorPosY), World.sectorSize);
            s.chunks = SectorData.GetChunkFromData(data.chunks, s);
            return s;
        }
        return null;
    }
}

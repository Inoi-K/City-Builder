using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveLoadSystem {
    static readonly string fileName = "/city.data";
    static readonly string path = Application.persistentDataPath + fileName;

    public static void SaveData(GameData data) {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static GameData LoadData() {
        if (!File.Exists(path)) {
            Debug.LogError("Saves not found");
            return null;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Open);

        GameData data = formatter.Deserialize(stream) as GameData;
        stream.Close();

        return data;
    }
}

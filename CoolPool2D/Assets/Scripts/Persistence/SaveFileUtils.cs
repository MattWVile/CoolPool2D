using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SaveFileUtils
{
    public static void SaveDataToFile(Data data, string filePath = "PlayerData") {
        string json = JsonUtility.ToJson(data);
        System.IO.File.WriteAllText(filePath, json);
    }

    public static Data LoadDataFromFile(string filePath = "PlayerData") {
        if (System.IO.File.Exists(filePath)) {
            string json = System.IO.File.ReadAllText(filePath);
            Data data = JsonUtility.FromJson<Data>(json);
            return data;
        }
        return null;
    }
}

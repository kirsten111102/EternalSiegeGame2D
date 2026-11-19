using System;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public void SaveGame(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("Save failed: SaveData is null.");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            string path = GetSavePath();

            File.WriteAllText(path, json);

            Debug.Log($"Game saved: {path}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Save failed: {exception.Message}");
        }
    }

    public SaveData LoadGame()
    {
        string path = GetSavePath();

        if (!File.Exists(path))
        {
            Debug.Log("No save file found.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Load failed: {exception.Message}");
            return null;
        }
    }

    public bool HasSaveFile()
    {
        return File.Exists(GetSavePath());
    }

    public void DeleteSave()
    {
        string path = GetSavePath();

        try
        {
            if (!File.Exists(path))
                return;

            File.Delete(path);

            Debug.Log("Save file deleted.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Delete save failed: {exception.Message}");
        }
    }

    private string GetSavePath()
    {
        return Path.Combine(
            Application.persistentDataPath,
            "savegame.json"
        );
    }
}
using System;
using UnityEngine;

// Keep existing scene references and button bindings to this component.
// Network requests run on a separate persistent object, surviving scene changes.
public class SaveSystem : MonoBehaviour
{
    public void SaveGame(SaveData data) => CloudSaveClient.Instance.SaveGame(data);
    public void LoadGame(Action<SaveData> onLoaded) => CloudSaveClient.Instance.LoadGame(onLoaded);
    public void RetryPendingScore() => CloudSaveClient.Instance.RetryPendingScore();
}

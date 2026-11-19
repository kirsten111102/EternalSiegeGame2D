using UnityEngine;

public class SaveLoadUI : MonoBehaviour
{
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private Transform player;
    // [SerializeField] private PlayerHealth playerHealth;
    // [SerializeField] private GameScore gameScore;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameSaveController gameSaveController;
    public void SaveGame()
    {
        if (gameSaveController == null)
        {
            Debug.LogWarning("GameSaveController is missing.");
            return;
        }

        gameSaveController.SaveGame();
    }

    public void LoadGame()
    {
        if (gameSaveController == null)
        {
            Debug.LogWarning("GameSaveController is missing.");
            return;
        }

        gameSaveController.LoadGame();
    }

}
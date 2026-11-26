using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenu : MonoBehaviour
{
    [SerializeField] private SaveLoadUI saveLoadUI;
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private Button continueButton;
    public void StartGame()
    {
        if (!CloudSaveClient.Instance.CanChangeScene)
        {
            Debug.LogWarning("Wait for server requests / retry the pending score before New Game.");
            return;
        }
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            "MainMap",
            LoadSceneMode.Single
        );
    }

    private void UpdateContinueButton()
    {
        if (continueButton == null)
            return;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        continueButton.interactable =
            gameManager != null &&
            gameManager.CanContinue;
    }

    public void ContinueGame()
    {
        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null || !gameManager.CanContinue)
        {
            Debug.LogWarning("No paused game to continue.");
            UpdateContinueButton();
            return;
        }
        gameManager.ResumeGame();
    }

    public void LoadGame()
    {
        GameSaveController.RequestLoadGame(saveSystem);
    }

    private void Start()
    {
        UpdateContinueButton();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
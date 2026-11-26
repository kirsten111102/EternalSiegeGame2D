using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameScore gameScore;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMPro.TextMeshProUGUI finalScoreText;
    [SerializeField] private GameObject player;
    [Header("Disable while Main Menu is open")]
    [SerializeField] private Behaviour[] gameplayComponents;
    [Header("Leaderboard submission")]
    [SerializeField] private GameXP gameXP;

    [Header("Music")]
    [SerializeField] private GameplayMusic gameplayMusic;

    private readonly List<Behaviour> disabledForMenu = new();
    private bool isChangingMenu;

    public bool CanContinue => isPaused && !isGameOver;
    public bool IsGameOver => isGameOver;
    private bool isPaused;
    private bool isGameOver;
    // comment
    private void Start()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        isPaused = false;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void GameOver() => EndRun(false);
    public void CampaignCompleted() => EndRun(true);

    private void EndRun(bool victory)
    {
        if (isGameOver) return;
        isGameOver = true;
        isPaused = false;

        if (gameScore != null && finalScoreText != null)
            finalScoreText.text = (victory ? "Victory! Score: " : "Final Score: ") + gameScore.Score;

        if (gameXP != null && gameScore != null)
            CloudSaveClient.Instance.SubmitResult(gameXP.CurrentLevel, gameScore.Score);
        else
            Debug.LogError("Cannot submit result: assign GameXP and GameScore.");

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        if (!CloudSaveClient.Instance.CanChangeScene)
        {
            Debug.LogWarning("Wait for server requests / retry the pending score before restarting.");
            return;
        }
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            "MainMap",
            LoadSceneMode.Single
        );
    }

    public void PauseGame()
    {
        if (isGameOver || isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
    }

    //Resume o MainMap
    public void ResumeGame()
    {
        if (!CanContinue || isChangingMenu)
            return;

        if (SceneManager.GetSceneByName("Main Menu").isLoaded)
        {
            StartCoroutine(CloseMainMenuAndResume());
            return;
        }

        FinishResume();
    }

    private IEnumerator CloseMainMenuAndResume()
    {
        isChangingMenu = true;

        // GameManager vẫn thuộc scene MainMap.
        SceneManager.SetActiveScene(gameObject.scene);

        yield return SceneManager.UnloadSceneAsync("Main Menu");

        RestoreGameplayComponents();

        //Restore Gameplay Music
        if (gameplayMusic != null)
            gameplayMusic.UnmuteMusic();

        isChangingMenu = false;
        FinishResume();
    }

    private void FinishResume()
    {
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
    }

    //Continue o mainmenu
    private void DisableGameplayComponents()
    {
        disabledForMenu.Clear();
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            Behaviour[] components =
                root.GetComponentsInChildren<Behaviour>(true);

            foreach (Behaviour component in components)
            {
                if (component is Camera ||
                    component is AudioListener ||
                    component is Canvas ||
                    component is UnityEngine.UI.GraphicRaycaster ||
                    component is EventSystem ||
                    component is BaseInputModule)
                {
                    DisableForMenu(component);
                }
            }
        }

        if (gameplayComponents != null)
        {
            foreach (Behaviour component in gameplayComponents)
                DisableForMenu(component);
        }
    }

    private void DisableForMenu(Behaviour component)
    {
        if (component == null ||
            component == this ||
            !component.enabled ||
            disabledForMenu.Contains(component))
        {
            return;
        }

        disabledForMenu.Add(component);
        component.enabled = false;
    }

    private void RestoreGameplayComponents()
    {
        foreach (Behaviour component in disabledForMenu)
        {
            if (component != null)
                component.enabled = true;
        }

        disabledForMenu.Clear();
    }

    private IEnumerator OpenMainMenu()
    {
        isChangingMenu = true;
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Pause gameplay Music
        if (gameplayMusic != null)
            gameplayMusic.MuteMusic();

        DisableGameplayComponents();

        Time.timeScale = 0f;

        yield return SceneManager.LoadSceneAsync(
            "Main Menu",
            LoadSceneMode.Additive
        );

        Scene menuScene = SceneManager.GetSceneByName("Main Menu");

        if (menuScene.IsValid() && menuScene.isLoaded)
            SceneManager.SetActiveScene(menuScene);

        Time.timeScale = 0f;
        isChangingMenu = false;
    }

    public void ReturnToMainMenu()
    {
        if (isChangingMenu)
            return;

        if (SceneManager.GetSceneByName("Main Menu").isLoaded)
            return;

        StartCoroutine(OpenMainMenu());
    }

    private void Update()
    {
        if (isGameOver ||
            isChangingMenu ||
            SceneManager.GetSceneByName("Main Menu").isLoaded)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private void LateUpdate()
    {
        if (isPaused || isGameOver || isChangingMenu)
        {
            Time.timeScale = 0f;
        }
    }
}

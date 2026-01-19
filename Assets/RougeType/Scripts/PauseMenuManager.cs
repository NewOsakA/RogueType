using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuPanel;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        if (GameManager.Instance != null)
            GameManager.Instance.isPaused = true;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        if (GameManager.Instance != null)
            GameManager.Instance.isPaused = false;
    }

    public void GoToUpgrade()
    {
        // Change this when change scene name
        EndRunAndLoadScene("Upgrade");
    }

    public void GoToMainMenu()
    {   
        // Change this when change scene name
        EndRunAndLoadScene("Main manu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void EndRunAndLoadScene(string sceneName)
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.EndRunWithoutSave();

        SceneManager.LoadScene(sceneName);
    }
}

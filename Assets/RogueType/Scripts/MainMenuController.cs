using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OnPlayButton()
    {
        SceneManager.LoadScene("Save Selection Scene");
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OnPlayButton()
    {
        SceneManager.LoadScene("Upgrade");
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}

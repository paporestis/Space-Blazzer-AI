using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;
    }

    public void NewGame()
    {
        SceneManager.LoadScene("Base Level");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

using UnityEngine.SceneManagement;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject creditsCanva;
    public GameObject menuCanva;
    public GameObject controlsCanva;
    public void PlayGame()
    {
        SceneManager.LoadScene("Level");
    }

    public void LoadCredits()
    {
        creditsCanva.SetActive(true);
        menuCanva.SetActive(false);
    }

    public void HideCredits()
    {
        creditsCanva.SetActive(false);
        menuCanva.SetActive(true);
    }

    public void LoadInstructios()
    {
        controlsCanva.SetActive(true);
        menuCanva.SetActive(false);
    }

    public void HideInstructions()
    {
        controlsCanva.SetActive(false);
        menuCanva.SetActive(true);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}

using UnityEngine.SceneManagement;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
   private bool isPaused = false;
   private GameObject canvas;
    private GameObject pausePanel;

    void Awake()
    {
        canvas = GameObject.Find("PauseCanvas");
        if (canvas != null)
        {
            Transform panelTransform = canvas.transform.Find("PauseOptions");
            if (panelTransform != null)
            {
                pausePanel = panelTransform.gameObject;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused) {ResumeGame();} else {PauseGame();}
        }
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

}

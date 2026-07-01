using UnityEngine;
using AnteNoctem.Core;

public class PauseManager : Singleton<PauseManager>
{
    private bool isPaused = false;
    private GameObject canvas;
    private GameObject pausePanel;

    public bool IsPaused => isPaused;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // No permitir pausar si el juego termino
            if (GameManager.Instance != null &&
                GameManager.Instance.gameState != GameManager.GameState.Playing)
            {
                return;
            }

            // No permitir pausar si hay un quick event en curso (ciudadano)
            if (QuickEventManager.Instance != null && QuickEventManager.Instance.IsActive)
            {
                return;
            }

            // No permitir pausar si hay un EventFlower activo (basurero/planta)
            if (IsAnyEventFlowerActive())
            {
                return;
            }

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    bool IsAnyEventFlowerActive()
    {
        var flowers = FindObjectsByType<EventFlower>(FindObjectsSortMode.None);
        foreach (var f in flowers)
        {
            if (f != null && f.IsActive) return true;
        }
        return false;
    }

    public void ResumeGame()
    {
        // No reanudar si el juego ya termino
        if (GameManager.Instance != null &&
            GameManager.Instance.gameState != GameManager.GameState.Playing)
        {
            return;
        }

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
        LoadingScreen.LoadingScreenAsync("Menu");
    }
}
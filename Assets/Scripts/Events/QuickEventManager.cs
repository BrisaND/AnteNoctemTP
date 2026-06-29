using UnityEngine;
using AnteNoctem.Core;

public class QuickEventManager : Singleton<QuickEventManager>
{
    [Header("UI")]
    public GameObject quickEventCanvas;
    public SkillCheck skillCheck;

    private CitizenAI currentCitizen;
    private bool isActive = false;

    public bool IsActive => isActive;

    void Start()
    {
        if (quickEventCanvas != null) quickEventCanvas.SetActive(false);
        if (skillCheck != null)
        {
            skillCheck.OnSkillCheckResult.RemoveAllListeners();
        }
    }

    public void StartQuickEvent(CitizenAI citizen)
    {
        if (quickEventCanvas == null || skillCheck == null) return;

        // No iniciar si el juego no esta en Playing
        if (GameManager.Instance != null &&
            GameManager.Instance.gameState != GameManager.GameState.Playing)
        {
            return;
        }

        // No iniciar si esta pausado
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        currentCitizen = citizen;
        isActive = true;
        quickEventCanvas.SetActive(true);

                Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ajustar velocidad del SkillCheck según dificultad del ciudadano
        float multiplier = 1f;
        if (citizen != null)
        {
            switch (citizen.difficulty)
            {
                case RobberySystem.DifficultyLevel.Easy: multiplier = 1f; break;
                case RobberySystem.DifficultyLevel.Medium: multiplier = 3.6f; break;
                case RobberySystem.DifficultyLevel.Hard: multiplier = 5.4f; break;
            }
        }
        skillCheck.SetSpeedMultiplier(multiplier);

        skillCheck.OnSkillCheckResult.AddListener(HandleSkillResult);
        skillCheck.StartSkillCheck();
    }

    void HandleSkillResult(bool success)
    {
        skillCheck.OnSkillCheckResult.RemoveListener(HandleSkillResult);

        quickEventCanvas.SetActive(false);
        isActive = false;

        // Solo restaurar el tiempo y el cursor si el juego sigue en Playing
        if (GameManager.Instance == null ||
            GameManager.Instance.gameState == GameManager.GameState.Playing)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (currentCitizen != null)
        {
            currentCitizen.OnPlayerInteractionResult(success);
            currentCitizen = null;
        }
    }
}
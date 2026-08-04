//TPFinal - Brisa Desouches

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class ExitZone : MonoBehaviour
{
    [Header("Configuración General")]
    public string playerTag = "Player";
    [Tooltip("Nombre exacto de la escena de la Base en tu proyecto")]
    public string baseSceneName = "Base";

    [Header("Popup de Confirmación UI (Asignar en Inspector)")]
    public GameObject confirmationPanel;
    public TMP_Text confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Advertencia de Puntos")]
    [Tooltip("Sub-panel de UI opcional que dice 'Necesitas más puntos para escapar' si intenta irse antes")]
    public GameObject warningCanvasMessage;
    public float warningDuration = 3f;

    private bool isShowingWarning = false;
    private bool isWaitingForConfirmation = false;

    // Esta variable estática sobrevive al cambio de escenas. 
    public static bool hasVisitedBase = false;

    private void Awake()
    {
        // Enlazar los eventos de los botones Sí y No
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmYes);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmNo);

        HideConfirmation();

        if (warningCanvasMessage != null)
            warningCanvasMessage.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isWaitingForConfirmation) return;

        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            if (GameManager.Instance != null)
            {
                // ¿El jugador alcanzó o superó el puntaje objetivo?
                if (GameManager.Instance.currentScore >= GameManager.Instance.targetScore)
                {
                    ShowConfirmation();
                }
                else
                {
                    Debug.Log("No puedes escapar todavía, te faltan puntos.");
                    if (warningCanvasMessage != null && !isShowingWarning)
                    {
                        StartCoroutine(ShowWarningRoutine());
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Si el jugador sale del Trigger con el pop-up abierto, lo cerramos automáticamente
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            HideConfirmation();
        }
    }

    // --- MÉTODOS DE POPUP DE CONFIRMACIÓN ---

    private void ShowConfirmation()
    {
        isWaitingForConfirmation = true;

        if (confirmationText != null)
        {
            confirmationText.text = !hasVisitedBase
                ? "¿Deseas escapar a la Base?"
                : "¿Deseas escapar y finalizar el nivel?";
        }

        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);

        // Opcional: Pausar y liberar cursor para el clic
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnConfirmYes()
    {
        // Reanudamos el tiempo y ocultamos el panel
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        StartCoroutine(EscapeSequenceRoutine());
    }

    private void OnConfirmNo()
    {
        HideConfirmation();
    }

    private void HideConfirmation()
    {
        isWaitingForConfirmation = false;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        // Reanudar juego por si estaba en pausa
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- SECUENCIAS ---

    private IEnumerator EscapeSequenceRoutine()
    {
        // 1. Fundido a negro de la pantalla
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeToBlackRoutine());
        }
        else
        {
            Debug.LogWarning("No se encontró una instancia de ScreenFader en la escena.");
            yield return new WaitForSeconds(0.5f);
        }

        // 2. Decidir a dónde enviar al jugador
        if (!hasVisitedBase)
        {
            // PRIMER ESCAPE -> Ir a la Base
            hasVisitedBase = true;
            Debug.Log("Primer escape exitoso. Viajando a la base para comprar items...");

            SceneManager.LoadScene(baseSceneName);
        }
        else
        {
            // SEGUNDO ESCAPE -> ¡Victoria definitiva!
            Debug.Log("Segundo escape exitoso con puntos recolectados de nuevo. ¡Ganaste!");

            hasVisitedBase = false;
            GameManager.Instance.CompleteLevelAndLoadVictory();
        }
    }

    private IEnumerator ShowWarningRoutine()
    {
        isShowingWarning = true;
        warningCanvasMessage.SetActive(true);
        yield return new WaitForSeconds(warningDuration);
        warningCanvasMessage.SetActive(false);
        isShowingWarning = false;
    }
}
//TPFinal - Brisa Desouches

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Necesario para cargar la escena de la base

public class ExitZone : MonoBehaviour
{
    [Header("Configuración")]
    public string playerTag = "Player";
    [Tooltip("Nombre exacto de la escena de la Base en tu proyecto")]
    public string baseSceneName = "Base";

    [Tooltip("Sub-panel de UI opcional que dice 'Necesitas más puntos para escapar' si intenta irse antes")]
    public GameObject warningCanvasMessage;
    public float warningDuration = 3f;

    private bool isShowingWarning = false;

    // Esta variable estática sobrevive al cambio de escenas. 
    // Recuerda si el jugador ya completó la primera fase e hizo su viaje a la base.
    public static bool hasVisitedBase = false;

    private void Awake()
    {
        if (warningCanvasMessage != null) warningCanvasMessage.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            if (GameManager.Instance != null)
            {
                // ¿El jugador alcanzó o superó el puntaje objetivo?
                if (GameManager.Instance.currentScore >= GameManager.Instance.targetScore)
                {
                    Debug.Log("Puntaje alcanzado. Evaluando destino de escape...");
                    StartCoroutine(EscapeSequenceRoutine());
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

            // Cargamos la escena de la base usando el SceneManager
            SceneManager.LoadScene(baseSceneName);
        }
        else
        {
            // SEGUNDO ESCAPE -> ¡Victoria definitiva!
            Debug.Log("Segundo escape exitoso con puntos recolectados de nuevo. ¡Ganaste!");

            // Reseteamos la variable para futuras partidas antes de ir a la victoria
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
using UnityEngine;
using System.Collections; // Necesario para IEnumerator y las Corrutinas

public class ExitZone : MonoBehaviour
{
    [Header("Configuración")]
    public string playerTag = "Player";

    [Tooltip("Sub-panel de UI opcional que dice 'Necesitas más puntos para escapar' si intenta irse antes")]
    public GameObject warningCanvasMessage;
    public float warningDuration = 3f;

    private bool isShowingWarning = false;

    private void Awake()
    {
        // Usamos Awake para asegurarnos de apagar el cartel en el frame 0 
        // independientemente de cómo esté configurado en el Inspector.
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
                    Debug.Log("¡Escape exitoso por la puerta! Iniciando fundido a negro...");

                    // --- CAMBIO AQUÍ: Iniciamos la corrutina de escape con fade ---
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

    // --- NUEVA CORRUTINA DE SECUENCIA DE ESCAPE ---
    private IEnumerator EscapeSequenceRoutine()
    {
        // 1. Buscamos si el ScreenFader existe en la escena actual
        if (ScreenFader.Instance != null)
        {
            // Esperamos a que la corrutina del fader termine por completo (pantalla 100% negra)
            yield return StartCoroutine(ScreenFader.Instance.FadeToBlackRoutine());
        }
        else
        {
            // Salvaguarda: Si te olvidaste de poner el ScreenFader en esta escena, 
            // espera medio segundo para simular un tiempo de reacción y que no se rompa el juego.
            Debug.LogWarning("No se encontró una instancia de ScreenFader en la escena.");
            yield return new WaitForSeconds(0.5f);
        }

        // 2. Recién cuando terminó el fundido a negro, cargamos la escena definitiva mediante el GameManager
        GameManager.Instance.CompleteLevelAndLoadVictory();
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
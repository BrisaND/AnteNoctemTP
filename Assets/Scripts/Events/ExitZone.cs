using UnityEngine;

public class ExitZone : MonoBehaviour
{
    [Header("Configuración")]
    public string playerTag = "Player";

    [Tooltip("Sub-panel de UI opcional que dice 'Necesitas más puntos para escapar' si intenta irse antes")]
    public GameObject warningCanvasMessage;
    public float warningDuration = 3f;

    private bool isShowingWarning = false;

    private void Start()
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
                    Debug.Log("¡Escape exitoso por la puerta con puntos de sobra!");

                    // LLAMAMOS AL NUEVO MÉTODO QUE SÍ CARGA LA ESCENA
                    GameManager.Instance.CompleteLevelAndLoadVictory();
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

    private System.Collections.IEnumerator ShowWarningRoutine()
    {
        isShowingWarning = true;
        warningCanvasMessage.SetActive(true);
        yield return new WaitForSeconds(warningDuration);
        warningCanvasMessage.SetActive(false);
        isShowingWarning = false;
    }
}
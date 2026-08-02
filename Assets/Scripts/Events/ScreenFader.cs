//TPFinal - Malena Misson

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Configuración")]
    [Tooltip("El componente Image que cubre toda la pantalla.")]
    public Image fadeImage;
    [Tooltip("Duración en segundos del fundido a negro.")]
    public float fadeDuration = 1.5f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Nos aseguramos de que empiece transparente y bloquee o no clicks según corresponda
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false; // Para que no tape los botones del juego al inicio
        }
    }

    // La corrutina que realiza la transición
    public IEnumerator FadeToBlackRoutine()
    {
        if (fadeImage == null) yield break;

        fadeImage.raycastTarget = true; // Bloquea inputs del jugador durante la transición
        float timer = 0f;
        Color color = fadeImage.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Calculamos la opacidad de manera fluida de 0 a 1
            color.a = Mathf.Clamp01(timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        // Nos aseguramos de que quede negro puro al final
        color.a = 1f;
        fadeImage.color = color;
    }
}
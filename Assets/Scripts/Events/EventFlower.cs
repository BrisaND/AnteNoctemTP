using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class EventFlower : MonoBehaviour
{
    [Header("Configuración General")]
    [Tooltip("Número de pulsaciones de espacio necesarias para ganar")]
    public int requiredPresses = 10;

    [Header("Configuración de Tiempo")]
    [Tooltip("¿Este skillcheck tiene un límite de tiempo para completarse?")]
    public bool useTimeLimit = false;
    [Tooltip("Duración máxima si 'useTimeLimit' está activado")]
    public float duration = 3f;

    [Header("UI")]
    [Tooltip("Canvas que contiene el skillcheck")]
    public GameObject uiPanel;
    [Tooltip("Texto TextMeshPro")]
    public TMP_Text pressesTMP;
    [Tooltip("Barra que indica el progreso en el evento")]
    public Slider progressBar;

    public UnityEvent<bool> OnSkillCheckResult;

    int currentPresses;
    bool active;
    float timer;

    // Iniciacion del skillcheck
    public void StartSkillCheck()
    {
        if (active) return;
        currentPresses = 0;
        active = true;
        timer = duration; // Inicializa el temporizador por si se usa tiempo

        if (uiPanel != null) uiPanel.SetActive(true);
        UpdateUI();
        StartCoroutine(RunSkillCheck());
    }

    // Lógica del skillcheck unificada
    IEnumerator RunSkillCheck()
    {
        while (active)
        {
            // Si tiene límite de tiempo, restamos frame a frame
            if (useTimeLimit)
            {
                timer -= Time.unscaledDeltaTime;

                // Si el tiempo se agota, el jugador pierde el skillcheck
                if (timer <= 0f)
                {
                    Finish(false);
                    yield break;
                }
            }

            // Detección de la pulsación
            if (Input.GetKeyDown(KeyCode.Space))
            {
                currentPresses++;
                UpdateUI();

                if (currentPresses >= requiredPresses)
                {
                    Finish(true);
                    yield break;
                }
            }

            yield return null;
        }
    }

    // Actualiza el texto y la barra de progreso
    void UpdateUI()
    {
        string text = $"{currentPresses}/{requiredPresses}";

        if (pressesTMP != null)
        {
            pressesTMP.text = text;
        }

        if (progressBar != null)
        {
            progressBar.value = Mathf.Clamp01(currentPresses / (float)requiredPresses);
        }
    }

    // Finaliza el skillcheck, desactiva UI y lanza el evento con el resultado
    void Finish(bool success)
    {
        active = false;
        if (uiPanel != null) uiPanel.SetActive(false);
        OnSkillCheckResult?.Invoke(success);
    }

    // Permite cancelar el skillcheck desde fuera
    public void Cancel()
    {
        if (!active) return;
        active = false;
        if (uiPanel != null) uiPanel.SetActive(false);
        OnSkillCheckResult?.Invoke(false);
    }

    public bool IsActive => active;
}
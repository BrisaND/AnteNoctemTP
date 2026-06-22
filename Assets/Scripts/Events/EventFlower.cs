using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class EventFlower : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Número de pulsaciones de espacio necesarias para ganar")]
    public int requiredPresses = 10;
    [Tooltip("Duración para completar el skillcheck")]
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
    float timer;
    bool active;

    // Iniciacion del skillcheck
    public void StartSkillCheck()
    {
        if (active) return;
        currentPresses = 0;
        timer = duration;
        active = true;
        if (uiPanel != null) uiPanel.SetActive(true);
        UpdateUI();
        StartCoroutine(RunSkillCheck());
    }

    // Lógica del skillcheck
    IEnumerator RunSkillCheck()
    {
        while (active && timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;

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

            UpdateUI();
            yield return null;
        }

        if (active) Finish(false);
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

    // Permite cancelar el skillcheck desde fuera (si el jugador se libera o muere)
    public void Cancel()
    {
        if (!active) return;
        active = false;
        if (uiPanel != null) uiPanel.SetActive(false);
        OnSkillCheckResult?.Invoke(false);
    }

    // para saber si el skillcheck está activo
    public bool IsActive => active;
}
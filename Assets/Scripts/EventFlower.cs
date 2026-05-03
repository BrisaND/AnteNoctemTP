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
    [Tooltip("Duración máxima en segundos para completar el skillcheck")]
    public float duration = 3f;

    [Header("UI")]
    [Tooltip("Canvas o panel que contiene la UI del skillcheck (se activará/desactivará)")]
    public GameObject uiPanel;
    [Tooltip("Texto TextMeshPro (TMP_Text). Asigna si usas TextMeshPro.")]
    public TMP_Text pressesTMP;
    [Tooltip("Barra que indica el progreso en el evento")]
    public Slider progressBar;

    public UnityEvent<bool> OnSkillCheckResult;

    int currentPresses;
    float timer;
    bool active;

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

    void Finish(bool success)
    {
        active = false;
        if (uiPanel != null) uiPanel.SetActive(false);
        OnSkillCheckResult?.Invoke(success);
    }

    public void Cancel()
    {
        if (!active) return;
        active = false;
        if (uiPanel != null) uiPanel.SetActive(false);
        OnSkillCheckResult?.Invoke(false);
    }

    public bool IsActive => active;
}
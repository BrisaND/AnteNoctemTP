using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public abstract class BaseSkillCheck : MonoBehaviour
{
    [Header("Configuración Base")]
    [Tooltip("Número de pulsaciones de espacio necesarias para ganar")]
    public int requiredPresses = 10;

    [Header("UI Base")]
    [Tooltip("Canvas que contiene el skillcheck")]
    public GameObject uiPanel;
    [Tooltip("Texto TextMeshPro")]
    public TMP_Text pressesTMP;
    [Tooltip("Barra que indica el progreso en el evento")]
    public Slider progressBar;

    public UnityEvent<bool> OnSkillCheckResult;

    protected int currentPresses;
    protected bool active;

    // Método virtual de iniciación común
    public virtual void StartSkillCheck()
    {
        if (active) return;
        currentPresses = 0;
        active = true;

        if (uiPanel != null) uiPanel.SetActive(true);
        UpdateUI();
        StartCoroutine(RunSkillCheckRoutine());
    }

    // Corrutina obligatoria que cada subclase implementará según sus reglas de tiempo
    protected abstract IEnumerator RunSkillCheckRoutine();

    // Actualiza el texto y la barra de progreso
    protected virtual void UpdateUI()
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
    protected virtual void Finish(bool success)
    {
        active = false;
        if (uiPanel != null) uiPanel.SetActive(false);
        OnSkillCheckResult?.Invoke(success);
    }

    // Permite cancelar el skillcheck desde fuera (si el jugador se libera o muere)
    public virtual void Cancel()
    {
        if (!active) return;
        active = false;
        if (uiPanel != null) uiPanel.SetActive(false);
        OnSkillCheckResult?.Invoke(false);
    }

    public bool IsActive => active;
}
using UnityEngine;
using TMPro;

public class PlayerHudController : MonoBehaviour
{
    [Header("Referencias de Puntos")]
    [Tooltip("Texto TMP para mostrar los puntos (Ej: Puntos: 15 / 50)")]
    public TextMeshProUGUI scoreText;

    [Header("Referencias de Materiales (Individuales)")]
    [Tooltip("Texto TMP exclusivo para la cantidad de Hilo")]
    public TextMeshProUGUI hiloText;

    [Tooltip("Texto TMP exclusivo para la cantidad de Tela")]
    public TextMeshProUGUI telaText;

    [Tooltip("Texto TMP exclusivo para la cantidad de Cuero")]
    public TextMeshProUGUI cueroText;

    void LateUpdate()
    {
        UpdateScoreUI();
        UpdateMaterialsUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText == null) return;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            // Muestra puntos " [actual] / [necesarios]"
            scoreText.text = $"{gm.currentScore} / {gm.targetScore}";
        }
    }

    void UpdateMaterialsUI()
    {
        var inv = MaterialInventory.Instance;
        if (inv == null) return;

        // 1. Actualizar contador de Hilo
        if (hiloText != null)
        {
            int hiloCount = inv.GetCount(MaterialInventory.MaterialType.Hilo);
            hiloText.text = $"{hiloCount}";
        }

        // 2. Actualizar contador de Tela
        if (telaText != null)
        {
            int telaCount = inv.GetCount(MaterialInventory.MaterialType.Tela);
            telaText.text = $"{telaCount}";
        }

        // 3. Actualizar contador de Cuero
        if (cueroText != null)
        {
            int cueroCount = inv.GetCount(MaterialInventory.MaterialType.Cuero);
            cueroText.text = $"{cueroCount}";
        }
    }
}
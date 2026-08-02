//TPFinal - Joaquin Campana

using UnityEngine;

public class TimeClockUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("El RectTransform de la imagen de la aguja.")]
    public RectTransform needleTransform;

    [Header("Configuración de Rotación (Eje Z)")]
    [Tooltip("Ángulo de la aguja al inicio del día (Progreso = 1).")]
    public float startAngle = 90f;

    [Tooltip("Ángulo de la aguja al final de la noche (Progreso = 0).")]
    public float endAngle = -90f;

    [Header("Opciones")]
    [Tooltip("Si es true, usa el valor invertido (0 al inicio, 1 al final) igual que el controlador del cielo.")]
    public bool invertProgress = true;

    void LateUpdate()
    {
        UpdateNeedleRotation();
    }

    void UpdateNeedleRotation()
    {
        if (needleTransform == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // 1 = mañana/inicio, 0 = noche/final
        float timeRemaining01 = gm.GetDayProgress01();

        // Calculamos el factor de progreso (entre 0 y 1)
        float progress = Mathf.Clamp01(timeRemaining01);

        if (invertProgress)
        {
            // 0 al inicio (día), 1 al final (noche) -> igual al 'darkness' de tu otro script
            progress = 1f - progress;
        }

        // Interpolamos entre el ángulo inicial y final en el eje Z (que es el que rota en 2D)
        float currentAngle = Mathf.Lerp(startAngle, endAngle, progress);

        // Aplicamos la rotación local a la aguja
        needleTransform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}
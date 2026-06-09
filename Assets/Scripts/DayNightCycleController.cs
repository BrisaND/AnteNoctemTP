using UnityEngine;
using UnityEngine.Rendering;

/// Oscurece la escena de forma continua según el temporizador de GameManager.
/// Tiempo lleno (inicio) = día claro; tiempo en 0 = noche.
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class DayNightCycleController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Luz direccional principal (sol).")]
    public Light sunLight;
    [Tooltip("Yaw fijo del sol.")]
    public float sunYaw = -30f;

    [Header("Configuración del Sol Visual (Eje X, Color, Intensidad)")]
    [Tooltip("Curva para la inclinación del sol de 0 (Día) a 1 (Noche).")]
    public AnimationCurve sunPitchCurve = AnimationCurve.Linear(0f, 55f, 1f, -22f);
    [Tooltip("Gradiente de color del sol desde el inicio hasta el final del nivel.")]
    public Gradient sunColorGradient;
    [Tooltip("Curva de intensidad del sol de 0 (Día) a 1 (Noche).")]
    public AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0f, 1.35f, 1f, 0.12f);

    [Header("Ambiente Global (Cielo, Ecuador, Suelo)")]
    public bool controlAmbient = true;
    public Gradient ambientSkyGradient;
    public Gradient ambientEquatorGradient;
    public Gradient ambientGroundGradient;

    [Header("Niebla")]
    public bool controlFog = true;
    public Gradient fogColorGradient;
    public AnimationCurve fogDensityCurve = AnimationCurve.Linear(0f, 0.002f, 1f, 0.018f);

    [Header("Debug")]
    public bool disableDuplicateSunLights = true;

    void Awake() => ResolveSunLight();
    void Start() => ApplyFromGameManager();
    void LateUpdate() => ApplyFromGameManager();

    void ApplyFromGameManager()
    {
        if (sunLight == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // 1 = empezó (día), 0 = terminó (noche)
        float timeRemaining01 = gm.GetDayProgress01();

        // Convertimos a "progreso hacia la noche": 0 al inicio (día), 1 al final (noche)
        float darkness = 1f - Mathf.Clamp01(timeRemaining01);

        ApplyVisuals(darkness);

        if (RenderSettings.sun != sunLight)
            RenderSettings.sun = sunLight;
    }

    void ApplyVisuals(float darkness)
    {
        // 1. Aplicar al Sol (Rotación, Color e Intensidad) usando las curvas y gradientes
        float pitch = sunPitchCurve.Evaluate(darkness);
        sunLight.transform.rotation = Quaternion.Euler(pitch, sunYaw, 0f);

        if (sunColorGradient != null) sunLight.color = sunColorGradient.Evaluate(darkness);
        sunLight.intensity = sunIntensityCurve.Evaluate(darkness);

        // 2. Aplicar Iluminación Ambiental
        if (controlAmbient)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (ambientSkyGradient != null) RenderSettings.ambientSkyColor = ambientSkyGradient.Evaluate(darkness);
            if (ambientEquatorGradient != null) RenderSettings.ambientEquatorColor = ambientEquatorGradient.Evaluate(darkness);
            if (ambientGroundGradient != null) RenderSettings.ambientGroundColor = ambientGroundGradient.Evaluate(darkness);
        }

        // 3. Aplicar Niebla
        if (controlFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            if (fogColorGradient != null) RenderSettings.fogColor = fogColorGradient.Evaluate(darkness);
            RenderSettings.fogDensity = fogDensityCurve.Evaluate(darkness);
        }
    }

    void ResolveSunLight()
    {
        if (sunLight != null) return;
        Light primary = null;
        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type != LightType.Directional) continue;
            if (primary == null) { primary = l; continue; }
            if (disableDuplicateSunLights) l.enabled = false;
        }
        sunLight = primary;
    }
}
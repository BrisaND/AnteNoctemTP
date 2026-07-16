using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSlider : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra tu MainMixer aqui")]
    public AudioMixer mainMixer;

    [Tooltip("El nombre exacto del parametro expuesto (ej: MusicVol o SFXVol)")]
    public string exposedParameterName;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();

        // Configuramos el Slider de 0.0001 a 1 para evitar problemas logaritmicos con el 0 absoluto
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;

        // Recuperamos el valor actual del mixer para sincronizar el slider al iniciar el juego
        if (mainMixer.GetFloat(exposedParameterName, out float currentDb))
        {
            // Operacion inversa para convertir Decibelios a valor lineal 0-1
            slider.value = Mathf.Pow(10f, currentDb / 20f);
        }

        // Nos suscribimos al evento cuando el jugador mueve la barrita
        slider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        // Formula matematica para convertir el valor lineal (0 a 1) a logaritmico (Decibelios: -80dB a 0dB)
        float dbValue = Mathf.Log10(value) * 20f;

        mainMixer.SetFloat(exposedParameterName, dbValue);
    }
}
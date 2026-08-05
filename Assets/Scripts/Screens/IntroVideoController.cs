using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class IntroVideoController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private VideoPlayer _videoPlayer;

    [Header("Configuración de Carga")]
    [Tooltip("ESCRIBE AQUÍ EL NOMBRE DE TU NIVEL (Ejemplo: Nivel1, MenuPrincipal)")]
    [SerializeField] private string _targetSceneName;

    [Tooltip("¿Permitir al jugador saltar el video con cualquier tecla?")]
    [SerializeField] private bool _canSkip = true;

    private bool _hasTransitioned = false;

    void Start()
    {
        if (_videoPlayer == null)
        {
            _videoPlayer = GetComponent<VideoPlayer>();
        }

        _videoPlayer.loopPointReached += OnVideoEnd;
    }

    void Update()
    {
        if (_canSkip && !_hasTransitioned)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                GoToLoadingScreen();
            }
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        GoToLoadingScreen();
    }

    private void GoToLoadingScreen()
    {
        if (_hasTransitioned) return;
        _hasTransitioned = true;

        _videoPlayer.loopPointReached -= OnVideoEnd;

        // Le pasamos a la pantalla de carga el nombre del NIVEL FINAL
        LoadingScreen.LoadingScreenAsync(_targetSceneName);
    }
}
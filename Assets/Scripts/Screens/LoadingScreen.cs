//TPFinal - Joaquin Campana

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI Elementos")]
    [SerializeField] TextMeshProUGUI _progressText;
    [SerializeField] GameObject _pressAnyKey;

    // Nombre estático de la escena destino a la que queremos ir
    static string _targetSceneName;

    void Start()
    {
        // Al iniciar la escena "Loading", empieza a cargar la escena destino
        if (!string.IsNullOrEmpty(_targetSceneName))
        {
            StartCoroutine(LoadSceneAsync(_targetSceneName));
        }
    }

    /// <summary>
    /// Guarda el nombre del nivel final y cambia primero a la escena "Loading".
    /// </summary>
    public static void LoadingScreenAsync(string targetLevel)
    {
        _targetSceneName = targetLevel;
        SceneManager.LoadSceneAsync("Loading"); // <--- Debe decir estrictamente "Loading"
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        yield return new WaitForSeconds(0.5f);

        // Inicia la carga asíncrona del nivel destino
        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            int percentage = Mathf.RoundToInt(progress * 100f);

            if (_progressText != null)
            {
                _progressText.text = percentage + "%";
            }

            if (progress >= 1f)
            {
                yield return new WaitForSeconds(0.5f);

                if (_pressAnyKey != null) _pressAnyKey.SetActive(true);

                bool _anyKey = false;
                while (!_anyKey)
                {
                    if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                    {
                        _anyKey = true;
                    }
                    yield return null;
                }

                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
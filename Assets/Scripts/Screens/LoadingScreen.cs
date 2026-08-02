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

    static string _nameLoadScene;

    void Start()
    {
        StartCoroutine(LoadSceneAsync(_nameLoadScene));
    }

    public static void LoadingScreenAsync(string lvl)
    {
        _nameLoadScene = lvl;
        SceneManager.LoadSceneAsync("Loading");
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        yield return new WaitForSeconds(0.5f);
        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            int percentage = Mathf.RoundToInt(progress * 100f);

            // Actualizamos el texto en pantalla (Ej: "85%")
            if (_progressText != null)
            {
                _progressText.text = percentage + "%";
            }

            if (progress >= 1f)
            {
                yield return new WaitForSeconds(0.5f);

                if (_pressAnyKey != null) _pressAnyKey.SetActive(true);

                bool _anyKey = false;
                while (_anyKey == false)
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
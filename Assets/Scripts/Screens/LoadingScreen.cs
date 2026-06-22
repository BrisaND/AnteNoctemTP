using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] Image _progressBar;
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
            float progress = operation.progress / 0.9f;
            _progressBar.fillAmount = progress;

            if (progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.5f);
                _pressAnyKey.SetActive(true);
                bool _anyKey = false;
                while (_anyKey == false)
                {
                    if (Keyboard.current.anyKey.wasPressedThisFrame)
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

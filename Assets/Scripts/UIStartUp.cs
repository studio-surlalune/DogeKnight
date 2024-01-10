using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// This script is attached to all scenes and handle loading the UI if necessary,
/// or loading the first game level.
public class UIStartUp : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Normal game boot up.
        if (SceneManager.sceneCount == 1 && SceneManager.GetSceneByName("UI").isLoaded)
        {
            MenuSystem.s_Instance.SetScreenFaded(true);

            // Only UI scene was loaded and nothing else, so load start screen too.
            StartCoroutine(LoadLevelCoroutine("L0-StartScreen", false));
            MenuSystem.s_Instance.DoMenuTransition(MenuSystem.MenuIndex.Title);
        }
        else
        {
            // Load UI if not present (when we are launching specific levels for debugging).
            if (!SceneManager.GetSceneByName("UI").isLoaded)
                StartCoroutine(LoadLevelCoroutine("UI", true));
            

        }
    }

    private IEnumerator LoadLevelCoroutine(string sceneName, bool isUI)
    {
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Wait until the asynchronous scene fully loads
        while (!asyncOp.isDone)
            yield return null; // continue execution after Update phase

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene != null && !isUI)
        {
            SceneManager.SetActiveScene(loadedScene);
            MenuSystem.s_Instance.BeginScreenFadeIn();
        }
        else if (loadedScene != null && isUI)
        {
            yield return null; // continue execution after Update phase
            // Give a chance to the UI to initialize.
            MenuSystem.s_Instance.DoMenuTransition(MenuSystem.MenuIndex.InGame);
        }
    }
}

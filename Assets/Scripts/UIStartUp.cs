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
        // Handle case when we are launching specific levels for debugging.
        if (!SceneManager.GetSceneByName("UI").isLoaded)
        {
            // Load UI if not present.
            SceneManager.LoadScene("UI", LoadSceneMode.Additive);

            // Special case for L0-StartScreen, set the UI to title screen (for debugging).
            if (SceneManager.GetSceneByName("L0-StartScreen").isLoaded)
                Menus.s_Instance.DoMenuTransition(Menus.MenuState.Title);
        }
        else if (SceneManager.sceneCount == 1)
        {
            // Only UI scene was loaded and nothing else, so load start screen too.
            StartCoroutine(LoadLevelCoroutine("L0-StartScreen"));
            if (Menus.s_Instance) // it will be null if UI scene did not load
                Menus.s_Instance.DoMenuTransition(Menus.MenuState.Title);
        }
    }

    private IEnumerator LoadLevelCoroutine(string sceneName)
    {
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Wait until the asynchronous scene fully loads
        while (!asyncOp.isDone)
            yield return null; // continue execution after Update phase

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene != null)
            SceneManager.SetActiveScene(loadedScene);
    }
}

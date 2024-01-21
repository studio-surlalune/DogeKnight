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
            MenuSystem.SetScreenFaded(true);

            // Only UI scene was loaded and nothing else, so load start screen too.
            StartCoroutine(LoadLevelCoroutine("L0-StartScreen", false));
            MenuSystem.DoMenuTransition(MenuSystem.MenuIndex.Title);
        }
        else if (SceneManager.sceneCount == 1) // level development/debugging
        {
            // Create a new game instance for debugging.
            Game.NewGame();

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
            MenuSystem.BeginScreenFadeIn();
        }
        else if (loadedScene != null && isUI)
        {
            // Give a chance to the UI to initialize.
            yield return null; // continue execution after Update phase
            
            while (MenuSystem.IsScreenFading())
                yield return null;

            MenuSystem.DoMenuTransition(MenuSystem.MenuIndex.InGame);
        }
    }
}

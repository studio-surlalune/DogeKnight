using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

/// This script is attached to all scenes and handle loading the UI if necessary,
/// or loading the first game level.
public class UIStartUp : MonoBehaviour
{
    void Awake()
    {
        // Normal game boot up.
        if (SceneManager.sceneCount == 1 && IsUISceneLoaded())
        {
            StartCoroutine(NormalBootupCoroutine());
        }
        // Development/debugging scene boot up.
        else if (SceneManager.sceneCount == 1)
        {
            // Create a new game instance for debugging.
            Game.NewGame();

            // Load UI if not present (when we are launching specific levels for debugging).
            if (!IsUISceneLoaded())
                SceneManager.LoadScene("UI", LoadSceneMode.Additive);

            StartCoroutine(TransitionToInGameScreenNextFrame());
        }
    }

    private static bool IsUISceneLoaded()
    {
        // When calling during Awake() on the UI scene, isLoaded may actually be false!
        return SceneManager.GetSceneByName("UI").isLoaded || SceneManager.GetActiveScene().name == "UI";
    }

    private IEnumerator NormalBootupCoroutine()
    {
        // Must wait a frame to let the MenuSystem initialize.
        yield return null;

        string startScreenScene = "L0-StartScreen";
        MenuSystem.SetScreenFaded(true);

        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(startScreenScene, LoadSceneMode.Additive);

        // Wait until the asynchronous scene fully loads
        while (!asyncOp.isDone)
            yield return null; // continue execution after Update phase

        Scene loadedScene = SceneManager.GetSceneByName(startScreenScene);
        if (loadedScene != null)
        {
            SceneManager.SetActiveScene(loadedScene);
            MenuSystem.BeginScreenFadeIn();

            while (MenuSystem.IsScreenFading())
                yield return null; 
            
            MenuSystem.DoMenuTransition(MenuSystem.MenuIndex.Title);
        }
    }

    private IEnumerator TransitionToInGameScreenNextFrame()
    {
        // Must wait a frame to let the MenuSystem initialize.
        yield return null;
        
        MenuSystem.DoMenuTransition(MenuSystem.MenuIndex.InGame);
    }
}

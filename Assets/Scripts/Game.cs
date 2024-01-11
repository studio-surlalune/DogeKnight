using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Game : MonoBehaviour
{
    private static Game s_Instance;
    private static bool isPaused;
    private static float pauseAnimTime;

    // Start is called before the first frame update
    void Awake()
    {
        Assert.IsTrue(s_Instance == null);
        s_Instance = this;

        // This will allow the game component to persist between scenes
        // so we can continue receiving Update() calls.
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGame();
    }

    public static void UpdateGame()
    {
        UpdatePauseAnimation();
    }

    public static void TransitionPause(bool pause)
    {
        if (pause == isPaused)
            return;

        isPaused = pause;
        if (pause)
        {
            Time.timeScale = 0f;
            pauseAnimTime = 0;
        }
    }

    private static void UpdatePauseAnimation()
    {
        if (!isPaused)
        {
            if (pauseAnimTime < 1f)
            {
                pauseAnimTime += Time.unscaledDeltaTime;
                // Animate time flowing back to normal speed.
                float s = Mathf.Min(pauseAnimTime / 0.36f, 1f);
                s = s * s;
                Time.timeScale = s;
            }
        }
    }
}

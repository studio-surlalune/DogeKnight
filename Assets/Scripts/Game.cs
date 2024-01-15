using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Creature
{
    public int hp;
    public int mp;
    public int atk;
    public int def;
    public int spd;
    public int luck;
    public int exp;
    public int level;
    public int hpMax;
    public int mpMax;
}

public class DogeKnight : Creature
{
    public GameObject gameObject;

    public DogeKnight()
    {
        hp = 100;
        mp = 100;
        atk = 10;
        def = 10;
        spd = 10;
        luck = 10;
        exp = 0;
        level = 1;
        hpMax = 100;
        mpMax = 100;
    }
}

public class Game : MonoBehaviour
{
    public static DogeKnight dogeKnight;

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

    /// Create a new game instance.
    public static void NewGame()
    {
    }

    /// Set the game to pause/unpause state (with animation).
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

    public static void RegisterDogeKnightProxy(GameObject obj)
    {
        if (dogeKnight == null)
            dogeKnight = new DogeKnight();
        
        dogeKnight.gameObject = obj;
    }

    /// Callback for game update.
    private static void UpdateGame()
    {
        UpdatePauseAnimation();
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

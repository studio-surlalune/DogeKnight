using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Game : MonoBehaviour
{
    public static List<Creature> creatures = new List<Creature>();

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
        creatures.Clear();
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

    public static void RegisterCreatureProxy(Creature.Type creatureType, bool isNPC, GameObject obj)
    {
        Creature creature;
        if (creatureType == Creature.Type.DogeKnight)
            creature = new DogeKnight(creatureType, isNPC, obj);
        else if (creatureType == Creature.Type.Slime)
            creature = new Slime(creatureType, isNPC, obj);
        else
            creature = new Creature(creatureType, isNPC, obj);
        
        creatures.Add(creature);
    }

    public static DogeKnight FindDogeKnight()
    {
        foreach (Creature creature in creatures)
            if (creature.type == Creature.Type.DogeKnight)
                return creature as DogeKnight;

        return null;
    }

    /// Callback for game update.
    private static void UpdateGame()
    {
        UpdatePauseAnimation();

        if (isPaused)
            return;
        
        UpdateCreatures();
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

    private static void UpdateCreatures()
    {
        foreach (Creature creature in creatures)
            creature.Update(creatures);
    }

    private static void UpdateCreature(Creature creature)
    {
        Vector3 posWS = creature.gameObject.transform.position;

        // Test again world limits.
        if (posWS.y < -1f)
        {
            if (creature.stats.hp > 0)
            {
                creature.stats.hp = 0;
                creature.animator.SetTrigger("TriggerFatalHit");
            }

        }
        if (creature.isNPC)
        {}

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// This is instantiated once by the UI scene by the GameCallback gameobject in that scene.
// The Game MonoBahaviour instance is only needed in order for the Game singleton to receive
// Update() and LateUpdate() callbacks.
public class Game : MonoBehaviour
{
    public static List<Creature> creatures;

    private static Game s_Instance;
    private static bool isPaused;
    private static float pauseAnimTime;

    // Game script awake must have higher priority than Creature Awake() so we can register creatures.
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

    void LateUpdate()
    {
        LateUpdateGame();
    }

    /// Create a new game instance.
    public static void NewGame()
    {
        creatures = new List<Creature>();
    }

    public static void EndGame()
    {
        creatures.Clear();
        creatures = null;
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

    public static Creature RegisterCreatureProxy(Creature.Type creatureType, bool isPlayer, GameObject obj)
    {
        Creature creature;
        if (creatureType == Creature.Type.DogeKnight)
            creature = new DogeKnight(creatureType, isPlayer, obj);
        else if (creatureType == Creature.Type.Slime)
            creature = new Slime(creatureType, isPlayer, obj);
        else
            creature = new Creature(creatureType, isPlayer, obj);
        
        creatures.Add(creature);
        return creature;
    }

    public static Creature FindCreatureByProxy(GameObject gameObject)
    {
        foreach (Creature creature in creatures)
            if (creature.gameObject == gameObject)
                return creature;

        return null;
    }

    public static DogeKnight FindPlayer()
    {
        foreach (Creature creature in creatures)
            if (creature.type == Creature.Type.DogeKnight && !creature.isNPC)
                return creature as DogeKnight;

        return null;
    }

    /// Callback for game update.
    private static void UpdateGame()
    {
        UpdatePauseAnimation();

        if (isPaused)
            return;
        
        // NewGame() has not been called.
        if (creatures == null)
            return;
        
        foreach (Creature creature in creatures)
            creature.Update(creatures);
    }

    private static void LateUpdateGame()
    {
        if (isPaused)
            return;
        
        // NewGame() has not been called.
        if (creatures == null)
            return;
        
        foreach (Creature creature in creatures)
            creature.LateUpdate(creatures);
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

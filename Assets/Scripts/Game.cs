using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public struct CreatureStats
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

    public static CreatureStats Instanciate(Creature.Type type)
    {
        switch(type)
        {
            case Creature.Type.DogeKnight:  return new CreatureStats { hp=100, mp=100, atk=10, def=10, spd=10, luck=10, exp=0, level=1, hpMax=100, mpMax=100 };
            case Creature.Type.Slime:       return new CreatureStats { hp=10, mp=0, atk=2, def=2, spd=4, luck=10, exp=0, level=1, hpMax=10, mpMax=0 };
            case Creature.Type.Turtle:      return new CreatureStats { hp=15, mp=0, atk=2, def=4, spd=2, luck=10, exp=0, level=1, hpMax=15, mpMax=0 };
            case Creature.Type.Skeleton:    return new CreatureStats { hp=15, mp=0, atk=5, def=2, spd=4, luck=10, exp=0, level=1, hpMax=10, mpMax=0 };
            default:
                Assert.IsTrue(false);
                return new CreatureStats();
        }
    }
}

public class Creature
{
    public enum Type
    {
        DogeKnight,
        Slime,
        Turtle,
        Skeleton,
    }

    public Creature(Type type, GameObject gameObject)
    {
        stats = CreatureStats.Instanciate(type);
        this.gameObject = gameObject;
        this.type = type;
    }

    public CreatureStats stats;

    // Unity game object.
    public GameObject gameObject;

    public Type type;
}

public class DogeKnight : Creature
{
    public DogeKnight(Type type, GameObject gameObject) : base(type, gameObject)
    {}
}

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

    public static void RegisterCreatureProxy(Creature.Type creatureType, GameObject obj)
    {
        Creature creature;
        if (creatureType == Creature.Type.DogeKnight)
            creature = new DogeKnight(creatureType, obj);
        else
            creature = new Creature(creatureType, obj);
        
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
        {
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public struct CreatureEvent
{
    public enum Type
    {
        Hit,
    }

    public Creature source;
    public Creature target;
    public Type type;
    public int value;

    public CreatureEvent(Creature source, Creature target, Type type, int value)
    {
        this.source = source;
        this.target = target;
        this.type = type;
        this.value = value;
    }
}

public struct CreatureAction
{
    public enum Type
    {
        Attack,
    }

    public Creature source;
    public float startTime;
    public float endTime;
    public Type type;
    public int value;
    public List<Creature> records;
}

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
            case Creature.Type.DogeKnight:  return new CreatureStats { hp=100, mp=10, atk=10, def=10, spd=20, luck=10, exp=0, level=1, hpMax=100, mpMax=10 };
            case Creature.Type.Slime:       return new CreatureStats { hp= 30, mp= 0, atk= 2, def= 2, spd= 8, luck=10, exp=0, level=1, hpMax= 10, mpMax= 0 };
            case Creature.Type.Turtle:      return new CreatureStats { hp= 60, mp= 0, atk= 2, def= 4, spd= 4, luck=10, exp=0, level=1, hpMax= 15, mpMax= 0 };
            case Creature.Type.Skeleton:    return new CreatureStats { hp= 50, mp= 0, atk= 5, def= 2, spd=12, luck=10, exp=0, level=1, hpMax= 10, mpMax= 0 };
            default:
                Assert.IsTrue(false);
                return new CreatureStats();
        }
    }
}

public class Creature
{
    /// Speed factor for creature movement.
    /// A speed of 10 means double-speed.
    public const float kSpeedFactorInv = 1/10f;
    public enum Type
    {
        DogeKnight,
        Slime,
        Turtle,
        Skeleton,
    }

    // Unity game object.
    public GameObject gameObject;
    // Shortcut for gameObject.transform.
    public Transform transform;
    // Shortcut for gameObject.GetComponent<Rigidbody>().
    public Rigidbody rigidbody;
    // Shortcut for gameObject.GetComponent<Animator>().
    public Animator animator;
    // Shortcut for gameObject.GetComponentInChildren<Renderer>().material.
    public Material material;

    public CreatureStats stats;
    public Type type;
    public bool isPlayer;
    public bool isNPC { get { return !isPlayer; } set { isPlayer = !value; } }

    /// <summary>
    /// List of actions in progress.
    /// They are usually physics-related.
    /// </summary>
    public List<CreatureAction> actions;
    /// </summary>
    /// List of events that the creature received during the Update phase
    /// and must deal with during the LateUpdate phase.
    /// </summary>
    public List<CreatureEvent> receivedEvents;

    public Creature(Type type, bool isPlayer, GameObject gameObject)
    {
        this.gameObject = gameObject;
        // Caching objects for faster access.
        this.transform = gameObject.transform;
        this.rigidbody = gameObject.GetComponent<Rigidbody>();
        this.animator = gameObject.GetComponent<Animator>();
        // Get the Renderer component from this GameObject or one of its children
        Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
        // Calling renderer.material create a unique material instance for the gameObject
        // if it didn't already exist.
        material = renderer.material;

        stats = CreatureStats.Instanciate(type);
        this.type = type;
        this.isPlayer = isPlayer;

        actions = new List<CreatureAction>();
        receivedEvents = new List<CreatureEvent>();
    }

    /// <summary>
    /// Update creature own actions.
    /// </summary>
    /// <param name="creatures"></param>
    public virtual void Update(List<Creature> creatures)
    {}

    /// <summary>
    /// Update creature reactions from the environment.
    /// </summary>
    /// <param name="creatures"></param>
    public virtual void LateUpdate(List<Creature> creatures)
    {}

    /// <summary>
    /// Called by physics callback event when we detect a creature collide with another creature.
    /// </summary>
    /// <param name="gameObject">the object which received the collision event.
    /// It may be the gameObject linked to the creature or a child object from the creature.
    /// For instance, the sword attached to the DogeKnight creature.</param>
    /// <param name="other">the creature that was hit.</param>
    public virtual void RegisterCollision(GameObject otherObject, Creature other)
    {}

    internal static Creature FindClosestCreature(Creature self, List<Creature> creatures, bool isPlayer, bool isAlive, out float dist)
    {
        Creature closestCreature = null;
        float closestDistance = float.MaxValue;

        foreach (Creature creature in creatures)
        {
            if (creature == self || creature.isPlayer != isPlayer)
                continue;
            
            if (isAlive != (creature.stats.hp > 0))
                continue;

            float distance = Vector3.Distance(self.transform.position, creature.transform.position);
            if (distance < closestDistance)
            {
                closestCreature = creature;
                closestDistance = distance;
            }
        }

        dist = closestDistance;
        return closestCreature;
    }

    internal static void TrimActions(List<CreatureAction> actions, float time)
    {
        for (int i = 0; i < actions.Count; ++i)
        {
            CreatureAction action = actions[i];
            if (time >= action.endTime)
            {
                actions.RemoveAt(i);
                --i;
            }
        }
    }

    internal static Transform FindRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
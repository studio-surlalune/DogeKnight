using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
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
            case Creature.Type.DogeKnight:  return new CreatureStats { hp=100, mp=10, atk=10, def=10, spd=10, luck=10, exp=0, level=1, hpMax=100, mpMax=10 };
            case Creature.Type.Slime:       return new CreatureStats { hp= 10, mp= 0, atk= 2, def= 2, spd= 4, luck=10, exp=0, level=1, hpMax= 10, mpMax= 0 };
            case Creature.Type.Turtle:      return new CreatureStats { hp= 15, mp= 0, atk= 2, def= 4, spd= 2, luck=10, exp=0, level=1, hpMax= 15, mpMax= 0 };
            case Creature.Type.Skeleton:    return new CreatureStats { hp= 15, mp= 0, atk= 5, def= 2, spd= 4, luck=10, exp=0, level=1, hpMax= 10, mpMax= 0 };
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

    public CreatureStats stats;

    // Unity game object.
    public GameObject gameObject;
    public Animator animator;

    public Type type;
    public bool isNPC;

    public Creature(Type type, bool isNPC, GameObject gameObject)
    {
        stats = CreatureStats.Instanciate(type);
        this.gameObject = gameObject;
        // Caching objects for faster access.
        this.animator = gameObject.GetComponent<Animator>();
        this.type = type;
        this.isNPC = isNPC;
    }

    // make Update method an abstract method
    public virtual void Update(List<Creature> creatures)
    {}

    public static Creature FindClosestCreature(Creature self, List<Creature> creatures, bool isNPC, out float dist)
    {
        Transform selfTransform = self.gameObject.transform;
        Creature closestCreature = null;
        float closestDistance = float.MaxValue;

        foreach (Creature creature in creatures)
        {
            if (creature == self || creature.isNPC != isNPC)
                continue;

            float distance = Vector3.Distance(selfTransform.position, creature.gameObject.transform.position);
            if (distance < closestDistance)
            {
                closestCreature = creature;
                closestDistance = distance;
            }
        }

        dist = closestDistance;
        return closestCreature;

    }
}

public class DogeKnight : Creature
{
    public DogeKnight(Type type, bool isNPC, GameObject gameObject) : base(type, isNPC, gameObject)
    {}

    public override void Update(List<Creature> creatures)
    {}

}


public class Slime : Creature
{
    public Slime(Type type, bool isNPC, GameObject gameObject) : base(type, isNPC, gameObject)
    {}

    public override void Update(List<Creature> creatures)
    {
        const float kAwarenessDistance = 8f;
        const float kChargeDistance = 5f;
        const float kAttackDistance = 1.5f;

        // Find the closest creature.
        float closestDistance;
        Creature closestCreature = Creature.FindClosestCreature(this, creatures, false, out closestDistance);

        // Do nothing if there is no playable character nearby.
        if (closestDistance > kAwarenessDistance)
            return;

        // Abnormal condition: do nothing to avoid null maths?        
        if (closestDistance < 0.01f)
            return;

        float deltaTime = Time.deltaTime;
        float motionSpeed = 1f + stats.spd * kSpeedFactorInv;
        float rotationSpeed = 10.0f;
        Transform transform = this.gameObject.transform;
        Vector3 translationWS = Vector3.zero;
        Quaternion rotation = transform.rotation;

        Vector3 directionWS = closestCreature.gameObject.transform.position - transform.position;
        directionWS.y = 0;
        directionWS.Normalize();

        // Move towards the closest creature.
        if (closestDistance > kAttackDistance && closestDistance <= kChargeDistance)
            translationWS = directionWS * motionSpeed * deltaTime;

        if (closestDistance <= kAwarenessDistance)
        {
            Quaternion newRotation = Quaternion.LookRotation(directionWS, Vector3.up);
            rotation = Quaternion.Lerp(rotation, newRotation, rotationSpeed * deltaTime);
        }

        if (closestDistance <= kAttackDistance)
            animator.SetTrigger("Attack");
        
        // Apply movements.
        transform.Translate(translationWS, Space.World);
        transform.rotation = rotation;

        // Apply animations.
        animator.SetBool("EnemyNearby", closestDistance <= kAwarenessDistance);
        animator.SetBool("IsWalking", closestDistance > kAttackDistance && closestDistance <= kChargeDistance);
    }
}
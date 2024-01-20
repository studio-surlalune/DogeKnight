using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Assertions;

public struct CreatureEvent
{
    public enum Type
    {
        Attack,
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
            case Creature.Type.Slime:       return new CreatureStats { hp= 10, mp= 0, atk= 2, def= 2, spd= 8, luck=10, exp=0, level=1, hpMax= 10, mpMax= 0 };
            case Creature.Type.Turtle:      return new CreatureStats { hp= 15, mp= 0, atk= 2, def= 4, spd= 4, luck=10, exp=0, level=1, hpMax= 15, mpMax= 0 };
            case Creature.Type.Skeleton:    return new CreatureStats { hp= 15, mp= 0, atk= 5, def= 2, spd=12, luck=10, exp=0, level=1, hpMax= 10, mpMax= 0 };
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

    /// </summary>
    /// List of events that the creature received during the Update phase
    /// and must deal with during the LateUpdate phase.
    /// </summary>
    public List<CreatureEvent> receivedEvents;
    

    public Creature(Type type, bool isNPC, GameObject gameObject)
    {
        stats = CreatureStats.Instanciate(type);
        this.gameObject = gameObject;
        // Caching objects for faster access.
        this.animator = gameObject.GetComponent<Animator>();
        this.type = type;
        this.isNPC = isNPC;

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

    public override void LateUpdate(List<Creature> creatures)
    {
        foreach (CreatureEvent ev in receivedEvents)
        {
            if (ev.type == CreatureEvent.Type.Attack)
            {
                stats.hp -= ev.value;
                if (stats.hp <= 0)
                {
                    stats.hp = 0;
                    animator.SetTrigger("TriggerFatalHit");
                }
                else
                {
                    animator.SetTrigger("TriggerHit");
                }
            }
        }
        receivedEvents.Clear();
    }

}


public class Slime : Creature
{
    private float attackTriggerCooldown;
    private int attackConsecutiveCount;

    public Slime(Type type, bool isNPC, GameObject gameObject) : base(type, isNPC, gameObject)
    {}

    public override void Update(List<Creature> creatures)
    {
        const float kAwarenessDistance = 8f;
        const float kChargeDistance = 5f;
        const float kAttackDistance = 1.9f;

        // Find thssaaae closest creature.
        float closestDistance;
        Creature closestCreature = Creature.FindClosestCreature(this, creatures, false, out closestDistance);

        // Do nothing if there is no playable character nearby.
        if (closestDistance > kAwarenessDistance)
            return;

        // Abnormal condition: do nothing to avoid incorrect maths?        
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
        if (closestDistance > kAttackDistance * 0.5f && closestDistance <= kChargeDistance)
            translationWS = directionWS * motionSpeed * deltaTime;

        if (closestDistance <= kAwarenessDistance)
        {
            Quaternion newRotation = Quaternion.LookRotation(directionWS, Vector3.up);
            rotation = Quaternion.Lerp(rotation, newRotation, rotationSpeed * deltaTime);
        }

        // Update trigger status.
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool attackInProgress = stateInfo.IsName("Attack01") || stateInfo.IsName("Attack02");
        if (attackTriggerCooldown > 0f)
        {
            attackTriggerCooldown -= deltaTime;
            if (attackInProgress)
                attackTriggerCooldown = 0f;
        }

        // Decide new trigger status.            
        int doAttack = 0;
        if (closestDistance <= kAttackDistance)
        {
            if (attackTriggerCooldown <= 0f && !attackInProgress)
            {
                attackTriggerCooldown = 1f;
                ++attackConsecutiveCount;
                doAttack = attackConsecutiveCount < 3 ? 1 : 2;
                attackConsecutiveCount %= 3;

                int attackDamage = doAttack == 1 ? stats.atk : stats.atk*2;
                closestCreature.receivedEvents.Add(
                    new CreatureEvent(this, closestCreature, CreatureEvent.Type.Attack, attackDamage)
                );
            }
        }
        else
        {
            attackTriggerCooldown = 1f;
            attackConsecutiveCount = 0;
        }

        // Apply movements.
        transform.Translate(translationWS, Space.World);
        transform.rotation = rotation;

        if (doAttack == 1)
            animator.SetTrigger("Attack0");
        else if (doAttack == 2)
            animator.SetTrigger("Attack1");
        
        // Apply animations.
        animator.SetBool("EnemyNearby", closestDistance <= kAwarenessDistance);
        animator.SetBool("IsWalking", closestDistance > kAttackDistance && closestDistance <= kChargeDistance);
    }

    public override void LateUpdate(List<Creature> creatures)
    {
        receivedEvents.Clear();
    }
}
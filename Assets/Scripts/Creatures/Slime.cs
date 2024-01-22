using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Slime : Creature
{
    private float attackTriggerCooldown;
    private int attackConsecutiveCount;

    public Slime(Type type, bool isPlayer, GameObject gameObject) : base(type, isPlayer, gameObject)
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
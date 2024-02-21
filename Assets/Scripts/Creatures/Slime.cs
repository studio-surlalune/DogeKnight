using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Slime : Creature
{
    private static readonly Color kCharHitColor = new Color(0f, 0.5f, 0.85f, 1f);
    private static readonly float kCharHitDuration = 1.25f;
    // Hit animation for the character.
    private float charHitAnimTime = -1f;
    private float attackTriggerCooldown;
    private int attackConsecutiveCount;

    public Slime(Type type, bool isPlayer, GameObject gameObject) : base(type, isPlayer, gameObject)
    {
        charHitAnimTime = -1f;
    }

    public override void Update(List<Creature> creatures)
    {
        // Process pending actions.
        TrimActions(actions, Time.time);

        if (charHitAnimTime >= 0f)
            charHitAnimTime += Time.deltaTime;

        ApplyHitColors();

        UpdateMotion(creatures);

        if (stats.hp <= 0)
        {
            // Adjust the box collider when the slime is dead.
            // When dead, the skime is deflated and the default collider is too big.
            BoxCollider[] boxColliders = gameObject.GetComponentsInChildren<BoxCollider>();
            BoxCollider mainCollider = null;
            foreach (BoxCollider boxCollider in boxColliders)
            {
                if (boxCollider.isTrigger)
                    continue;
                mainCollider = boxCollider;
                break;
            }

            if (mainCollider != null)
            {
                Vector3 size = mainCollider.size;
                Vector3 center = mainCollider.center;
                size.x = 0.4f;
                center.x = -0.16f;
                mainCollider.center = center;
                mainCollider.size = size;
            }

            rigidbody.constraints &= RigidbodyConstraints.FreezeRotation;
        }
    }

    public override void LateUpdate(List<Creature> creatures)
    {
        foreach (CreatureEvent ev in receivedEvents)
        {
            if (ev.type == CreatureEvent.Type.Hit)
            {
                stats.hp -= ev.value;

                /*
                // If several attacks, just keep the strongest one for push-back animation.
                if (ev.value * kForceFactor > pushBackForce)
                {
                    pushBackForce = 3f + ev.value * kForceFactor;
                    pushBackDirection = Vector3.Normalize(transform.position - ev.source.transform.position);
                    // Make it go upward a little bit to prevent friction.
                    if (pushBackDirection.y < 0.33f)
                    {
                        pushBackDirection.y += 0.33f;
                        pushBackDirection = Vector3.Normalize(pushBackDirection);
                    }
                }
                */

                if (stats.hp <= 0)
                {
                    stats.hp = 0;
                    animator.SetTrigger("TriggerFatalHit");
                }
                else
                {
                    animator.SetTrigger("TriggerHit");
                    charHitAnimTime = 0f;
                    ApplyHitColors();
                }
            }
        }

        receivedEvents.Clear();
    }

    public override void RegisterCollision(GameObject otherObject, Creature otherCreature)
    {
        // Find if an attack action is in progress, and generate a hit event on the creature if that's the case.
        for (int i = 0; i < actions.Count; ++i)
        {
            CreatureAction action = actions[i];
            if (action.type != CreatureAction.Type.Attack)
                continue;

            if (action.records.Contains(otherCreature))
                continue;

            action.records.Add(otherCreature);
            otherCreature.receivedEvents.Add(
                new CreatureEvent(this, otherCreature, CreatureEvent.Type.Hit, action.value)
            );
            break;
        }
    }


    private void ApplyHitColors()
    {
        // Color.Lerp() is implicitly clamped between 0 and 1.
        if (charHitAnimTime >= 0f && charHitAnimTime < kCharHitDuration)
        {
            float s = charHitAnimTime / kCharHitDuration;
            material.color = Color.Lerp(kCharHitColor, Color.white, s);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", kCharHitColor * 0.4f * (1f - s));
        }
        else
        {
            material.color = Color.white;
            material.DisableKeyword("_EMISSION");
        }
    }

    private void UpdateMotion(List<Creature> creatures)
    {
        const float kAwarenessDistance = 8f;
        const float kChargeDistance = 5f;
        const float kAttackDistance = 1.9f;

        // Find the closest player.
        float closestDistance;
        Creature closestCreature = Creature.FindClosestCreature(this, creatures, true, true, out closestDistance);

        // Do nothing if there is no playable character nearby.
        if (closestDistance > kAwarenessDistance)
            return;

        // Abnormal condition: do nothing to avoid NaN maths?        
        if (closestDistance < 0.01f)
            return;

        // The creature is dead, do nothing.
        if (stats.hp <= 0)
            return;

        float time = Time.time;
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
                actions.Add(new CreatureAction {
                    source = this,
                    startTime = time,
                    endTime = time + 0.3f,
                    type = CreatureAction.Type.Attack,
                    value = attackDamage,
                    records = new List<Creature>(),
                });
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

        // Apply animations.
        if (doAttack == 1)
            animator.SetTrigger("Attack0");
        else if (doAttack == 2)
            animator.SetTrigger("Attack1");
        animator.SetBool("EnemyNearby", closestDistance <= kAwarenessDistance);
        animator.SetBool("IsWalking", closestDistance > kAttackDistance && closestDistance <= kChargeDistance);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class DogeKnight : Creature
{
    private static readonly Color kHitColor = new Color(1f, 0.5f, 0.5f, 1f);
    private static readonly float kHitDuration = 0.25f;
    private float hitAnimTime;

    public DogeKnight(Type type, bool isPlayer, GameObject gameObject) : base(type, isPlayer, gameObject)
    {
        hitAnimTime = -1f;
    }

    public override void Update(List<Creature> creatures)
    {
        if (hitAnimTime >= 0f)
        {
            hitAnimTime += Time.deltaTime;
            SetHitColor();
            if (hitAnimTime >= kHitDuration)
                hitAnimTime = -1f;
        }
    }

    public override void LateUpdate(List<Creature> creatures)
    {
        const float kForceFactor = 10f;
        Vector3 pushBackDirection = Vector3.zero;
        float pushBackForce = 0f;

        foreach (CreatureEvent ev in receivedEvents)
        {
            if (ev.type == CreatureEvent.Type.Attack)
            {
                stats.hp -= ev.value;

                // If several attacks, just keep the strongest one for push-back animation.
                if (ev.value * kForceFactor > pushBackForce)
                {
                    pushBackForce = ev.value * kForceFactor;
                    pushBackDirection = Vector3.Normalize(transform.position - ev.source.transform.position);
                }

                if (stats.hp <= 0)
                {
                    stats.hp = 0;
                    animator.SetTrigger("TriggerFatalHit");

                    MenuSystem.DoMenuTransition(MenuSystem.MenuIndex.GameOver);
                }
                else
                {
                    animator.SetTrigger("TriggerHit");
                    hitAnimTime = 0f;
                    SetHitColor();
                }
            }
        }
        receivedEvents.Clear();

        // Apply push back if any.
        if (pushBackForce > 0f)
        {
            rigidbody.AddForce(pushBackDirection * pushBackForce, ForceMode.Impulse);
        }
    }

    private void SetHitColor()
    {
        material.color = Color.Lerp(kHitColor, Color.white, hitAnimTime / kHitDuration);
    }
}

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
        foreach (CreatureEvent ev in receivedEvents)
        {
            if (ev.type == CreatureEvent.Type.Attack)
            {
                stats.hp -= ev.value;
                if (stats.hp <= 0)
                {
                    stats.hp = 0;
                    animator.SetTrigger("TriggerFatalHit");

                    MenuSystem.DoMenuTransition(MenuSystem.MenuIndex.GameOver);
                    Game.TransitionPause(true);
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
    }

    private void SetHitColor()
    {
        material.color = Color.Lerp(kHitColor, Color.white, hitAnimTime / kHitDuration);
    }
}

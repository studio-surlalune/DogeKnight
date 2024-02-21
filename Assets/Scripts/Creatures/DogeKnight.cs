using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class DogeKnight : Creature
{
    private static readonly Color kCharHitColor = new Color(1f, 0.5f, 0.5f, 1f);
    private static readonly Color kShieldHitColor = new Color(1f, 0.2f, 0.2f, 1f);
    private static readonly float kCharHitDuration = 0.25f;
    private static readonly float kShieldHitDuration = 0.25f;

    internal bool isDefending;
    // Hit animation for the character.
    private float charHitAnimTime = -1f;
    // Hit animation on the character shield.
    private float shieldHitAnimTime = -1f;
    private Material shieldMaterial;

    public DogeKnight(Type type, bool isPlayer, GameObject gameObject) : base(type, isPlayer, gameObject)
    {
        Transform shieldTransform = Creature.FindRecursive(gameObject.transform, "Shield");
        GameObject shield = shieldTransform.gameObject;
        Renderer shieldRenderer = shield.GetComponentInChildren<Renderer>();
        // Calling renderer.material create a unique material instance for the gameObject
        // if it didn't already exist.
        shieldMaterial = shieldRenderer.material;
    }

    public override void Update(List<Creature> creatures)
    {
        TrimActions(actions, Time.time);

        if (charHitAnimTime >= 0f)
            charHitAnimTime += Time.deltaTime;

        if (shieldHitAnimTime >= 0f)
            shieldHitAnimTime += Time.deltaTime;

        ApplyHitColors();

        if (charHitAnimTime >= kCharHitDuration)
            charHitAnimTime = -1f;
        if (shieldHitAnimTime >= kShieldHitDuration)
            shieldHitAnimTime = -1f;
    }

    public override void LateUpdate(List<Creature> creatures)
    {
        const float kForceFactor = 1f;
        Vector3 pushBackDirection = Vector3.zero;
        float pushBackForce = 0f;

        foreach (CreatureEvent ev in receivedEvents)
        {
            if (ev.type == CreatureEvent.Type.Hit)
            {
                // Consider defending is successful only if we are facing the attack.
                bool isDeflecting = isDefending ? CalculateDefenseAngle(ev.source.transform.position) < 45f : false;

                if (!isDeflecting)
                    stats.hp -= ev.value;

                // If several attacks, just keep the strongest one for push-back animation.
                if (ev.value * kForceFactor > pushBackForce)
                {
                    pushBackForce = 3f + ev.value * kForceFactor;
                    pushBackDirection = Vector3.Normalize(transform.position - ev.source.transform.position);
                }

                if (isDeflecting)
                {
                    shieldHitAnimTime = 0f;
                    ApplyHitColors();

                    // TODO: play deflect sound, maybe some sparks?
                }
                else if (stats.hp <= 0)
                {
                    stats.hp = 0;
                    animator.SetTrigger("TriggerFatalHit");

                    MenuSystem.DoMenuTransition(MenuSystem.MenuIndex.GameOver);
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

        // Apply push back if any.
        if (pushBackForce > 0f)
        {
            rigidbody.AddForce(pushBackDirection * pushBackForce, ForceMode.VelocityChange);

            // Attempt to avoid bouncing artifacts.
            float maxVelocity = 0.5f;
            if (rigidbody.velocity.magnitude > maxVelocity)
                rigidbody.velocity = rigidbody.velocity.normalized * maxVelocity;
        }
    }

    public override void RegisterCollision(GameObject otherObject, Creature otherCreature)
    {
        // Find if an attack action is in progress, and generate a hit event on the creature if that's the case.
        for (int i = 0; i < actions.Count; ++i)
        {
            CreatureAction action = actions[i];
            if (action.type != CreatureAction.Type.Attack)
                continue;

            // Prevent the sword from hitting the same creature multiple frames.
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

        if (shieldHitAnimTime >= 0f && shieldHitAnimTime < kShieldHitDuration)
        {
            float s = shieldHitAnimTime / kShieldHitDuration;
            shieldMaterial.color = Color.Lerp(kShieldHitColor, Color.white, s);
            shieldMaterial.EnableKeyword("_EMISSION");
            shieldMaterial.SetColor("_EmissionColor", kShieldHitColor * 0.4f * (1f - s));
        }
        else
        {
            shieldMaterial.color = Color.white;
            shieldMaterial.DisableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// Calculate the angle of attach from the attacker to the defender
    /// </summary>
    /// <param name="posWS"></param>
    /// <returns> angle in degrees</returns>
    private float CalculateDefenseAngle(Vector3 posWS)
    {
        Vector3 hitDir = Vector3.Normalize(posWS - transform.position);
        Vector3 selfDir = transform.forward;
        float cosAngle = Vector3.Dot(hitDir, selfDir);
        return Mathf.Acos(cosAngle) * Mathf.Rad2Deg;
    }
}

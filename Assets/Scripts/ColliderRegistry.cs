using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderRegistry : MonoBehaviour
{
    private static int creatureLayer = -1;
    Creature creature;

    void Awake()
    {
        if (creatureLayer == -1)
            creatureLayer = LayerMask.NameToLayer("Creature");
    }

    void Start()
    {
        // Find parent creature.
        GameObject gameObject = this.gameObject;
        CreatureRegistry creatureRegistry = gameObject.GetComponentInParent<CreatureRegistry>();
        if (creatureRegistry != null)
            creature = creatureRegistry.creature;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != creatureLayer)
            return;
        
        if (creature == null)
            return;

        CreatureRegistry otherRegistry = other.gameObject.GetComponentInParent<CreatureRegistry>();
        if (otherRegistry == null)
            return;
        
        creature.RegisterCollision(otherRegistry.creature);
    }
}

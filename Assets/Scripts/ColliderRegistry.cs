using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ColliderRegistry : MonoBehaviour
{
    private static int creatureLayer = -1;
    Creature creature;

    void Awake()
    {
        if (creatureLayer == -1)
            creatureLayer = LayerMask.NameToLayer("Creature");

        if (Debug.isDebugBuild)
        {
            Collider[] colliders = this.GetComponents<Collider>();
            bool aColliderHasTrigger = false;
            foreach (Collider collider in colliders)
                aColliderHasTrigger |= collider.isTrigger;
            Assert.IsTrue(aColliderHasTrigger, "ColliderRegistry requires at least one trigger collider");
        }
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
        // Only process collisions with creatures layer.
        if (other.gameObject.layer != creatureLayer)
            return;
        
        if (creature == null)
            return;

        CreatureRegistry otherRegistry = other.gameObject.GetComponentInParent<CreatureRegistry>();
        if (otherRegistry == null)
            return;
        
        creature.RegisterCollision(other.gameObject, otherRegistry.creature);
    }
}

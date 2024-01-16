using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureProxy : MonoBehaviour
{
    public Creature.Type creatureType;

    void Start()
    {
        Game.RegisterCreatureProxy(creatureType, this.gameObject);
    }
}

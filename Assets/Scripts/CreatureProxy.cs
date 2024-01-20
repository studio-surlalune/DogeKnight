using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureProxy : MonoBehaviour
{
    public Creature.Type creatureType;
    public bool isNPC = true;

    void Start()
    {
        Game.RegisterCreatureProxy(creatureType, isNPC, this.gameObject);
    }
}

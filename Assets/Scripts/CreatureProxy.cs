using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureProxy : MonoBehaviour
{
    public Creature.Type creatureType;
    public bool isPlayer = false;

    void Start()
    {
        Game.RegisterCreatureProxy(creatureType, isPlayer, this.gameObject);
    }
}

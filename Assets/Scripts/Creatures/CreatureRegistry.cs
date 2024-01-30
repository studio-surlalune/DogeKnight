using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureRegistry : MonoBehaviour
{
    public Creature.Type creatureType;
    public bool isPlayer = false;
    internal Creature creature;

    void Awake()
    {
        creature = Game.RegisterGameObject(creatureType, isPlayer, this.gameObject);
    }
}

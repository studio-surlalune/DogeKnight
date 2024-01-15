using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureProxy : MonoBehaviour
{
    public Creature.Type creatureType;

    void Awake()
    {
        Game.RegisterCreatureProxy(creatureType, this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DogeKnightProxy : MonoBehaviour
{
    public void Awake()
    {
        Game.RegisterDogeKnightProxy(this.gameObject);
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

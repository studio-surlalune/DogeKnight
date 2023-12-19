using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    public GameObject mainCharacter;

    // Start is called before the first frame update
    void Start()
    {
        // Minimum buffering, whatever it may be.
        QualitySettings.maxQueuedFrames = 1;

        GameObject startPoint = GameObject.Find("StartPoint");
        if (startPoint != null)
            mainCharacter.transform.position = startPoint.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }
}

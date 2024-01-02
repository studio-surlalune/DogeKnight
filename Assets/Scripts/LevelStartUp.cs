using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    public GameObject mainCharacter;

    // Start is called before the first frame update
    void Start()
    {
        // Minimum buffering (double-buffering).
        // If possible, disable multithreaded rendering (the render thread)
        // and enable graphics jobs. This is the bast combination to minimize input delay.
        QualitySettings.maxQueuedFrames = 1;

        GameObject startPoint = GameObject.Find("StartPoint");
        if (mainCharacter && startPoint != null)
            mainCharacter.transform.position = startPoint.transform.position;
    }
}

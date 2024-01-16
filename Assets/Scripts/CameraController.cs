using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject trackingTarget;
    private float cameraFocusDistance = 16.0f;
    private float cameraSpeed = 2.5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Camera cam = GetComponent<Camera>();

        // Get camera translation vectors.
        Vector3 rightDirWS = cam.transform.right;
        Vector3 upDirWS = cam.transform.up;
        rightDirWS.y = 0.0f;
        upDirWS.y = 0.0f;
        rightDirWS.Normalize();
        upDirWS.Normalize();

        Vector3 charPosWS = trackingTarget.transform.position;
        Vector3 charFocusPosWS = charPosWS;

        Vector3 camFocusPosWs = cam.transform.position + cam.transform.forward * cameraFocusDistance;

        if ((cam.transform.position - camFocusPosWs).sqrMagnitude > 0.1f)
            cam.transform.position += (charFocusPosWS - camFocusPosWs) * cameraSpeed * Time.deltaTime;
    }
}

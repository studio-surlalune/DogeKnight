using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject trackingTarget;
    public Creature trackingCreature;
    private float cameraFocusDistance = 16.0f;
    private float cameraSpeed = 2.5f;
    // Camera position before shaking.
    private Vector3 cameraPositionWS;

    // Detect when hp decrease and induce camera shake.
    private int hpStored;
    // Strength of camera shake.
    private float shakeFactor;
    // Shake animation time, -1 if no shake in progress.
    private float shakeAnimTime;
    // Used to generate shake motion.
    //Unity.Mathematics.Random random;

    void Start()
    {
        // We may set the tracking target to a child object of the creature,
        // so we climp up the hiearchy to find the "root" creature.
        GameObject obj = trackingTarget;
        do
        {
            trackingCreature = Game.FindCreatureByProxy(obj);
            if (trackingCreature == null)
                obj = obj.transform.parent.gameObject;
        }
        while (obj != null && trackingCreature == null);

        if (trackingCreature != null)
        {
            hpStored = trackingCreature.stats.hp;
            shakeAnimTime = -1f;
            //random = new Unity.Mathematics.Random((uint)253679);

        }

        Camera cam = GetComponent<Camera>();
        cameraPositionWS = cam.transform.position;
    }

    void LateUpdate()
    {
        Camera cam = GetComponent<Camera>();
        float deltaTime = Time.deltaTime;

        if (trackingTarget != null)
            cameraPositionWS = UpdateCameraTracking(cam, cameraPositionWS, deltaTime);
        
        Vector3 camPosWS = cameraPositionWS;

        if (trackingCreature != null)
            camPosWS = UpdateCameraShake(cam, camPosWS, deltaTime);
        
        cam.transform.position = camPosWS;
    }

    private Vector3 UpdateCameraTracking(Camera cam, Vector3 camPosWS, float deltaTime)
    {
        Vector3 forwardWS = cam.transform.forward;
        Vector3 focusPosWS = trackingTarget.transform.position;
        Vector3 camFocusPosWs = camPosWS + forwardWS * cameraFocusDistance;

        if ((camPosWS - camFocusPosWs).sqrMagnitude > 0.1f)
            camPosWS += (focusPosWS - camFocusPosWs) * cameraSpeed * deltaTime;

        return camPosWS;
    }

    private Vector3 UpdateCameraShake(Camera cam, Vector3 camPosWS, float deltaTime)
    {
        if (trackingCreature.stats.hp != hpStored)
        {
            shakeFactor = (hpStored - trackingCreature.stats.hp) / (float)trackingCreature.stats.hpMax;
            hpStored = trackingCreature.stats.hp;

            if (shakeFactor > 0)
                shakeAnimTime = 0f; // start shaking!
        }

        if (shakeAnimTime >= 0f)
        {
            shakeAnimTime += deltaTime;
            Vector3 rightDirWS = cam.transform.right;
            Vector3 upDirWS = cam.transform.up;

            float duration = Mathf.Clamp(shakeFactor * 2f, 0f, 0.1f);
            float intensity = (1f - Mathf.Pow(Mathf.Clamp01(shakeAnimTime / duration), 2f)) * 0.1f;
            float rx = 0f;
            float ry = (1f - shakeAnimTime / duration) * intensity;

            camPosWS += rightDirWS * rx + upDirWS * ry;

            if (shakeAnimTime >= duration)
                shakeAnimTime = -1f;
        }

        return camPosWS;
    }
}

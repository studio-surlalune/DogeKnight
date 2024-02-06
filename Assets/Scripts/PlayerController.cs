using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public Camera trackingCam;

    private float speed = 4.0f;
    private float rotationSpeed = 16.0f;
    private float defensiveRotationSpeed = 7.0f;
    private float jumpForce = 20.0f;
    private Creature creature;
    private Animator animator;
    private Rigidbody rb;

    void Awake()
    {
        Game.RegisterPlayerController(this);
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        // Cannot be done in Awake() due to script ordering issue.
        creature = GetComponent<CreatureRegistry>().creature;
    }

    public void UpdateGame(List<Creature> creatures)
    {
        float deltaTime = Time.deltaTime;

        // Maybe the event should be processed only by the UI system (mouse click).
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0);
        bool isDefending = !isOverUI && Input.GetButton("Fire3");

        if (creature is DogeKnight)
           ((DogeKnight)creature).isDefending = isDefending;

        // Get camera translation vectors.
        Vector3 rightDirWS = trackingCam.transform.right;
        Vector3 upDirWS = trackingCam.transform.up;
        rightDirWS.y = 0.0f;
        upDirWS.y = 0.0f;
        rightDirWS = Vector3.Normalize(rightDirWS);
        upDirWS = Vector3.Normalize(upDirWS);

        // Movement relative to the camera
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        if (isDefending)
        {
            // Move slower if defending (raising shield).
            moveHorizontal *= 0.75f;
            moveVertical *= 0.75f;
        }

        Vector3 characterVelWS = moveVertical * upDirWS + moveHorizontal * rightDirWS;
        Vector3 characterDirWS = characterVelWS;
        float actualRotationSpeed = rotationSpeed;

        if (isDefending)
        {
            // Find nearest enemy.
            float closestDistance;
            Creature closestCreature = Creature.FindClosestCreature(creature, creatures, false, out closestDistance);
            if (closestDistance < 5f)
            {
                Vector3 directionWS = closestCreature.gameObject.transform.position - transform.position;
                directionWS.y = 0;
                characterDirWS = Vector3.Normalize(directionWS);
                actualRotationSpeed = defensiveRotationSpeed;
            }
        }

        bool isOnFloor = TestFloorContact();

        Quaternion prevRotation = transform.rotation;

        transform.rotation = Quaternion.identity;
        transform.Translate(characterVelWS * speed * deltaTime);

        if (characterDirWS.sqrMagnitude > 0.01f)
        {
            Quaternion newRotation = Quaternion.LookRotation(characterDirWS, Vector3.up);
            transform.rotation = Quaternion.Lerp(prevRotation, newRotation, actualRotationSpeed * deltaTime);
        }
        else
            transform.rotation = prevRotation;

        animator.SetBool("IsRunning", characterVelWS.sqrMagnitude > 0.3f);
        animator.SetBool("IsOnFloor", isOnFloor);

        // Attack
        if (!isOverUI && Input.GetButtonDown("Fire1"))
            animator.SetTrigger("TriggerAttack01");

        // Defense
        animator.SetBool("IsDefending", isDefending);

        // Jumping
        if (Input.GetButtonDown("Jump") && isOnFloor)
        {
            animator.SetTrigger("TriggerJump");
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
        }
    }

    private bool TestFloorContact()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        Debug.Assert(boxCollider, "Expected box collider");
        if (boxCollider == null)
            return false;

        Span<Vector3> extentSelector = stackalloc Vector3[] {
            new Vector3(-1.0f, -1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, 1.0f),
            new Vector3(-1.0f, -1.0f, 1.0f)
        };

        Vector3 posWS = transform.position + boxCollider.center;
        Vector3 extents = boxCollider.size * 0.5f;

        float minDistSq = float.MaxValue;
        for (int i = 0; i < 4; ++i)
        {
            Vector3 side = extentSelector[i];
            Vector3 raycastPos = posWS + Vector3.Scale(extents, side);

            RaycastHit hit;
            if (Physics.Raycast(raycastPos, Vector3.down, out hit, 0.5f))
                minDistSq = Mathf.Min(minDistSq, (hit.point - raycastPos).sqrMagnitude);
        }

        return minDistSq <= (0.15f * 0.15f);
    }
}

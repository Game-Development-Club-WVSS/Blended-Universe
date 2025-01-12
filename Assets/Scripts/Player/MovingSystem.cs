using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class MovingSystem : MonoBehaviour
{
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
    public Camera playerCamera;
    public Transform headCamera;
    public Transform head;
    public GameObject joint;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;
    bool canMove = true;

    private Animator animator;
    private AudioSource step;
    private CombatSystem combatSystem;

    void Start()
    {
        animator = joint.GetComponent<Animator>();
        characterController = this.GetComponent<CharacterController>();
        step = this.GetComponent<AudioSource>();
        combatSystem = this.GetComponent<CombatSystem>();
    }

    int attackCnt = 0;
    bool attack = false;
    float lastAttack;
    bool lastGrounded = true;

    void Update()
    {
        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        // Press Left Shift to run

        canMove = Cursor.lockState == CursorLockMode.Locked;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;

        // get hurt
        if (characterController.isGrounded && lastGrounded == false && moveDirection.y < -20)
        {
            combatSystem.HealthChange(Convert.ToInt32(150 * moveDirection.y)+1000);
        }
        lastGrounded = characterController.isGrounded;


        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);


        // Player and Camera rotation
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        //==================== Animation =======================

        if (Input.GetMouseButtonDown(0) && canMove)
        {
            attack = true;
            attackCnt++;
            if (attackCnt > 3) attackCnt = 1;
            lastAttack = Time.realtimeSinceStartup;
        }

        if (attack && attackCnt == 1) animator.SetTrigger("Attack01");
        if (attack && attackCnt == 2) animator.SetTrigger("Attack02");
        if (attack && attackCnt == 3) animator.SetTrigger("Attack03");

        step.pitch = 0.7f / 4 * Mathf.Max(curSpeedX,curSpeedY);
        if (Time.realtimeSinceStartup - lastAttack >= 1.5f)
        {
            attackCnt = 0;
            attack = false;
            animator.SetBool("Attack", false);
            if (moveDirection.z != 0)
            {
                animator.SetBool("Move", true);
                if(!step.isPlaying && characterController.isGrounded) step.Play();
            }
            else
            {
                step.Stop();
                animator.SetBool("Move", false);
            }
        }

        if (attack)
        {
            attack = false;
            animator.SetBool("Move", moveDirection.z != 0);
        }
    }

    void LateUpdate()
    {
        Vector3 jointPosition = joint.transform.localPosition;
        jointPosition.x = 0;
        jointPosition.z = 0;
        joint.transform.localPosition = jointPosition;

        headCamera.position = head.position;
        Vector3 headRotation = head.localEulerAngles;
        if (headRotation.x > 270) headRotation.x -= 360;
        if (headRotation.y > 270) headRotation.y -= 360;
        if (headRotation.z > 270) headRotation.z -= 360;
        headCamera.localEulerAngles = headRotation * 0.25f;
    }
}

using UnityEngine;

public class EnemyFollow : UnityEngine.MonoBehaviour
{
    [Header("References")]
    public UnityEngine.Transform player;
    public UnityEngine.Animator animator;
    public UnityEngine.Rigidbody playerRigidbody;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 12f;

    [Header("Movement Smoothing")]
    public float accelerationTime = 0.2f;
    public float decelerationTime = 0.04f;

    [Header("Distance Settings")]
    public float stoppingDistance = 3f;
    public float respawnDistance = 20f;
    public float respawnOffset = 2f;

    [Header("Obstacle Detection")]
    public float obstacleCheckDistance = 1.5f;
    public float obstacleCheckHeight = 0.6f;
    public float jumpForce = 6f;
    public float jumpCooldown = 0.8f;

    private UnityEngine.Rigidbody rb;

    private UnityEngine.Vector3 currentMovement;
    private UnityEngine.Vector3 movementVelocity;

    private bool isMoving;
    private bool isRunning;
    private bool isGrounded;

    private float lastJumpTime = -10f;

    void Start()
    {
        rb = GetComponent<UnityEngine.Rigidbody>();

        if (rb == null)
            UnityEngine.Debug.LogError("Enemy needs a Rigidbody!");

        if (animator == null)
            UnityEngine.Debug.LogError("Enemy Animator is not assigned!");

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsJumping", false);
        }
    }

    void Update()
    {
        if (player == null || rb == null)
            return;

        float distance = UnityEngine.Vector3.Distance(
            transform.position,
            player.position
        );

        // Respawn if too far away.
        if (distance > respawnDistance &&
            playerRigidbody != null &&
            playerRigidbody.linearVelocity.y >= 0f)
        {
            UnityEngine.Vector3 respawnPosition =
                player.position + player.right * respawnOffset;

            rb.position = respawnPosition;
            rb.linearVelocity = UnityEngine.Vector3.zero;
            rb.angularVelocity = UnityEngine.Vector3.zero;

            currentMovement = UnityEngine.Vector3.zero;
            movementVelocity = UnityEngine.Vector3.zero;

            isGrounded = false;

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsJumping", false);
            }

            return;
        }

        // Calculate direction towards player.
        UnityEngine.Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        isMoving = distance > stoppingDistance &&
                   direction.sqrMagnitude > 0.001f;

        isRunning = distance > 8f && isMoving;

        // Update movement animation.
        if (animator != null)
        {
            float animationSpeed = 0f;

            if (isMoving)
                animationSpeed = isRunning ? 1f : 0.5f;

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.1f,
                UnityEngine.Time.deltaTime
            );
        }
    }

    void FixedUpdate()
    {
        if (player == null || rb == null)
            return;

        UnityEngine.Vector3 direction =
            player.position - rb.position;

        direction.y = 0f;

        float distance = direction.magnitude;

        // Determine movement direction.
        UnityEngine.Vector3 targetMovement =
            UnityEngine.Vector3.zero;

        if (distance > stoppingDistance && distance > 0.001f)
            targetMovement = direction.normalized;

        // Check for obstacles in front.
        if (isGrounded &&
            targetMovement.sqrMagnitude > 0.001f &&
            UnityEngine.Time.time - lastJumpTime >= jumpCooldown)
        {
            UnityEngine.Vector3 rayOrigin =
                rb.position +
                UnityEngine.Vector3.up * obstacleCheckHeight;

            bool obstacleDetected =
                UnityEngine.Physics.Raycast(
                    rayOrigin,
                    targetMovement,
                    out UnityEngine.RaycastHit hit,
                    obstacleCheckDistance
                );

            if (obstacleDetected &&
                hit.collider.gameObject.CompareTag("Ground"))
            {
                Jump();
            }
        }

        // Smooth acceleration and deceleration.
        float smoothTime = targetMovement.sqrMagnitude < 0.001f
            ? decelerationTime
            : accelerationTime;

        currentMovement = UnityEngine.Vector3.SmoothDamp(
            currentMovement,
            targetMovement,
            ref movementVelocity,
            smoothTime
        );

        // Choose walking or running speed.
        float currentSpeed = distance > 8f
            ? runSpeed
            : moveSpeed;

        // Move using Rigidbody.
        rb.MovePosition(
            rb.position +
            currentMovement * currentSpeed *
            UnityEngine.Time.fixedDeltaTime
        );

        // Smoothly rotate towards player.
        if (direction.sqrMagnitude > 0.001f &&
            distance > stoppingDistance)
        {
            UnityEngine.Quaternion targetRotation =
                UnityEngine.Quaternion.LookRotation(direction);

            UnityEngine.Quaternion smoothRotation =
                UnityEngine.Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed *
                    UnityEngine.Time.fixedDeltaTime
                );

            rb.MoveRotation(smoothRotation);
        }
    }

    // Perform jump.
    void Jump()
    {
        if (!isGrounded)
            return;

        isGrounded = false;
        lastJumpTime = UnityEngine.Time.time;

        rb.AddForce(
            UnityEngine.Vector3.up * jumpForce,
            UnityEngine.ForceMode.Impulse
        );

        if (animator != null)
            animator.SetBool("IsJumping", true);
    }

    // Detect ground collision.
    void OnCollisionEnter(UnityEngine.Collision collision)
    {
        CheckGroundCollision(collision);
    }

    void OnCollisionStay(UnityEngine.Collision collision)
    {
        CheckGroundCollision(collision);
    }

    void OnCollisionExit(UnityEngine.Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;

            if (animator != null)
                animator.SetBool("IsJumping", true);
        }
    }

    // Check whether enemy is actually standing on ground.
    void CheckGroundCollision(UnityEngine.Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
            return;

        foreach (UnityEngine.ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;

                if (animator != null)
                    animator.SetBool("IsJumping", false);

                return;
            }
        }
    }
}

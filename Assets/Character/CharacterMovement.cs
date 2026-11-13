using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Numerics;

public class CharacterMovement : UnityEngine.MonoBehaviour
{
    [Header("SpecialEffect")]
    public Volume sceneVolume;
    private ChromaticAberration chromaticAberration;
    public float maxCA;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float diveMovingSpeed = 7f;
    private float moveSpeed = 3f;
    public float runSpeed = 8f;
    public float rotationSpeed = 12f;

    [Header("Movement Smoothing")]
    public float accelerationTime = 0.2f;
    public float decelerationTime = 0.04f;

    [Header("Double Tap Sprint")]
    public float doubleTapWindow = 0.3f;

    private float lastWPressTime = float.NegativeInfinity;
    private float lastAPressTime = float.NegativeInfinity;
    private float lastSPressTime = float.NegativeInfinity;
    private float lastDPressTime = float.NegativeInfinity;

    private bool sprintForward;
    private bool sprintLeft;
    private bool sprintBackward;
    private bool sprintRight;

    private bool isDoubleTapSprinting;

    [Header("Jumping")]
    public float jumpForce = 5f;
    public float MaxSpeed = 30f;
    public float FloatSpeed = 5f;
    private float terminalVelocity = 30f;

    [Header("Camera")]
    public UnityEngine.Transform cameraTransform;

    [Header("Camera FOV")]
    public UnityEngine.Camera playerCamera;
    public float normalFOV = 30f;
    public float sprintFOV = 40f;
    public float fallFOV = 70f;
    public float floatingFOV = 50f;
    private float fallingFOV = 70f;

    public float fovSmoothTime = 0.25f;
    public float fallingFOVSmoothTime = 0.12f;

    [Header("Falling FOV")]
    public float fallingSpeedThreshold = 10f;

    private float fovVelocity;

    [Header("Dive Movement")]
    [Range(0f, 1f)]
    public float diveRotationMultiplier = 0.35f;

    [Header("Animation")]
    public UnityEngine.Animator animator;

    private UnityEngine.Rigidbody rb;

    private bool isGrounded;
    private bool isJumping;
    private bool isSprinting;

    private UnityEngine.Vector2 moveInput;
    private UnityEngine.Vector2 currentMoveInput;
    private UnityEngine.Vector2 moveInputVelocity;

    private bool wasDiving;
    private bool movementLocked;
    private float movementUnlockTime;
    public float diveRecoveryTime = 1f;

    void Start()
    {
        moveSpeed = walkSpeed;
        sceneVolume.profile.TryGet(out chromaticAberration);
        fallingFOV = fallFOV;
        terminalVelocity = MaxSpeed;
        rb = GetComponent<UnityEngine.Rigidbody>();

        if (rb == null)
            UnityEngine.Debug.LogError("Player needs a Rigidbody!");

        if (animator == null)
            UnityEngine.Debug.LogError("Animator is not assigned!");

        if (playerCamera == null && cameraTransform != null)
            playerCamera = cameraTransform.GetComponent<UnityEngine.Camera>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<UnityEngine.Camera>();

        if (playerCamera == null)
            UnityEngine.Debug.LogError("Player Camera is not assigned!");

        isGrounded = false;
        isJumping = !isGrounded;

        UpdateJumpAnimation();

        if (animator != null)
            animator.SetFloat("Speed", 0f);

        if (playerCamera != null)
            playerCamera.fieldOfView = normalFOV;
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        if (moveInput == UnityEngine.Vector2.zero)
        {
            rb.angularVelocity = UnityEngine.Vector3.zero;
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;

        HandleDiveMovementLock();
        HandleSlowFalling();

        // Punching input.
        if (!movementLocked && animator != null && mouse != null &&
            !sprintForward && !sprintLeft && !sprintRight &&
            !sprintBackward && isGrounded)
        {
            if (keyboard.xKey.wasPressedThisFrame)
            {
                animator.SetTrigger("Punch");
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                animator.SetTrigger("Cross");
            }
        }

        if (movementLocked)
        {
            moveInput = UnityEngine.Vector2.zero;
            currentMoveInput = UnityEngine.Vector2.zero;
            moveInputVelocity = UnityEngine.Vector2.zero;

            isSprinting = false;

            if (animator != null)
                animator.SetFloat("Speed", 0f);

            return;
        }

        // Double-tap detection: W
        if (keyboard.wKey.wasPressedThisFrame)
        {
            if (UnityEngine.Time.time - lastWPressTime <= doubleTapWindow && isGrounded)
                sprintForward = true;

            lastWPressTime = UnityEngine.Time.time;
        }

        // Double-tap detection: A
        if (keyboard.aKey.wasPressedThisFrame)
        {
            if (UnityEngine.Time.time - lastAPressTime <= doubleTapWindow && isGrounded)
                sprintLeft = true;

            lastAPressTime = UnityEngine.Time.time;
        }

        // Double-tap detection: S
        if (keyboard.sKey.wasPressedThisFrame)
        {
            if (UnityEngine.Time.time - lastSPressTime <= doubleTapWindow && isGrounded)
                sprintBackward = true;

            lastSPressTime = UnityEngine.Time.time;
        }

        // Double-tap detection: D
        if (keyboard.dKey.wasPressedThisFrame)
        {
            if (UnityEngine.Time.time - lastDPressTime <= doubleTapWindow && isGrounded)
                sprintRight = true;

            lastDPressTime = UnityEngine.Time.time;
        }

        // Reset sprint when corresponding key is released.
        if (!keyboard.wKey.isPressed)
            sprintForward = false;

        if (!keyboard.aKey.isPressed)
            sprintLeft = false;

        if (!keyboard.sKey.isPressed)
            sprintBackward = false;

        if (!keyboard.dKey.isPressed)
            sprintRight = false;

        // Read movement input.
        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed)
            horizontal -= 1f;

        if (keyboard.dKey.isPressed)
            horizontal += 1f;

        if (keyboard.wKey.isPressed)
            vertical += 1f;

        if (keyboard.sKey.isPressed)
            vertical -= 1f;

        moveInput = new UnityEngine.Vector2(horizontal, vertical);

        // Sprint states.
        bool shiftSprinting = keyboard.leftShiftKey.isPressed;

        isDoubleTapSprinting =
            (sprintForward && keyboard.wKey.isPressed) ||
            (sprintBackward && keyboard.sKey.isPressed) ||
            (sprintLeft && keyboard.aKey.isPressed) ||
            (sprintRight && keyboard.dKey.isPressed);

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        isSprinting = isMoving &&
            (shiftSprinting || isDoubleTapSprinting);

        // Update movement animation.
        if (animator != null)
        {
            float animationSpeed = 0f;

            if (isMoving)
            {
                if (isDoubleTapSprinting)
                    animationSpeed = 2f;
                else if (shiftSprinting)
                    animationSpeed = 1f;
                else
                    animationSpeed = 0.5f;
            }

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.1f,
                UnityEngine.Time.deltaTime
            );
        }

        // Update camera FOV.
        UpdateCameraFOV();

        // Jump.
        if (rb != null &&
            keyboard.spaceKey.wasPressedThisFrame &&
            isGrounded &&
            !isJumping)
        {
            isGrounded = false;

            if (animator != null)
                animator.SetBool("JumpNotFall", true);

            UpdateJumpAnimation();

            rb.AddForce(
                UnityEngine.Vector3.up * jumpForce,
                UnityEngine.ForceMode.Impulse
            );
        }
    }

    // Smoothly transition camera FOV.
    void UpdateCameraFOV()
    {
        if (playerCamera == null || rb == null) return;

        float targetFOV = normalFOV;

        bool isFallingFast =
            rb.linearVelocity.y < -fallingSpeedThreshold;

        float currentSmoothTime = fovSmoothTime;

        if (isFallingFast)
        {
            targetFOV = fallingFOV;
            currentSmoothTime = fallingFOVSmoothTime;
        }
        else if (isSprinting)
        {
            targetFOV = sprintFOV;
        }

        playerCamera.fieldOfView = UnityEngine.Mathf.SmoothDamp(
            playerCamera.fieldOfView,
            targetFOV,
            ref fovVelocity,
            currentSmoothTime
        );
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Terminal velocity.
        UnityEngine.Vector3 velocity = rb.linearVelocity;

        if (velocity.y < -terminalVelocity)
        {
            velocity.y = -terminalVelocity;
            rb.linearVelocity = velocity;
        }

        if (movementLocked)
        {
            currentMoveInput = UnityEngine.Vector2.zero;
            moveInputVelocity = UnityEngine.Vector2.zero;
            return;
        }

        if (cameraTransform == null) return;

        // Check whether player is diving.
        bool isDiving = animator != null && animator.GetBool("Diving");

        // Movement smoothing.
        float smoothTime = moveInput.sqrMagnitude < 0.01f
            ? decelerationTime
            : accelerationTime;

        currentMoveInput = UnityEngine.Vector2.SmoothDamp(
            currentMoveInput,
            moveInput,
            ref moveInputVelocity,
            smoothTime
        );

        // Camera-relative directions.
        UnityEngine.Vector3 forward = cameraTransform.forward;
        UnityEngine.Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // Calculate movement direction.
        UnityEngine.Vector3 movement =
            (forward * currentMoveInput.y +
             right * currentMoveInput.x).normalized;

        // Walking or running speed.
        float currentSpeed = isSprinting ? runSpeed : moveSpeed;

        // Move character.
        // Movement remains enabled while jumping and diving.
        rb.MovePosition(
            rb.position +
            movement * currentSpeed *
            UnityEngine.Time.fixedDeltaTime
        );

        // Character rotation.
        if (movement.sqrMagnitude > 0.001f)
        {
            // While jumping normally, don't rotate.
            // While diving, rotation is allowed but reduced.
            if (isJumping && !isDiving)
                return;

            UnityEngine.Quaternion targetRotation =
                UnityEngine.Quaternion.LookRotation(movement);

            float currentRotationSpeed = isDiving
                ? rotationSpeed * diveRotationMultiplier
                : rotationSpeed;

            UnityEngine.Quaternion smoothRotation =
                UnityEngine.Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    currentRotationSpeed * UnityEngine.Time.fixedDeltaTime
                );

            rb.MoveRotation(smoothRotation);
        }

        if(isDiving)
        {
            moveSpeed = diveMovingSpeed;
        }
        else
        {
            moveSpeed = walkSpeed;
        }
    }

    void UpdateJumpAnimation()
    {
        isJumping = !isGrounded;

        if (animator != null)
            animator.SetBool("IsJumping", isJumping);
    }

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
            UpdateJumpAnimation();
        }
    }

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
                    animator.SetBool("JumpNotFall", false);

                UpdateJumpAnimation();

                return;
            }
        }
    }

    void HandleDiveMovementLock()
    {
        if (animator == null) return;

        bool isDiving = animator.GetBool("Diving");

        // Detect when the dive animation finishes.
        if (wasDiving && !isDiving)
        {
            movementLocked = true;
            movementUnlockTime = UnityEngine.Time.time + diveRecoveryTime;
        }

        wasDiving = isDiving;

        // Unlock movement after recovery period.
        if (movementLocked && UnityEngine.Time.time >= movementUnlockTime)
        {
            movementLocked = false;
        }
    }

    void HandleSlowFalling()
    {
        if (animator.GetBool("Diving") && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
        {
            chromaticAberration.intensity.value = 0f;
            animator.SetBool("SlowFalling", true);
            terminalVelocity = FloatSpeed;
            fallingFOV = floatingFOV;
        }
        else if (animator.GetBool("Diving"))
        {
            chromaticAberration.intensity.value = maxCA;
            animator.SetBool("SlowFalling", false);
            terminalVelocity = MaxSpeed;
            fallingFOV = fallFOV;
        }
        else
        {
            chromaticAberration.intensity.value = 0;
        }
    }
}

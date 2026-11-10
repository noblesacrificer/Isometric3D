
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovement : UnityEngine.MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
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

    [Header("Camera")]
    public UnityEngine.Transform cameraTransform;

    [Header("Camera FOV")]
    public UnityEngine.Camera playerCamera;
    public float normalFOV = 50f;
    public float sprintFOV = 80f;
    public float fallingFOV = 90f;

    public float fovSmoothTime = 0.25f;
    public float fallingFOVSmoothTime = 0.12f;

    [Header("Falling FOV")]
    public float fallingSpeedThreshold = 10f;

    private float fovVelocity;

    [Header("Animation")]
    public UnityEngine.Animator animator;

    private UnityEngine.Rigidbody rb;

    private bool isGrounded;
    private bool isJumping;
    private bool isSprinting;

    private UnityEngine.Vector2 moveInput;
    private UnityEngine.Vector2 currentMoveInput;
    private UnityEngine.Vector2 moveInputVelocity;

    void Start()
    {
        rb = GetComponent<UnityEngine.Rigidbody>();

        if (rb == null)
            UnityEngine.Debug.LogError("Player needs a Rigidbody!");

        if (animator == null)
            UnityEngine.Debug.LogError("Animator is not assigned!");

        // Automatically find camera if not assigned.
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

        var keyboard = UnityEngine.InputSystem.Keyboard.current;

        // Double-tap detection: W
        if (keyboard.wKey.wasPressedThisFrame)
        {
            if (UnityEngine.Time.time - lastWPressTime <= doubleTapWindow)
                sprintForward = true;

            lastWPressTime = UnityEngine.Time.time;
        }

        // Double-tap detection: A
        if (keyboard.aKey.wasPressedThisFrame)
        {
            if (UnityEngine.Time.time - lastAPressTime <= doubleTapWindow)
                sprintLeft = true;

            lastAPressTime = UnityEngine.Time.time;
        }

        // Double-tap detection: S
        if (keyboard.sKey.wasPressedThisFrame)
        {
            if (UnityEngine.Time.time - lastSPressTime <= doubleTapWindow)
                sprintBackward = true;

            lastSPressTime = UnityEngine.Time.time;
        }

        // Double-tap detection: D
        if (keyboard.dKey.wasPressedThisFrame)
        {
            if (UnityEngine.Time.time - lastDPressTime <= doubleTapWindow)
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

        // Falling FOV takes priority over sprint FOV.
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
        if (rb == null || cameraTransform == null) return;

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

        UnityEngine.Vector3 movement =
            (forward * currentMoveInput.y +
             right * currentMoveInput.x).normalized;

        // Walking or running speed.
        float currentSpeed = isSprinting ? runSpeed : moveSpeed;

        // Move character.
        rb.MovePosition(
            rb.position +
            movement * currentSpeed *
            UnityEngine.Time.fixedDeltaTime
        );

        // Smooth character rotation.
        if (movement.sqrMagnitude > 0.001f)
        {
            UnityEngine.Quaternion targetRotation =
                UnityEngine.Quaternion.LookRotation(movement);

            UnityEngine.Quaternion smoothRotation =
                UnityEngine.Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * UnityEngine.Time.fixedDeltaTime
                );

            rb.MoveRotation(smoothRotation);
        }
    }

    // Keep IsJumping opposite to isGrounded.
    void UpdateJumpAnimation()
    {
        isJumping = !isGrounded;

        if (animator != null)
            animator.SetBool("IsJumping", isJumping);
    }

    // Detect initial ground collision.
    void OnCollisionEnter(UnityEngine.Collision collision)
    {
        CheckGroundCollision(collision);
    }

    // Continuously check for ground contact.
    void OnCollisionStay(UnityEngine.Collision collision)
    {
        CheckGroundCollision(collision);
    }

    // Reset grounded state when leaving the ground.
    void OnCollisionExit(UnityEngine.Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            UpdateJumpAnimation();
        }
    }

    // Check whether the collision is actually ground contact.
    void CheckGroundCollision(UnityEngine.Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
            return;

        foreach (UnityEngine.ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                UpdateJumpAnimation();

                return;
            }
        }
    }
}

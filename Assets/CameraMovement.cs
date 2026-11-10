
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : UnityEngine.MonoBehaviour
{
    [Header("Target")]
    public UnityEngine.Transform target;

    [Header("Camera Position")]
    public float cameraDistance = 6f;
    public float cameraHeight = 2.5f;
    public float lookAtHeight = 1.2f;
    public float minimumHeightOffset = -1f;
    public float followSmoothness = 10f;

    [Header("Camera Rotation")]
    public float rotationSpeed = 100f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch = 15f;

    void Start()
    {
        if (target == null) return;

        yaw = target.eulerAngles.y;
        pitch = 15f;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        // Arrow key camera rotation
        if (Keyboard.current.leftArrowKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.rightArrowKey.isPressed)
            horizontal += 1f;

        if (Keyboard.current.upArrowKey.isPressed)
            vertical += 1f;

        if (Keyboard.current.downArrowKey.isPressed)
            vertical -= 1f;

        yaw += horizontal * rotationSpeed *
               UnityEngine.Time.deltaTime;

        pitch -= vertical * rotationSpeed *
                 UnityEngine.Time.deltaTime;

        pitch = UnityEngine.Mathf.Clamp(
            pitch, minPitch, maxPitch
        );
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate camera rotation
        UnityEngine.Quaternion rotation =
            UnityEngine.Quaternion.Euler(pitch, yaw, 0f);

        // Calculate look-at position
        UnityEngine.Vector3 lookAtPosition =
            target.position +
            UnityEngine.Vector3.up * lookAtHeight;

        // Calculate desired camera position
        UnityEngine.Vector3 desiredPosition =
            lookAtPosition +
            rotation * new UnityEngine.Vector3(
                0f, 0f, -cameraDistance
            );

        desiredPosition.y += cameraHeight;

        // Prevent camera from going too far below player
        float minimumCameraY =
            target.position.y + minimumHeightOffset;

        desiredPosition.y = UnityEngine.Mathf.Max(
            desiredPosition.y,
            minimumCameraY
        );

        // Smooth camera movement
        transform.position = UnityEngine.Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSmoothness * UnityEngine.Time.deltaTime
        );

        // Always look towards the designated point
        transform.rotation = UnityEngine.Quaternion.LookRotation(
            lookAtPosition - transform.position
        );
    }
}

using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerLook : NetworkBehaviour
{
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float mouseSensitivity = 0.08f;
    [SerializeField] private float xClamp = 80f;

    [Header("Smooth")]
    [SerializeField] private float smoothTime = 0.03f;

    private PlayerInput playerInput;
    private InputAction lookAction;

    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;

    private float yawVelocity;
    private float pitchVelocity;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        lookAction = playerInput.actions["Look"];

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        targetYaw = transform.eulerAngles.y;
        currentYaw = targetYaw;
        targetPitch = 0f;
        currentPitch = 0f;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (lookAction == null) return;
        if (cameraRoot == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        targetYaw += lookInput.x * mouseSensitivity;
        targetPitch -= lookInput.y * mouseSensitivity;
        targetPitch = Mathf.Clamp(targetPitch, -xClamp, xClamp);

        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, smoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, smoothTime);

        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        cameraRoot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
}
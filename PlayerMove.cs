using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMove : NetworkBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkAnimator networkAnimator;

    [Header("Footstep Loop Audio")]
    [SerializeField] private AudioSource footstepLoopAudioSource;
    [SerializeField] private float minMoveInput = 0.1f;

    [Header("InputAction")]
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    private CharacterController controller;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (networkAnimator == null)
        {
            networkAnimator = GetComponentInChildren<NetworkAnimator>();
        }

        // 不建議自動抓，因為 Player 可能有槍聲 AudioSource。
        // 所以 footstepLoopAudioSource 建議你在 Inspector 手動拖。
    }

    public override void OnNetworkSpawn()
    {
        bool isLocalOwner = IsOwner;

        if (playerInput != null)
        {
            playerInput.enabled = isLocalOwner;
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(isLocalOwner);

            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = isLocalOwner;
            }
        }

        if (isLocalOwner && playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (moveAction == null || jumpAction == null) return;

        bool grounded = controller.isGrounded;

        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();

        HandleFootstepLoop(input, grounded);

        if (animator != null)
        {
            animator.SetFloat("Speed", input.magnitude);
        }

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        if (move.magnitude > 1f)
        {
            move = move.normalized;
        }

        controller.Move(move * moveSpeed * Time.deltaTime);

        if (jumpAction.WasPressedThisFrame() && grounded)
        {
            StopFootstepLoop();

            if (networkAnimator != null)
            {
                networkAnimator.SetTrigger("Jump");
            }
            else if (animator != null)
            {
                animator.SetTrigger("Jump");
            }

            velocity.y = Mathf.Sqrt(jumpHeight * gravity * -2f);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleFootstepLoop(Vector2 input, bool grounded)
    {
        if (footstepLoopAudioSource == null) return;

        bool shouldPlayFootstep =
            grounded &&
            input.magnitude > minMoveInput;

        if (shouldPlayFootstep)
        {
            if (!footstepLoopAudioSource.isPlaying)
            {
                footstepLoopAudioSource.Play();
            }
        }
        else
        {
            StopFootstepLoop();
        }
    }

    private void StopFootstepLoop()
    {
        if (footstepLoopAudioSource != null && footstepLoopAudioSource.isPlaying)
        {
            footstepLoopAudioSource.Stop();
        }
    }

    private void OnDisable()
    {
        StopFootstepLoop();
    }
}
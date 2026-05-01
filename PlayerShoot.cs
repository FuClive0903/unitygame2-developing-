using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerShoot : NetworkBehaviour
{
    [Header("Shoot")]
    [SerializeField] private float shootDistance = 100f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float fireCooldown = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkAnimator networkAnimator;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSfx;

    private PlayerInput playerInput;
    private InputAction fireAction;
    private float cooldownTimer;

    private PlayerNetworkState selfState;
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        selfState = GetComponent<PlayerNetworkState>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (networkAnimator == null)
        {
            networkAnimator = GetComponentInChildren<NetworkAnimator>();
        }
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        fireAction = playerInput.actions["Fire"];
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (fireAction == null) return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!fireAction.WasPressedThisFrame()) return;
        if (cooldownTimer > 0f) return;

        TryShoot();
        cooldownTimer = fireCooldown;
    }
    private void PlayShootSfxLocal()
    {
        if (audioSource != null && shootSfx != null)
        {
            audioSource.PlayOneShot(shootSfx);
        }
    }
    private void TryShoot()
    {
        Camera cam = LocalPlayerCamera.LocalCam;
        if (cam == null) return;

        // 自己立刻聽到槍聲
        PlayShootSfxLocal();

        // 通知其他玩家也播放槍聲
        PlayShootSfxServerRpc();

        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger("Shoot");
        }
        else if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            PlayerNetworkState target = hit.collider.GetComponentInParent<PlayerNetworkState>();

            if (target == null) return;
            if (target == selfState) return;
            if (target.IsDead.Value) return;

            ReportHitServerRpc(target.NetworkObject);

            Debug.Log($"Hit: {target.PlayerName.Value}  Damage: {damage}");
        }
    }

    [ServerRpc]
    private void ReportHitServerRpc(NetworkObjectReference targetRef)
    {
        if (!targetRef.TryGet(out NetworkObject targetObj)) return;

        PlayerNetworkState targetState = targetObj.GetComponent<PlayerNetworkState>();
        if (targetState == null) return;

        PlayerNetworkState shooterState = GetComponent<PlayerNetworkState>();
        if (shooterState != null && targetState == shooterState) return;
        if (targetState.IsDead.Value) return;

        targetState.ApplyDamage(damage);
    }
    [ServerRpc]
    private void PlayShootSfxServerRpc()
    {
        PlayShootSfxClientRpc();
    }

    [ClientRpc]
    private void PlayShootSfxClientRpc()
    {
        if (IsOwner) return;

        PlayShootSfxLocal();
    }
}
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerNetworkState : NetworkBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Death / Respawn Refs")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CapsuleCollider hitCollider;
    [SerializeField] private Behaviour[] componentsToDisableWhenDead; // 拖 PlayerMove / PlayerLook / PlayerShoot
    [SerializeField] private GameObject headUIAnchor;                 // 頭頂 UI 掛點

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkAnimator networkAnimator;
    public int MaxHP => maxHP;

    public NetworkVariable<FixedString32Bytes> PlayerName =
        new NetworkVariable<FixedString32Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> HP =
        new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<bool> IsDead =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private Coroutine respawnCoroutine;
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private bool cachedInitialSpawn;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (hitCollider == null)
            hitCollider = GetComponent<CapsuleCollider>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (networkAnimator == null)
        {
            networkAnimator = GetComponentInChildren<NetworkAnimator>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!cachedInitialSpawn)
        {
            initialSpawnPosition = transform.position;
            initialSpawnRotation = transform.rotation;
            cachedInitialSpawn = true;
        }

        if (IsServer)
        {
            PlayerName.Value = new FixedString32Bytes($"player_{OwnerClientId + 1}");
            HP.Value = maxHP;
            IsDead.Value = false;
        }

        IsDead.OnValueChanged += OnDeadChanged;

        ApplyDeadState(IsDead.Value);

        if (IsOwner)
        {
            LocalPlayerHUD hud = FindFirstObjectByType<LocalPlayerHUD>();
            if (hud != null)
            {
                hud.Bind(this);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        IsDead.OnValueChanged -= OnDeadChanged;
    }

    public void ApplyDamage(int damage)
    {
        if (!IsServer) return;
        if (IsDead.Value) return;
        if (HP.Value <= 0) return;

        HP.Value = Mathf.Max(0, HP.Value - damage);

        if (HP.Value <= 0)
        {
            DieServer();
        }
    }

    public void ResetHP()
    {
        if (!IsServer) return;
        HP.Value = maxHP;
    }

    private void DieServer()
    {
        if (!IsServer) return;
        if (IsDead.Value) return;

        IsDead.Value = true;

        if (respawnCoroutine != null)
            StopCoroutine(respawnCoroutine);
        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger("Die");
        }
        else if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        respawnCoroutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        RespawnServer();

        respawnCoroutine = null;
    }

    private void RespawnServer()
    {
        Vector3 respawnPos = initialSpawnPosition;
        Quaternion respawnRot = initialSpawnRotation;

        GameObject[] points = GameObject.FindGameObjectsWithTag("Respawn");
        if (points != null && points.Length > 0)
        {
            int index = Random.Range(0, points.Length);
            respawnPos = points[index].transform.position;
            respawnRot = points[index].transform.rotation;
        }

        // 先關 CharacterController 再傳送，避免卡牆或位移失敗
        if (characterController != null)
            characterController.enabled = false;

        transform.SetPositionAndRotation(respawnPos, respawnRot);

        HP.Value = maxHP;
        IsDead.Value = false;
        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger("Respawn");
        }
        else if (animator != null)
        {
            animator.SetTrigger("Respawn");
        }
    }

    private void OnDeadChanged(bool oldValue, bool newValue)
    {
        ApplyDeadState(newValue);
    }

    private void ApplyDeadState(bool dead)
    {
        if (componentsToDisableWhenDead != null)
        {
            foreach (var comp in componentsToDisableWhenDead)
            {
                if (comp != null)
                    comp.enabled = !dead;
            }
        }

        if (hitCollider != null)
            hitCollider.enabled = !dead;

        if (characterController != null)
            characterController.enabled = !dead;

        if (headUIAnchor != null)
        {
            if (IsOwner)
                headUIAnchor.SetActive(false); // 自己本來就不顯示頭頂 UI
            else
                headUIAnchor.SetActive(!dead);
        }
    }
}
using Unity.Netcode;
using UnityEngine;

public class LocalPlayerCamera : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    public static Camera LocalCam { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                LocalCam = playerCamera;
            }

            if (audioListener != null)
                audioListener.enabled = true;
        }
        else
        {
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(false);

            if (audioListener != null)
                audioListener.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (LocalCam == playerCamera)
            LocalCam = null;
    }
}

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<ulong, GameObject> spawnedPlayers = new();
    private bool hasSpawnedPlayers;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        }
    }

    private void HandleLoadEventCompleted(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;
        if (sceneName != "Game") return;
        if (hasSpawnedPlayers) return;

        hasSpawnedPlayers = true;

        Debug.Log($"[Spawner] Game scene load completed. Connected clients = {NetworkManager.Singleton.ConnectedClientsIds.Count}");
        SpawnAllConnectedPlayers();
    }

    private void SpawnAllConnectedPlayers()
    {
        int index = 0;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (spawnedPlayers.ContainsKey(clientId))
                continue;

            Transform spawnPoint = spawnPoints.Length > 0
                ? spawnPoints[index % spawnPoints.Length]
                : null;

            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GameObject playerObj = Instantiate(playerPrefab, pos, rot);

            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(clientId, true);

            spawnedPlayers.Add(clientId, playerObj);

            Debug.Log($"[Spawner] Spawned Player for clientId = {clientId}");
            index++;
        }
    }
}
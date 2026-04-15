using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayConnectionUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text statusText;
    [Header("Room")]
    [SerializeField] private int maxPlayersIncludingHost = 4;

    private const string ConnectionType = "dtls";
    private UnityTransport _transport;
    private async void Start()
    {
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (UnityServicesBootstrap.Instance == null)
        {
            SetStatus("’“≤ªµΩ UnityServicesBootstrap");
            return;
        }

        await UnityServicesBootstrap.Instance.InitializeAsync();

        if (UnityServicesBootstrap.Instance.IsReady &&
            AuthenticationService.Instance.IsSignedIn)
        {
            SetStatus($"UGS signed in. PlayerId: {UnityServicesBootstrap.Instance.PlayerId}");
        }
        else
        {
            SetStatus("UGS / Authentication initialization failed. Check Console");
        }
    }
    private void SetStatus(string msg)
    {
        Debug.Log(msg);

        if (statusText != null)
        {
            statusText.text = msg;
        }
    }
    public async void OnClickJoin()
    {
        await JoinAsync();
    }
    public async void OnClickHost()
    {
        await HostAsync();
    }
    public void OnClickLeave()
    {
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
            SetStatus("Already Leave");

            if (joinCodeText != null)
                joinCodeText.text = "Join Code: -";
        }
    }
    public void OnClickStartGame()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("NetworkManager not found");
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            SetStatus("Only the host can start the game");
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            SetStatus("Network session is not running");
            return;
        }

        SetStatus("Loading Game scene...");
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
    private async Task EnsureSignedInAsync()
    {
        if (UnityServicesBootstrap.Instance != null && !UnityServicesBootstrap.Instance.IsReady)
        {
            await UnityServicesBootstrap.Instance.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
    private async Task JoinAsync()
    {
        try
        {
            await EnsureSignedInAsync();

            string joinCode = joinCodeInput != null
                ? joinCodeInput.text.Trim().ToUpperInvariant()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                SetStatus("Please Input Join Code");
                return;
            }

            SetStatus("Joining Relay Room...");

            JoinAllocation allocation =
                await RelayService.Instance.JoinAllocationAsync(joinCode);

            _transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, ConnectionType));

            bool ok = NetworkManager.Singleton.StartClient();

            if (!ok)
            {
                SetStatus("StartClient Failed");
                return;
            }

            SetStatus($"Client Join Success£¨Code: {joinCode}");
        }
        catch (RelayServiceException ex)
        {
            SetStatus($"Relay Wrong: {ex.Message}");
            Debug.LogException(ex);
        }
        catch (AuthenticationException ex)
        {
            SetStatus($"Authentication Failed: {ex.Message}");
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            SetStatus($"RequestFailed: {ex.Message}");
            Debug.LogException(ex);
        }
        catch (Exception ex)
        {
            SetStatus($"UnknowError: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    private async Task HostAsync()
    {
        try
        {
            await EnsureSignedInAsync();

            int maxConnections = Mathf.Max(1, maxPlayersIncludingHost - 1);

            SetStatus("Creating Relay room...");

            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(maxConnections);

            string joinCode =
                await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (joinCodeText != null)
                joinCodeText.text = $"Join Code: {joinCode}";

            Debug.Log($"Join Code: {joinCode}");

            _transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, ConnectionType));

            bool ok = NetworkManager.Singleton.StartHost();

            if (!ok)
            {
                SetStatus("StartHost failed");
                return;
            }

            SetStatus($"Host started successfully. Join Code: {joinCode}");
        }
        catch (RelayServiceException ex)
        {
            SetStatus($"Relay error: {ex.Message}");
            Debug.LogException(ex);
        }
        catch (AuthenticationException ex)
        {
            SetStatus($"Authentication error: {ex.Message}");
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            SetStatus($"Request failed: {ex.Message}");
            Debug.LogException(ex);
        }
        catch (Exception ex)
        {
            SetStatus($"Unknown error: {ex.Message}");
            Debug.LogException(ex);
        }
    }
}

using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesBootstrap : MonoBehaviour
{
    public static UnityServicesBootstrap Instance { get; private set; }

    public bool IsReady { get; private set; }

    public string PlayerId =>
        AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerId
            : "(not signed in)";

    private Task _initializeTask;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _initializeTask = InitializeInternalAsync();
    }

    public Task InitializeAsync()
    {
        if (IsReady)
            return Task.CompletedTask;

        return _initializeTask ??= InitializeInternalAsync();
    }

    private async Task InitializeInternalAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsReady = true;
            Debug.Log($"UGS Ready. PlayerId = {AuthenticationService.Instance.PlayerId}");
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Authentication failed: {ex.Message}");
            Debug.LogException(ex);
            throw;
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Unity Services init failed: {ex.Message}");
            Debug.LogException(ex);
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected bootstrap error: {ex.Message}");
            Debug.LogException(ex);
            throw;
        }
    }
}
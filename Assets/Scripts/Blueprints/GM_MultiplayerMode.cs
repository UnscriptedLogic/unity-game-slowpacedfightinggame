using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnscriptedEngine;

public class GM_MultiplayerMode : UGameModeBase
{
    [Header("Multiplayer Extensions")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private List<Transform> spawnPoints;

    [SerializeField] private UIC_AbilityLoadout abilityLoadoutPrefab;
    [SerializeField] private UIC_MeleeLoadout meleeLoadOutPrefab;
    [SerializeField] private SettingsHUD settingsHUD;

    private UIC_AbilityLoadout _abilityLoadout;
    private UIC_MeleeLoadout _meleeLoadout;
    private CustomGameInstance customGameInstance;

    protected override void Init() { }

    protected override IEnumerator Start()
    {
        customGameInstance = GetGameInstance<CustomGameInstance>();
        if (customGameInstance.StartAsClient)
        {
            unityTransport.SetConnectionData(customGameInstance.IP, customGameInstance.Port);
            networkManager.StartClient();

            customGameInstance.ResetToggle();
        }

        PlayerEvents.OnPlayerDied += OnPlayerDied;

        yield return base.Start();

        inputContext.FindAction("Escape").canceled += OnEscape;
        InputContext.FindAction("AbilityMenu").canceled += ShowAbilityMenu;
        InputContext.FindAction("MeleeMenu").canceled += ShowMeleeMenu;

        //networkManager.OnClientStopped += NetworkManager_OnClientStopped;

        networkManager.OnClientConnectedCallback += OnClientConnected;

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            yield return UnityServices.InitializeAsync();
            yield return AuthenticationService.Instance.SignInAnonymouslyAsync();
        }


#if DEDICATED_SERVER

        MultiplayService.Instance.ReadyServerForPlayersAsync();
        Camera.main.gameObject.SetActive(false);
        Destroy(Camera.main.gameObject);
        Application.targetFrameRate = 60;
        Debug.Log($"[SERVER] Framerate targetted at: {Application.targetFrameRate}");
#endif
    }

    private void OnEscape(InputAction.CallbackContext context)
    {
        UIEscInputHandler.Invoke();
    }

    private void NetworkManager_OnClientStopped(bool obj)
    {
        if (IsClient && IsOwner)
        {
            LoadScene(0);
        }
    }

    private void ShowMeleeMenu(InputAction.CallbackContext context)
    {
        if (_playerPawn == null) return;
        if (_meleeLoadout == null)
        {
            _meleeLoadout = _playerPawn.AttachUIWidget(meleeLoadOutPrefab);
        }
        else
        {
            _playerPawn.DettachUIWidget(_meleeLoadout);
            _meleeLoadout = null;
        }
    }

    private void ShowAbilityMenu(InputAction.CallbackContext context)
    {
        if (_playerPawn == null) return;
        if (_abilityLoadout == null)
        {
            _abilityLoadout = _playerPawn.AttachUIWidget(abilityLoadoutPrefab);
        }
        else
        {
            _playerPawn.DettachUIWidget(_abilityLoadout);
            _abilityLoadout = null;
        }
    }

    private void OnPlayerDied(ulong playerId)
    {
        if (IsServer)
        {
            StartCoroutine(OnPlayerDiedCoroutine(playerId));
        }
    }

    private IEnumerator OnPlayerDiedCoroutine(ulong playerId)
    {
        Debug.Log("Respawning: " + playerId);
        yield return new WaitForSeconds(3f);
        ReSpawnPlayer(playerId);
    }

    private void OnClientConnected(ulong playerId)
    {
        if (IsClient)
        {
            _playerController = networkManager.LocalClient.PlayerObject.GetComponent<UController>();

            _playerController.AttachUIWidget(settingsHUD);

            SpawnPlayerPawnServerRpc(playerId);
        }
    }

    private void ReSpawnPlayer(ulong playerId)
    {
        Transform spawnPoint = spawnPoints.GetRandomElement();
        ULevelPawn pawn = Instantiate(playerPawn, spawnPoint.position, spawnPoint.rotation);
        pawn.GetComponent<NetworkObject>().SpawnWithOwnership(playerId);
        NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<UController>().PossessPawn(pawn, true);

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { playerId }
            }
        };

        ReSpawnPlayerPawnClientRpc(pawn.GetComponent<NetworkObject>(), clientRpcParams);
    }

    [ClientRpc]
    private void ReSpawnPlayerPawnClientRpc(NetworkObjectReference spawnedPlayerReference, ClientRpcParams clientParams = default)
    {
        spawnedPlayerReference.TryGet(out NetworkObject networkObject);
        _playerPawn = networkObject.GetComponent<ULevelPawn>();
        _playerController.ChangePossession(_playerPawn);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerPawnServerRpc(ulong playerId)
    {
        Transform spawnPoint = spawnPoints.GetRandomElement();
        ULevelPawn pawn = Instantiate(playerPawn, spawnPoint.position, spawnPoint.rotation);
        pawn.GetComponent<NetworkObject>().SpawnWithOwnership(playerId);
        NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<UController>().PossessPawn(pawn, true);

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { playerId }
            }
        };

        SpawnPlayerPawnClientRpc(pawn.GetComponent<NetworkObject>(), clientRpcParams);
    }

    [ClientRpc]
    private void SpawnPlayerPawnClientRpc(NetworkObjectReference spawnedPlayerReference, ClientRpcParams clientParams = default)
    {
        spawnedPlayerReference.TryGet(out NetworkObject networkObject);
        _playerPawn = networkObject.GetComponent<ULevelPawn>();
        _playerController.PossessPawn(_playerPawn, true);
    }

    public override void OnDestroy()
    {
        InputContext.FindAction("AbilityMenu").canceled -= ShowAbilityMenu;
        InputContext.FindAction("MeleeMenu").canceled -= ShowMeleeMenu;

        networkManager.OnClientStopped -= NetworkManager_OnClientStopped;

        networkManager.OnClientConnectedCallback -= OnClientConnected;

        base.OnDestroy();
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnscriptedEngine;

public class GM_MultiplayerMode : UGameModeBase
{
    [Header("Multiplayer Extensions")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private List<Transform> spawnPoints;

    [SerializeField] private UIC_AbilityLoadout abilityLoadoutPrefab;
    [SerializeField] private UIC_MeleeLoadout meleeLoadOutPrefab;
    
    private UIC_AbilityLoadout _abilityLoadout;
    private UIC_MeleeLoadout _meleeLoadout;

    protected override void Init() { }

    protected override IEnumerator Start()
    {
        PlayerEvents.OnPlayerDied += OnPlayerDied;

        yield return base.Start();

        InputContext.FindAction("AbilityMenu").canceled += ShowAbilityMenu;
        InputContext.FindAction("MeleeMenu").canceled += ShowMeleeMenu;

        networkManager.OnClientConnectedCallback += OnClientConnected;
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

        networkManager.OnClientConnectedCallback -= OnClientConnected;

        base.OnDestroy();
    }
}
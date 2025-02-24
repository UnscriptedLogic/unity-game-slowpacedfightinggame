using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnscriptedEngine;

public class GM_MultiplayerMode : UGameModeBase
{
    [Header("Multiplayer Extensions")]
    [SerializeField] private NetworkManager networkManager;

    protected override void Init() { }

    protected override IEnumerator Start()
    {
        PlayerEvents.OnPlayerDied += OnPlayerDied;

        yield return base.Start();

        networkManager.OnClientConnectedCallback += OnClientConnected;
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
        ULevelPawn pawn = Instantiate(playerPawn, Vector3.zero, Quaternion.identity);
        pawn.GetComponent<NetworkObject>().SpawnWithOwnership(playerId);

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
        ULevelPawn pawn = Instantiate(playerPawn, Vector3.zero, Quaternion.identity);
        pawn.GetComponent<NetworkObject>().SpawnWithOwnership(playerId);

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
}
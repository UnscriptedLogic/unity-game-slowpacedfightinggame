using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AICreator : MonoBehaviour
{
    [SerializeField] private C_AIController aiControllerPrefab;
    [SerializeField] private P_DefaultPlayerPawn aiPawnPrefab;

    private C_AIController aiController;
    private P_DefaultPlayerPawn aiPawn;


    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnAIServerRpc()
    {

    }

    private void Server_SpawnAI()
    {
        aiController = Instantiate(aiControllerPrefab);
        aiPawn = Instantiate(aiPawnPrefab);

        aiPawn.GetComponent<NetworkObject>().Spawn();

        aiController.PossessPawn(aiPawn, true);
    }
}

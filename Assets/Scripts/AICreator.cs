using InteractionSystem;
using Unity.Netcode;
using UnityEngine;

public class AICreator : NetworkBehaviour
{
    [SerializeField] private C_AIController aiControllerPrefab;
    [SerializeField] private P_DefaultPlayerPawn aiPawnPrefab;

    private C_AIController aiController;
    private P_DefaultPlayerPawn aiPawn;

    public void SpawnAI(GameObject context)
    {
        RequestSpawnAIServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnAIServerRpc()
    {
        if (aiPawn && aiController != null) return;

        Server_SpawnAI();
    }

    private void Server_SpawnAI()
    {
        aiController = Instantiate(aiControllerPrefab);
        aiPawn = Instantiate(aiPawnPrefab);

        aiController.GetComponent<NetworkObject>().Spawn(true);
        aiPawn.GetComponent<NetworkObject>().Spawn(true);

        aiController.PossessPawn(aiPawn, true);
    }
}

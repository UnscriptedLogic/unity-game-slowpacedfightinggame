using System;
using Unity.Netcode;
using UnityEngine;

public class HookAbility : Ability
{
    [SerializeField] private Vector3 createOffset = new Vector3(0f, 1f, 1.5f);
    [SerializeField] private Hook hookPrefab;
    [SerializeField] private float castDelay;

    [SerializeField] private AnimationClip pullingAnim;
    [SerializeField] private AnimationClip slamInitiateAnim;
    [SerializeField] private AnimationClip slammingDownAnim;
    [SerializeField] private AnimationClip slamAnim;

    private ClientRpcParams clientRpcParams;
    private MovementComponent movementComponent;

    internal override void Client_Initialize(P_PlayerPawn context, PlayerAttackComponent attackComponent)
    {
        base.Client_Initialize(context, attackComponent);

        movementComponent = context.GetComponent<MovementComponent>();
    }

    internal override void Server_Initialize(P_DefaultPlayerPawn context)
    {
        base.Server_Initialize(context);

        if (IsServer)
        {
            uses.Value = 1;

            movementComponent = context.GetComponent<MovementComponent>();
        }
    }

    internal override void Server_OnCooldownFinished()
    {
        if (IsServer)
        {
            uses.Value = 1;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void RequestUseAbilityServerRpc(ServerRpcParams serverParams)
    {
        if (!CanUseAbility()) return;

        clientRpcParams = ClientSenderParams(serverParams);

        UseAbilityClientRpc(clientRpcParams);
        
        animatorComponent.Server_AbilityUpper(attackComponent.GetIndexFromAbility(this));
        uses.Value--;

        Invoke(nameof(Server_ThrowHook), castDelay);

        cooldown.Value = 12f;
    }

    internal void Server_ThrowHook()
    {
        if (!IsServer) return;

        Quaternion rotation = movementComponent.IsAirborne ? movementComponent.CameraRefTransform.rotation : PlayerRoot.rotation;
        GameObject hookObject = Instantiate(hookPrefab.gameObject, PlayerRoot.position + (rotation * createOffset), rotation);
        NetworkObject networkObject = hookObject.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(OwnerClientId);

        Hook hook = hookObject.GetComponent<Hook>();
        hook.Server_Initialize(OwnerClientId, PlayerRoot, movementComponent.IsAirborne, this);

        HookThrownClientRpc(networkObject);
    }

    [ClientRpc]
    private void UseAbilityClientRpc(ClientRpcParams clientRpcParams)
    {
        StartAbility(this);
    }

    [ClientRpc]
    private void HookThrownClientRpc(NetworkObjectReference hookReference)
    {
        if (hookReference.TryGet(out NetworkObject hookNO))
        {
            Hook hook = hookNO.GetComponent<Hook>();
            hook.Client_Initialize(PlayerRoot);
        }
    }
}

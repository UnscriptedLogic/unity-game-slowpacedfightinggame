using System;
using Unity.Netcode;
using UnityEngine;

public class HookAbility : Ability
{
    [SerializeField] private Vector3 createOffset = new Vector3(0f, 1f, 1.5f);
    [SerializeField] private Hook hookPrefab;
    [SerializeField] private AnimationClip throwAnimation;
    [SerializeField] private float castDelay;

    private ClientRpcParams clientRpcParams;

    protected override void Start()
    {
        base.Start();

        if (IsServer)
        {
            uses.Value = 1;
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
        
        animatorComponent.Ability2Upper();
        uses.Value--;

        Invoke(nameof(Server_ThrowHook), castDelay);

        cooldown.Value = 1f;
    }

    internal void Server_ThrowHook()
    {
        if (!IsServer) return;

        GameObject hookObject = Instantiate(hookPrefab.gameObject, GFXRoot.position + (GFXRoot.rotation * createOffset), GFXRoot.rotation);
        NetworkObject networkObject = hookObject.GetComponent<NetworkObject>();
        networkObject.Spawn();

        Hook hook = hookObject.GetComponent<Hook>();
        hook.Server_Initialize(OwnerClientId, PlayerRoot);

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

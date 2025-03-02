using System;
using Unity.Netcode;
using UnityEngine;

public class HookAbility : Ability
{
    [SerializeField] private Vector3 createOffset = new Vector3(0f, 1f, 1.5f);
    [SerializeField] private Hook hookPrefab;
    [SerializeField] private AnimationClip throwAnimation;

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

        animatorComponent.Ability2Upper();
        uses.Value--;

        cooldown.Value = 1f;

        UseAbilityClientRpc(clientRpcParams);
    }

    internal void ThrowHook()
    {
        if (!IsServer) return;

        GameObject hookObject = Instantiate(hookPrefab.gameObject, transform.position + (transform.rotation * createOffset), transform.rotation);
        NetworkObject networkObject = hookObject.GetComponent<NetworkObject>();
        networkObject.Spawn();

        Hook hook = hookObject.GetComponent<Hook>();
        hook.Server_Initialize(OwnerClientId, transform.parent);

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
            hook.Client_Initialize(transform.parent);
        }
    }

}

using System;
using Unity.Netcode;
using UnityEngine;

public class DefaultCubeAbility : Ability
{
    [SerializeField] private Vector3 createOffset = new Vector3(0f, 1f, 1.5f);
    [SerializeField] private AnimationClip throwAnimation;
    [SerializeField] private DefaultCube defaultCubePrefab;

    protected override void Start()
    {
        base.Start();

        if (IsServer)
        {
            uses.Value = 2;
        }
    }

    internal override void Server_OnCooldownFinished()
    {
        if (IsServer)
        {
            uses.Value = 2;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void RequestUseAbilityServerRpc(ServerRpcParams serverParams)
    {
        if (stateComponent.HasStatusEffect(StatusEffect.Type.Stun)) return;
        if (cooldown.Value > 0) return;
        if (uses.Value <= 0) return;

        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { serverParams.Receive.SenderClientId }
            }
        };

        animatorComponent.Ability1();
        uses.Value--;

        if (uses.Value > 0)
        {
            cooldown.Value = throwAnimation.length;
        }
        else
        {
            cooldown.Value = 15f;
        }

        UseAbilityClientRpc(clientParams);
    }

    internal void ThrowCube()
    {
        if (IsServer)
        {
            GameObject cube = Instantiate(defaultCubePrefab.gameObject, transform.position + (transform.rotation * createOffset), transform.rotation);
            NetworkObject networkObject = cube.GetComponent<NetworkObject>();
            networkObject.Spawn();

            Rigidbody cubeRb = cube.GetComponent<Rigidbody>();
            cubeRb.isKinematic = false;
            cubeRb.AddForce(transform.forward * 10f, ForceMode.Impulse);
        }
    }

    [ClientRpc]
    private void UseAbilityClientRpc(ClientRpcParams clientParams)
    {
        StartAbility(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 pos = transform.position + (transform.rotation * createOffset);
        Gizmos.DrawWireSphere(pos, 0.5f);
    }
}

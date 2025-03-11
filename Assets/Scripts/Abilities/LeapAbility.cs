using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LeapAbility : Ability
{
    [SerializeField] private float jumpForce;
    [SerializeField] private AudioClip jumpSFX;

    internal override void Server_Initialize(P_DefaultPlayerPawn context)
    {
        base.Server_Initialize(context);

        if (IsServer)
        {
            uses.Value = 1;
        }
    }

    internal override void Server_OnCooldownFinished()
    {
        if (IsServer)
        {
            if (uses.Value == 0)
            {
                uses.Value = 1;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void RequestUseAbilityServerRpc(ServerRpcParams serverParams)
    {
        base.RequestUseAbilityServerRpc(serverParams);

        if (!CanUseAbility()) return;
        stateComponent.Server_RemoveStatusEffect(StatusEffect.Type.Stun);
        animatorComponent.Server_AbilityLower(attackComponent.GetIndexFromAbility(this));

        uses.Value--;
        cooldown.Value = 10f;

        UseAbilityClientRpc(ClientSenderParams(serverParams));
    }

    internal override bool CanUseAbility()
    {
        if (stateComponent.HasStatusEffect(StatusEffect.Type.Silence)) return false;
        if (cooldown.Value > 0) return false;
        if (uses.Value <= 0) return false;

        return true;
    }

    [ClientRpc]
    private void UseAbilityClientRpc(ClientRpcParams clientParams)
    {
        context.Rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        AudioHandler.instance.PlayAudio(jumpSFX, transform.position, new AudioHandler.Settings()
        {
            volume = 0.5f
        });

        StartAbility(this);
    }
}
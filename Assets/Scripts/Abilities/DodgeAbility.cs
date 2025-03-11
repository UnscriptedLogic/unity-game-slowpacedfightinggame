using Unity.Netcode;
using UnityEngine;

public class DodgeAbility : Ability
{
    [SerializeField] private Color outlineColor;
    [SerializeField] private float outlineThickness;

    private float dodgeDuration = 1.5f;

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
        base.Server_OnCooldownFinished();

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

        stateComponent.Server_AddStatusEffect(new StatusEffect()
        {
            type = StatusEffect.Type.Silence,
            duration = dodgeDuration
        });

        stateComponent.Server_AddStatusEffect(new StatusEffect()
        {
            type = StatusEffect.Type.Invincible,
            duration = dodgeDuration
        });

        uses.Value--;
        cooldown.Value = 10;

        UseAbilityClientRpc();
    }

    [ClientRpc]
    private void UseAbilityClientRpc()
    {
        Outline outline = context.gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineThickness;

        Destroy(outline, dodgeDuration);

        StartAbility(this);
    }
}
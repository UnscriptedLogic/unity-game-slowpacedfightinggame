using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackComponent : PlayerBaseComponent
{
    public enum States
    {
        Idle,
        Attacking,
        Stunned
    }

    [SerializeField] private Ability meleeAbility;
    [SerializeField] private Ability ability1;
    [SerializeField] private Ability ability2;
    [SerializeField] private TriggerHandler meleeHitbox;

    [SerializeField] private UIC_AbilityHUD abilityHUDPrefab;

    private PlayerStateComponent playerStateComponent;

    private int abilityIndex;
    private Ability currentAbility;
    private States currentState;

    private UIC_AbilityHUD abilityHUD;

    public TriggerHandler MeleeHitbox => meleeHitbox;

    public override void Initialize(P_PlayerPawn context)
    {
        base.Initialize(context);

        playerStateComponent = GetComponent<PlayerStateComponent>();

        meleeAbility.Initialize(context, this);
        meleeAbility.OnStarted += OnAbilityStarted;
        meleeAbility.OnFinished += OnAbilityFinished;

        abilityHUD = context.AttachUIWidget(abilityHUDPrefab);
        abilityHUD.Initialize(meleeAbility, ability1, ability2);

        context.GetDefaultInputMap().FindAction("Ability1").performed += OnAbility1;
        context.GetDefaultInputMap().FindAction("Ability2").performed += OnAbility2;
    }

    private void OnAbility2(InputAction.CallbackContext context)
    {
        AbilityConfig(2);
    }

    private void OnAbility1(InputAction.CallbackContext context)
    {
        AbilityConfig(1);
    }

    public override void OnDefaultLeftMouseDown(out bool swallowInput)
    {
        base.OnDefaultLeftMouseDown(out swallowInput);

        AbilityConfig(0);
    }

    private void AbilityConfig(int abilityIndex)
    {
        if (IsClient)
        {
            ServerRpcParams serverParams = new ServerRpcParams
            {
                Receive = new ServerRpcReceiveParams
                {
                    SenderClientId = OwnerClientId
                }
            };

            RequestUseAbilityServerRpc(abilityIndex, serverParams);
        }

        if (IsServer)
        {
            if (currentState != States.Idle) return;
            UseAbilityClientRpc(abilityIndex, new ClientRpcParams());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUseAbilityServerRpc(int abilityIndex, ServerRpcParams serverParams)
    {
        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { serverParams.Receive.SenderClientId }
            }
        };

        if (currentState != States.Idle) return;

        UseAbilityClientRpc(abilityIndex, clientParams);
    }

    [ClientRpc]
    private void UseAbilityClientRpc(int abilityIndex, ClientRpcParams clientParams)
    {
        ServerRpcParams serverParams = new ServerRpcParams
        {
            Receive = new ServerRpcReceiveParams
            {
                SenderClientId = OwnerClientId
            }
        };

        switch (abilityIndex)
        {
            case 0:
                meleeAbility.RequestUseAbilityServerRpc(serverParams);
                break;
            case 1:
                ability1.RequestUseAbilityServerRpc(serverParams);
                break;
            case 2:
                ability2.RequestUseAbilityServerRpc(serverParams);
                break;
        }
    }    

    public override void UpdateTick(out bool swallowTick)
    {
        swallowTick = false;

        if (currentAbility != null)
        {
            currentAbility.UpdateTick();
        }
    }

    public override void FixedUpdateTick(out bool swallowTick)
    {
        swallowTick = false;

        if (currentAbility != null)
        {
            currentAbility.FixedUpdateTick();
        }
    }

    private void OnAbilityStarted(Ability ability)
    {
        currentAbility = ability;
        currentState = States.Attacking;
    }

    private void OnAbilityFinished(Ability ability)
    {
        currentAbility = null;
        currentState = States.Idle;
    }

    public override void DeInitialize(P_PlayerPawn context)
    {
        context.DettachUIWidget(abilityHUD);

        base.DeInitialize(context);
    }
}

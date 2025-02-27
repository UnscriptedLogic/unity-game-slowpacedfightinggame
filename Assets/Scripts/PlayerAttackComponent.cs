using Unity.Netcode;
using UnityEngine;

public class PlayerAttackComponent : PlayerBaseComponent
{
    public enum States
    {
        Idle,
        Attacking,
        Stunned
    }

    [SerializeField] private Ability meleeAbility;
    [SerializeField] private TriggerHandler meleeHitbox;

    private PlayerStateComponent playerStateComponent;

    private Ability currentAbility;
    private States currentState;

    public TriggerHandler MeleeHitbox => meleeHitbox;

    public override void Initialize(P_PlayerPawn context)
    {
        base.Initialize(context);

        playerStateComponent = GetComponent<PlayerStateComponent>();

        meleeAbility.Initialize(context, this);
        meleeAbility.OnStarted += OnAbilityStarted;
        meleeAbility.OnFinished += OnAbilityFinished;
    }

    public override void OnDefaultLeftMouseDown(out bool swallowInput)
    {
        base.OnDefaultLeftMouseDown(out swallowInput);

        if (IsClient)
        {
            ServerRpcParams serverParams = new ServerRpcParams
            {
                Receive = new ServerRpcReceiveParams
                {
                    SenderClientId = OwnerClientId
                }
            };

            RequestUseAbilityServerRpc(serverParams);
        }

        if (IsServer)
        {
            if (currentState != States.Idle) return;
            UseAbilityClientRpc(new ClientRpcParams());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUseAbilityServerRpc(ServerRpcParams serverParams)
    {
        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { serverParams.Receive.SenderClientId }
            }
        };

        if (currentState != States.Idle) return;

        UseAbilityClientRpc(clientParams);
    }

    [ClientRpc]
    private void UseAbilityClientRpc(ClientRpcParams clientParams)
    {
        ServerRpcParams serverParams = new ServerRpcParams
        {
            Receive = new ServerRpcReceiveParams
            {
                SenderClientId = OwnerClientId
            }
        };

        meleeAbility.RequestUseAbilityServerRpc(serverParams);
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
}

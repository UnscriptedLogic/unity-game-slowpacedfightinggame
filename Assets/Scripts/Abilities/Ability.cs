using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnscriptedEngine;

public abstract class Ability : NetworkBehaviour
{
    [SerializeField] private string abilityName;

    protected P_PlayerPawn context;
    protected PlayerAttackComponent attackComponent;
    protected PlayerStateComponent stateComponent;
    protected PlayerAudioComponent audioComponent;
    protected PlayerAnimator animatorComponent;

    public NetworkVariable<int> uses = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> cooldown = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<Ability> OnStarted;
    public event Action<Ability> OnFinished;

    public string AbilityName => abilityName;

    private Transform playerRoot;

    public Transform PlayerRoot
    {
        get
        {
            if (playerRoot == null)
            {
                if (IsServer)
                {
                    C_PlayerController controller = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<C_PlayerController>();
                    playerRoot = controller.GetPossessedPawn<P_DefaultPlayerPawn>().transform;
                }

                if (IsClient)
                {
                    playerRoot = UGameModeBase.instance.GetPlayerPawn().transform;
                }
            }

            return playerRoot;
        }
    }

    protected virtual void Start()
    {
        if (!IsOwner && IsClient)
        {
            ServerRpcParams serverRpcParams = new ServerRpcParams()
            {
                Receive =
                {
                    SenderClientId = OwnerClientId
                }
            };

            GetSelfServerRpc(serverRpcParams);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetSelfServerRpc(ServerRpcParams serverParams)
    {
        C_PlayerController controller = NetworkManager.ConnectedClients[serverParams.Receive.SenderClientId].PlayerObject.GetComponent<C_PlayerController>();
        NetworkObject pawnNO = controller.GetPossessedPawn<P_DefaultPlayerPawn>().GetComponent<NetworkObject>();
        GetSelfClientRpc(pawnNO);
    }

    [ClientRpc]
    private void GetSelfClientRpc(NetworkObjectReference networkObjectReference)
    {
        networkObjectReference.TryGet(out NetworkObject networkObject);
        context = networkObject.GetComponent<P_DefaultPlayerPawn>();
        playerRoot = networkObject.transform;

        attackComponent = context.GetComponentInChildren<PlayerAttackComponent>();
        stateComponent = context.GetComponent<PlayerStateComponent>();
        audioComponent = context.GetComponent<PlayerAudioComponent>();
        animatorComponent = context.GetComponentInChildren<PlayerAnimator>();

        attackComponent.OnAbilityApexed += OnAbilityApexed;
    }

    internal virtual void Server_Initialize(P_DefaultPlayerPawn context)
    {
        if (!IsServer) return;
        this.context = context;
        playerRoot = context.transform;

        attackComponent = context.GetComponentInChildren<PlayerAttackComponent>();
        stateComponent = context.GetComponent<PlayerStateComponent>();
        audioComponent = context.GetComponent<PlayerAudioComponent>();
        animatorComponent = context.GetComponentInChildren<PlayerAnimator>();

        attackComponent.OnAbilityApexed += OnAbilityApexed;
    }

    protected virtual void OnAbilityApexed(Ability ability) { }

    public virtual void Client_Initialize(P_PlayerPawn context, PlayerAttackComponent attackComponent)
    {
        this.context = context;
        this.attackComponent = attackComponent;

        stateComponent = context.GetComponent<PlayerStateComponent>();
        audioComponent = context.GetComponent<PlayerAudioComponent>();
        animatorComponent = context.GetComponentInChildren<PlayerAnimator>();

        attackComponent.OnAbilityApexed += OnAbilityApexed;
    }

    protected virtual void Update()
    {
        if (IsServer)
        {
            if (cooldown.Value > 0)
            {
                cooldown.Value -= Time.deltaTime;
                if (cooldown.Value <= 0)
                {
                    FinishAbility(this);
                    Server_OnCooldownFinished();
                }
            }
        }
    }

    internal virtual bool CanUseAbility()
    {
        if (stateComponent.HasStatusEffect(StatusEffect.Type.Stun)) return false;
        if (cooldown.Value > 0) return false;
        if (uses.Value <= 0) return false;

        return true;
    }

    internal virtual ClientRpcParams ClientSenderParams(ServerRpcParams serverParams)
    {
        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { serverParams.Receive.SenderClientId }
            }
        };

        return clientParams;
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void RequestUseAbilityServerRpc(ServerRpcParams serverParams) { }

    internal virtual void UpdateTick() { }
    internal virtual void FixedUpdateTick() { }

    internal virtual void Server_OnCooldownFinished() { }

    public virtual void StartAbility(Ability ability)
    {
        OnStarted?.Invoke(ability);
    }

    public virtual void FinishAbility(Ability ability)
    {
        OnFinished?.Invoke(ability);
    }
}

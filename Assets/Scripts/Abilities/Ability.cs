using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Ability : NetworkBehaviour
{
    [SerializeField] private int abilityId;
    [SerializeField] private string abilityName;
    [SerializeField] private AnimationClip abilityAnimation;

    protected P_PlayerPawn context;
    protected PlayerAttackComponent attackComponent;
    protected PlayerStateComponent stateComponent;
    protected PlayerAudioComponent audioComponent;
    protected PlayerAnimator animatorComponent;

    public NetworkVariable<int> uses = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> cooldown = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<Ability> OnStarted;
    public event Action<Ability> OnFinished;

    public int AbilityId => abilityId;
    public string AbilityName => abilityName;
    public AnimationClip AbilityAnimation => abilityAnimation;

    private Transform playerRoot;

    public Transform PlayerRoot => playerRoot;

    protected virtual void Start()
    {
        if (!IsOwner && IsClient)
        {
            ServerRpcParams serverRpcParams = new ServerRpcParams()
            {
                Receive =
                {
                    SenderClientId = NetworkManager.LocalClientId,
                }
            };

            GetSelfServerRpc(OwnerClientId, serverRpcParams);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetSelfServerRpc(ulong ownerCliendID, ServerRpcParams serverParams)
    {
        C_PlayerController controller = NetworkManager.ConnectedClients[ownerCliendID].PlayerObject.GetComponent<C_PlayerController>();
        NetworkObject pawnNO = controller.GetPossessedPawn<P_DefaultPlayerPawn>().GetComponent<NetworkObject>();

        ClientRpcParams clientParams = ClientSenderParams(serverParams);

        GetSelfClientRpc(pawnNO, clientParams);
    }

    [ClientRpc]
    private void GetSelfClientRpc(NetworkObjectReference networkObjectReference, ClientRpcParams clientParams)
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

    internal virtual void Client_Initialize(P_PlayerPawn context, PlayerAttackComponent attackComponent)
    {
        this.context = context;
        this.attackComponent = attackComponent;
        playerRoot = context.transform;

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

    public override void OnDestroy()
    {
        if (attackComponent != null)
        {
            attackComponent.OnAbilityApexed -= OnAbilityApexed;
        }

        base.OnDestroy();
    }
}

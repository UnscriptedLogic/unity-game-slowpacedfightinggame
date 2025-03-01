using System;
using Unity.Netcode;
using UnityEngine;

public abstract class Ability : NetworkBehaviour
{
    protected P_PlayerPawn context;
    protected PlayerAttackComponent attackComponent;
    protected PlayerStateComponent stateComponent;
    protected PlayerAudioComponent audioComponent;
    protected PlayerAnimator animatorComponent;

    public NetworkVariable<int> uses = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> cooldown = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<Ability> OnStarted;
    public event Action<Ability> OnFinished;

    protected virtual void Start()
    {
        stateComponent = transform.parent.GetComponent<PlayerStateComponent>();
        audioComponent = transform.parent.GetComponent<PlayerAudioComponent>();
        animatorComponent = transform.GetComponent<PlayerAnimator>();
        attackComponent = GetComponent<PlayerAttackComponent>();
    }

    public virtual void Initialize(P_PlayerPawn context, PlayerAttackComponent attackComponent)
    {
        if (IsOwner)
        {
            this.context = context;
            this.attackComponent = attackComponent;
        }
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

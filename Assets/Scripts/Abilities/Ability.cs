using System;
using Unity.Netcode;

public abstract class Ability : NetworkBehaviour
{
    protected P_PlayerPawn context;
    protected PlayerAttackComponent attackComponent;

    public event Action<Ability> OnStarted;
    public event Action<Ability> OnFinished;

    public virtual void Initialize(P_PlayerPawn context, PlayerAttackComponent attackComponent)
    {
        this.context = context;
        this.attackComponent = attackComponent;
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void RequestUseAbilityServerRpc(ServerRpcParams serverParams) { }

    public virtual void UpdateTick() { }
    public virtual void FixedUpdateTick() { }

    public virtual void StartAbility(Ability ability)
    {
        OnStarted?.Invoke(ability);
    }

    public virtual void FinishAbility(Ability ability)
    {
        OnFinished?.Invoke(ability);
    }
}

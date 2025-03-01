using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerAnimator : PlayerBaseComponent
{
    [SerializeField] private Animator animator;
    [SerializeField] private ClientNetworkAnimator networkAnimator;
    [SerializeField] private PlayerStateComponent playerStateComponent;

    public Animator Animator => animator;
    public NetworkAnimator NetworkAnimator => networkAnimator;

    private void Start()
    {
        if (IsServer)
        {
            playerStateComponent = transform.parent.GetComponent<PlayerStateComponent>();
            playerStateComponent.StatusEffects.OnListChanged += StatusEffects_OnListChanged;
        }
    }

    private void StatusEffects_OnListChanged(NetworkListEvent<StatusEffect> changeEvent)
    {
        if (playerStateComponent.HasStatusEffect(StatusEffect.Type.Stun))
        {
            networkAnimator.SetTrigger("LowerStunned");
            networkAnimator.SetTrigger("UpperStunned");
        }

        if (playerStateComponent.StatusEffects.Count <= 0)
        {
            networkAnimator.SetTrigger("FinishLowerStun");
            networkAnimator.SetTrigger("FinishUpperStun");
        }
    }

    public override void OnMove(Vector2 inputDir, out bool swallowInput)
    {
        base.OnMove(inputDir, out swallowInput);

        AnimateWalkServerRpc(inputDir);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AnimateWalkServerRpc(Vector2 inputDir)
    {
        animator.SetFloat("Speed", inputDir.magnitude);
    }

    internal void Attack1()
    {
        networkAnimator.SetTrigger("Attack1");
    }

    internal void Attack2()
    {
        networkAnimator.SetTrigger("Attack2");
    }

    internal void Attack3()
    {
        networkAnimator.SetTrigger("Attack3");
    }

    internal void Ability1()
    {
        networkAnimator.SetTrigger("Ability1");
    }
}
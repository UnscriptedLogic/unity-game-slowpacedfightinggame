using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnscriptedEngine;

public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
{
    public AnimationClipOverrides(int capacity) : base(capacity) { }

    public AnimationClip this[string name]
    {
        get { return this.Find(x => x.Key.name.Equals(name)).Value; }
        set
        {
            int index = this.FindIndex(x => x.Key.name.Equals(name));
            if (index != -1)
                this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
        }
    }
}

[Serializable]
public class SyncClientAnimParams : INetworkSerializable
{
    public string ability1ClipName;
    public string ability2ClipName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ability1ClipName);
        serializer.SerializeValue(ref ability2ClipName);
    }
}

public class PlayerAnimator : PlayerBaseComponent
{
    [SerializeField] private Animator animator;
    [SerializeField] private ClientNetworkAnimator networkAnimator;
    [SerializeField] private PlayerStateComponent playerStateComponent;
    [SerializeField] private AnimatorOverrideController overrideControllerPrefab;
    [SerializeField] private AnimationsSO allAnimationsContainer;
    [SerializeField] private PlayerAttackComponent attackComponent;

    private AnimatorOverrideController overrideController;
    private AnimationClipOverrides clipOverrides;
    private CustomGameInstance customGameInstance;

    public Animator Animator => animator;
    public NetworkAnimator NetworkAnimator => networkAnimator;

    //public NetworkVariable<FixedString128Bytes> ability1AnimClip = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //public NetworkVariable<FixedString128Bytes> ability2AnimClip = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        customGameInstance = UGameModeBase.instance.GetGameInstance<CustomGameInstance>();

        if (IsServer)
        {
            playerStateComponent = transform.parent.GetComponent<PlayerStateComponent>();
            playerStateComponent.StatusEffects.OnListChanged += StatusEffects_OnListChanged;
        }
    }

    private void OnAbility1Changed(int previousValue, int newValue)
    {
        clipOverrides["Ability1Upper"] = customGameInstance.AbilityMap.GetAbilityByID(newValue).AbilityAnimation;
        overrideController.ApplyOverrides(clipOverrides);
    }

    private void OnAbility2Changed(int previousValue, int newValue)
    {
        clipOverrides["Ability2Upper"] = customGameInstance.AbilityMap.GetAbilityByID(newValue).AbilityAnimation;
        overrideController.ApplyOverrides(clipOverrides);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        playerStateComponent.StatusEffects.OnListChanged -= StatusEffects_OnListChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        customGameInstance = UGameModeBase.instance.GetGameInstance<CustomGameInstance>();

        overrideController = new AnimatorOverrideController(overrideControllerPrefab);
        animator.runtimeAnimatorController = overrideController;
        clipOverrides = new AnimationClipOverrides(overrideController.overridesCount);
        overrideController.GetOverrides(clipOverrides);

        clipOverrides["Ability1Upper"] = customGameInstance.AbilityMap.GetAbilityByID(attackComponent.ability1Id.Value).AbilityAnimation;
        clipOverrides["Ability2Upper"] = customGameInstance.AbilityMap.GetAbilityByID(attackComponent.ability2Id.Value).AbilityAnimation;
        overrideController.ApplyOverrides(clipOverrides);

        attackComponent.ability1Id.OnValueChanged += OnAbility1Changed;
        attackComponent.ability2Id.OnValueChanged += OnAbility2Changed;
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

    internal void Server_AbilityUpper(int abilityIndex)
    {
        networkAnimator.SetTrigger($"Ability{abilityIndex}Upper");
    }
}
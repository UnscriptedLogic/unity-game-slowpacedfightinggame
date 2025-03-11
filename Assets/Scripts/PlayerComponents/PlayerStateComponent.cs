using System;
using Unity.Netcode;
using UnityEngine;

public struct StatusEffect : IEquatable<StatusEffect>, INetworkSerializable
{
    public enum Type
    {
        Stun,
        Slow,
        Root,
        Silence,
        TickingDamage,
        Invincible
    }

    public Type type;
    public float duration;

    public StatusEffect(Type type, float duration)
    {
        this.type = type;
        this.duration = duration;
    }

    public bool IsExpired()
    {
        return duration <= 0;
    }

    public bool Equals(StatusEffect other)
    {
        return type == other.type;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref duration);
    }

    public int TypeToInt()
    {
        return (int)type;
    }

    public static Type IntToType(int type)
    {
        return (StatusEffect.Type)type;
    }
}

public class PlayerStateComponent : PlayerBaseComponent
{
    private NetworkList<StatusEffect> statusEffects;

    public NetworkList<StatusEffect> StatusEffects => statusEffects;

    private void Awake()
    {
        statusEffects = new NetworkList<StatusEffect>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    }

    public override void Initialize(P_PlayerPawn context)
    {
        base.Initialize(context);
    }

    public void Server_AddStatusEffect(StatusEffect statusEffect)
    {
        if (HasStatusEffect(StatusEffect.Type.Invincible)) return;

        statusEffects.Add(statusEffect);
    }

    private void Update()
    {
        if (IsServer)
        {
            for (int i = statusEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect statusEffect = statusEffects[i];
                statusEffect.duration -= Time.deltaTime;

                if (statusEffect.IsExpired())
                {
                    statusEffects.RemoveAt(i);
                    continue;
                }

                statusEffects[i] = statusEffect;
            }
        }
    }

    public bool HasStatusEffect(StatusEffect.Type type)
    {
        foreach (StatusEffect statusEffect in statusEffects)
        {
            if (statusEffect.type == type)
            {
                return true;
            }
        }
        return false;
    }

    internal void Server_RemoveStatusEffect(StatusEffect.Type status)
    {
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            if (statusEffects[i].type == status)
            {
                statusEffects.Add(statusEffects[i]);
                return;
            }
        }
    }
}

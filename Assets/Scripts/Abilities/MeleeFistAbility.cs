using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnscriptedEngine;

[System.Serializable]
public class VFXSettings
{
    public GameObject vfx;
    public Vector3 position;
    public Vector3 rotation;
    public float delay;
    public float cleanUpTimer;
}

public class MeleeFistAbility : Ability
{
    [SerializeField] private AnimationClip attack1;
    [SerializeField] private AnimationClip attack2;
    [SerializeField] private AnimationClip attack3;
    [SerializeField] private List<AudioClip> hitSFXes;

    [Header("VFX Settings")]
    [SerializeField] private VFXSettings attack1VFX;
    [SerializeField] private VFXSettings attack2VFX;
    [SerializeField] private VFXSettings attack3VFX;
    [SerializeField] private GameObject hitLandedVFXPrefab;

    private CinemachineImpulseSource impulseSource;

    private List<P_PlayerPawn> alreadyHit = new List<P_PlayerPawn>();

    internal override void Server_Initialize(P_DefaultPlayerPawn context)
    {
        base.Server_Initialize(context);

        attackComponent.MeleeHitbox.TriggerEnter += OnHitboxTriggerEnter;
        impulseSource = attackComponent.GetComponent<CinemachineImpulseSource>();
        uses.Value = 2;
    }

    protected override void OnAbilityApexed(Ability ability)
    {
        if (ability == null) return;

        if (ability.AbilityName == AbilityName)
        {
            if (uses.Value == 0)
            {
                PlayVFX1();
            }
            else if (uses.Value == 1)
            {
                PlayVFX2();
            }
            else if (uses.Value == 2)
            {
                PlayVFX3();
            }
        }
    }

    internal override void Client_Initialize(P_PlayerPawn context, PlayerAttackComponent attackComponent)
    {
        base.Client_Initialize(context, attackComponent);

        if (IsOwner)
        {
            impulseSource = attackComponent.GetComponent<CinemachineImpulseSource>();
            attackComponent.MeleeHitbox.TriggerEnter += OnHitboxTriggerEnter;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        attackComponent.MeleeHitbox.TriggerEnter -= OnHitboxTriggerEnter;
    }

    private void OnHitboxTriggerEnter(object sender, Collider e)
    {
        if (e.TryGetComponent(out P_PlayerPawn target))
        {
            if (target == context) return;
            if (alreadyHit.Contains(target)) return;

            float force = 1500f;
            float impulse = 0.75f;
            float damage = 5f;
            float kbDuration = 0.1f;
            float stunDuration = 1f;

            if (uses.Value == 1)
            {
                impulse = 1f;
                force = 2000f;
                damage = 10f;
                stunDuration = 1.5f;
            }

            if (uses.Value == 2)
            {
                impulse = 1.5f;
                force = 2500f;
                damage = 15f;
                stunDuration = 0.5f;
                kbDuration = 0.25f;
            }

            if (IsServer)
            {
                OnHitClientRpc();

                target.GetPlayerComponent<PlayerHealthComponent>().Server_TakeDamage(new DamageSettings()
                {
                    damage = damage,
                    kbDir = (target.transform.position - PlayerRoot.position).normalized,
                    kbForce = force,
                    kbDuration = kbDuration,
                });

                target.GetPlayerComponent<PlayerStateComponent>().Server_AddStatusEffect(new StatusEffect()
                {
                    type = StatusEffect.Type.Stun,
                    duration = stunDuration
                });
            }

            impulseSource.GenerateImpulse(impulse);

            GameObject hitLanded = Instantiate(hitLandedVFXPrefab, target.transform.position, Quaternion.identity);
            hitLanded.transform.forward = PlayerRoot.forward;
            Destroy(hitLanded, 1f);

            alreadyHit.Add(target);
        }
    }

    [ClientRpc]
    private void OnHitClientRpc()
    {
        if (uses.Value == 2)
        {
            audioComponent.PlayAudio(hitSFXes[1], 0.5f);
            return;
        }

        audioComponent.PlayAudio(hitSFXes[0], 0.2f);
    }

    [ServerRpc(RequireOwnership = false)]
    public override void RequestUseAbilityServerRpc(ServerRpcParams serverParams)
    {
        if (stateComponent.HasStatusEffect(StatusEffect.Type.Stun)) return;
        if (stateComponent.HasStatusEffect(StatusEffect.Type.Silence)) return;
        if (cooldown.Value > 0) return;

        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { serverParams.Receive.SenderClientId }
            }
        };

        alreadyHit.Clear();

        if (uses.Value == 2)
        {
            animatorComponent.Attack1();
            cooldown.Value = attack1.length;
        }
        else if (uses.Value == 0)
        {
            animatorComponent.Attack2();
            cooldown.Value = attack2.length;
        }
        else if (uses.Value == 1)
        {
            animatorComponent.Attack3();
            cooldown.Value = attack3.length;
        }

        uses.Value = (uses.Value + 1) % 3;

        UseAbilityClientRpc(clientParams);
    }

    [ClientRpc]
    private void UseAbilityClientRpc(ClientRpcParams clientParams)
    {
        alreadyHit.Clear();

        StartAbility(this);
    }

    public void PlayVFX1() => StartCoroutine(PlayVFX(attack1VFX));
    public void PlayVFX2() => StartCoroutine(PlayVFX(attack2VFX));
    public void PlayVFX3() => StartCoroutine(PlayVFX(attack3VFX));

    private IEnumerator PlayVFX(VFXSettings settings)
    {
        yield return new WaitForSeconds(settings.delay);

        //position is relative to the forward direction of the player
        Vector3 pos = PlayerRoot.position + (PlayerRoot.rotation * settings.position);

        GameObject vfx = Instantiate(settings.vfx, pos, PlayerRoot.rotation * Quaternion.Euler(settings.rotation));
        Destroy(vfx, settings.cleanUpTimer);
    }
}
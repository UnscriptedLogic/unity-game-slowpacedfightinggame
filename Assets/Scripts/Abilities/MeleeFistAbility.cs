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
    private NetworkVariable<int> attackCount = new NetworkVariable<int>(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<P_PlayerPawn> alreadyHit = new List<P_PlayerPawn>();

    protected override void Start()
    {
        base.Start();

        if (IsServer)
        {
            attackComponent.MeleeHitbox.TriggerEnter += OnHitboxTriggerEnter;
            impulseSource = attackComponent.GetComponent<CinemachineImpulseSource>();
        }
    }

    public override void Initialize(P_PlayerPawn context, PlayerAttackComponent attackComponent)
    {
        base.Initialize(context, attackComponent);

        if (IsOwner)
        {
            impulseSource = attackComponent.GetComponent<CinemachineImpulseSource>();
            attackComponent.MeleeHitbox.TriggerEnter += OnHitboxTriggerEnter;
        }
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

            if (attackCount.Value == 1)
            {
                impulse = 1f;
                force = 2000f;
                damage = 10f;
                stunDuration = 1.5f;
            }

            if (attackCount.Value == 2)
            {
                impulse = 1.5f;
                force = 2500f;
                damage = 15f;
                stunDuration = 0.5f;
                kbDuration = 0.25f;
            }

            if (IsServer)
            {
                target.GetPlayerComponent<PlayerHealthComponent>().Server_TakeDamage(new DamageSettings()
                {
                    damage = damage,
                    kbDir = (target.transform.position - transform.position).normalized,
                    kbForce = force,
                    kbDuration = kbDuration,
                });

                target.GetPlayerComponent<PlayerStateComponent>().Server_AddStatusEffect(new StatusEffect()
                {
                    type = StatusEffect.Type.Stun,
                    duration = stunDuration
                });

                OnHitClientRpc();
            }

            impulseSource.GenerateImpulse(impulse);

            GameObject hitLanded = Instantiate(hitLandedVFXPrefab, target.transform.position, Quaternion.identity);
            hitLanded.transform.forward = transform.forward;
            Destroy(hitLanded, 1f);

            alreadyHit.Add(target);
        }
    }


    [ClientRpc]
    private void OnHitClientRpc()
    {
        audioComponent.PlayAudio(hitSFXes.GetRandomElement(), 0.2f);
    }

    [ServerRpc(RequireOwnership = false)]
    public override void RequestUseAbilityServerRpc(ServerRpcParams serverParams)
    {
        if (stateComponent.HasStatusEffect(StatusEffect.Type.Stun)) return;
        if (cooldown.Value > 0) return;

        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { serverParams.Receive.SenderClientId }
            }
        };

        alreadyHit.Clear();

        if (attackCount.Value == 2)
        {
            animatorComponent.Attack1();
            cooldown.Value = attack1.length;
        }
        else if (attackCount.Value == 0)
        {
            animatorComponent.Attack2();
            cooldown.Value = attack2.length;
        }
        else if (attackCount.Value == 1)
        {
            animatorComponent.Attack3();
            cooldown.Value = attack3.length;
        }

        attackCount.Value = (attackCount.Value + 1) % 3;

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
        Vector3 pos = transform.position + (transform.rotation * settings.position);

        GameObject vfx = Instantiate(settings.vfx, pos, transform.rotation * Quaternion.Euler(settings.rotation));
        Destroy(vfx, settings.cleanUpTimer);
    }
}
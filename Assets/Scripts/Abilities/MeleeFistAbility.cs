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

    private CinemachineImpulseSource impulseSource;
    private NetworkVariable<int> attackCount = new NetworkVariable<int>(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerAnimator playerAnimator;
    private PlayerAudioComponent playerAudioComponent;
    private PlayerStateComponent playerStateComponent;

    private float animationTime;

    private List<P_PlayerPawn> alreadyHit = new List<P_PlayerPawn>();

    private void Start()
    {
        if (IsServer)
        {
            playerStateComponent = transform.parent.GetComponent<PlayerStateComponent>();
            playerAudioComponent = transform.parent.GetComponent<PlayerAudioComponent>();
            playerAnimator = transform.GetComponent<PlayerAnimator>();

            attackComponent = GetComponent<PlayerAttackComponent>();
            attackComponent.MeleeHitbox.TriggerEnter += OnHitboxTriggerEnter;
            impulseSource = attackComponent.GetComponent<CinemachineImpulseSource>();
        }
    }

    public override void Initialize(P_PlayerPawn context, PlayerAttackComponent attackComponent)
    {
        base.Initialize(context, attackComponent);

        if (IsClient)
        {
            playerStateComponent = context.GetPlayerComponent<PlayerStateComponent>();
            playerAnimator = context.GetPlayerComponent<PlayerAnimator>();
            playerAudioComponent = context.GetPlayerComponent<PlayerAudioComponent>();
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
            }

            playerAudioComponent.PlayAudio(hitSFXes.GetRandomElement(), 0.2f);
            impulseSource.GenerateImpulse(impulse);

            alreadyHit.Add(target);
        }
    }

    private void Update()
    {
        if (animationTime > 0)
        {
            animationTime -= Time.deltaTime;
            if (animationTime <= 0)
            {
                FinishAbility(this);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void RequestUseAbilityServerRpc(ServerRpcParams serverParams)
    {
        if (playerStateComponent.HasStatusEffect(StatusEffect.Type.Stun)) return;
        if (animationTime > 0) return;

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
            playerAnimator.Attack1();
            StartCoroutine(PlayVFX(attack1VFX));
            animationTime = attack1.length;
        }
        else if (attackCount.Value == 0)
        {
            playerAnimator.Attack2();
            StartCoroutine(PlayVFX(attack2VFX));
            animationTime = attack2.length;
        }
        else if (attackCount.Value == 1)
        {
            playerAnimator.Attack3();
            StartCoroutine(PlayVFX(attack3VFX));
            animationTime = attack3.length;
        }

        attackCount.Value = (attackCount.Value + 1) % 3;

        UseAbilityClientRpc(clientParams);
        PlayerAttackClientRpc(serverParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void UseAbilityClientRpc(ClientRpcParams clientParams)
    {
        StartAbility(this);
    }

    [ClientRpc]
    private void PlayerAttackClientRpc(ulong sender, ClientRpcParams clientParams = default)
    {

    }

    private IEnumerator PlayVFX(VFXSettings settings)
    {
        yield return new WaitForSeconds(settings.delay);

        //position is relative to the forward direction of the player
        Vector3 pos = transform.position + (transform.rotation * settings.position);

        GameObject vfx = Instantiate(settings.vfx, pos, transform.rotation * Quaternion.Euler(settings.rotation));
        vfx.GetComponent<NetworkObject>().Spawn();
        Destroy(vfx, settings.cleanUpTimer);
    }
}
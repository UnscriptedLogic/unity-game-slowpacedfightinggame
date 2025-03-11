using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DefaultCube : NetworkBehaviour
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip explodeSFX;

    [SerializeField] private AudioSource audioSourcePrefab;
    [SerializeField] private AudioSource audioSource;

    private ulong sender;
    private bool called = false;

    [SerializeField] private List<P_DefaultPlayerPawn> contacted;

    internal void Server_Initialize(ulong sender)
    {
        this.sender = sender;

        contacted = new List<P_DefaultPlayerPawn>();
    }

    private void Update()
    {
        if (IsServer)
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0)
            {
                if (called) return;

                Server_Explode();
                ExplodeClientRpc();

                NetworkObject.Despawn(true);
                called = true;
            }
        }
    }

    private void Server_Explode()
    {
        //raycast in a sphere for all players
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out PlayerHealthComponent healthComponent))
            {
                if (collider.GetComponentInParent<NetworkObject>().OwnerClientId == sender) continue;

                healthComponent.Server_TakeDamage(new DamageSettings()
                {
                    damage = 20f,
                    kbForce = 2000f,
                    kbDir = (collider.transform.position - transform.position).normalized,
                    kbDuration = 0.2f,
                });
            }
            if (collider.TryGetComponent(out PlayerStateComponent stateComponent))
            {
                stateComponent.Server_AddStatusEffect(new StatusEffect()
                {
                    type = StatusEffect.Type.Stun,
                    duration = 1f,
                });
            }
        }
    }

    [ClientRpc]
    private void ExplodeClientRpc()
    {
        AudioSource externalAS = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);
        externalAS.PlayOneShot(explodeSFX);

        Destroy(externalAS.gameObject, 2f);
        Destroy(Instantiate(explosionPrefab, transform.position, Quaternion.identity), 2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsClient)
        {
            if (other.TryGetComponent(out PlayerHealthComponent healthComponent))
            {
                audioSource.PlayOneShot(hitSFX);
            }
        }

        if (IsServer)
        {
            if (other.TryGetComponent(out PlayerHealthComponent healthComponent))
            {
                if (contacted.Contains(healthComponent.GetComponentInParent<P_DefaultPlayerPawn>())) return;

                if (other.GetComponentInParent<NetworkObject>().OwnerClientId == sender) return;

                healthComponent.Server_TakeDamage(new DamageSettings()
                {
                    damage = 5f,
                    kbForce = 1000f,
                    kbDir = Vector3.zero,
                    kbDuration = 0.1f,
                });
            }

            if (other.TryGetComponent(out PlayerStateComponent stateComponent))
            {
                stateComponent.Server_AddStatusEffect(new StatusEffect()
                {
                    type = StatusEffect.Type.Stun,
                    duration = 1f,
                });
            }

            contacted.Add(other.GetComponentInParent<P_DefaultPlayerPawn>());
        }
    }
}

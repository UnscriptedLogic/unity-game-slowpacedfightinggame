using System;
using Unity.Netcode;
using UnityEngine;

public class Hook : NetworkBehaviour
{
    [SerializeField] private float travelTime = 2f;
    [SerializeField] private float travelSpeed = 10f;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private AudioClip hookLandedSFX;
    [SerializeField] private AudioSource audioSource;

    private ulong sender;
    private Transform senderTransform;
    private Vector3 senderPos;

    private Transform target;

    public void Server_Initialize(ulong sender, Transform senderTransform)
    {
        this.sender = sender;
        this.senderTransform = senderTransform;

        Debug.Log(sender);
        Debug.Log(senderTransform);
    }

    internal void Client_Initialize(Transform parent)
    {
        senderTransform = parent;
    }

    private void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent(out NetworkObject networkObject);
        if (networkObject == null) return;
        if (networkObject.OwnerClientId == sender) return;

        audioSource.PlayOneShot(hookLandedSFX);

        if (!IsServer) return;
        if (travelTime <= 0) return;

        travelTime = 0;

        if (other.gameObject.activeInHierarchy)
        {
            transform.position = other.transform.position;
            networkObject.TrySetParent(transform);

            target = other.transform;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            P_PlayerPawn player = other.GetComponentInParent<P_PlayerPawn>();

            player.GetPlayerComponent<PlayerStateComponent>().Server_AddStatusEffect(new StatusEffect(StatusEffect.Type.Stun, 2f));
        }
    }

    private void Update()
    {
        if (IsClient)
        {
            if (senderTransform != null)
            {
                senderPos = senderTransform.position;
            }

            lineRenderer.SetPosition(0, senderPos);
            lineRenderer.SetPosition(1, transform.position);
        }

        if (IsServer)
        {
            if (senderTransform != null)
            {
                senderPos = senderTransform.position;
            }

            if (travelTime > 0)
            {
                travelTime -= Time.deltaTime;
                transform.position += transform.forward * travelSpeed * Time.deltaTime;
            }
            else
            {
                if (senderTransform == null) return;

                //return to player
                transform.position = Vector3.MoveTowards(transform.position, senderPos, travelSpeed * Time.deltaTime);
                transform.forward = transform.position - senderPos;

                if (Vector3.Distance(transform.position, senderPos) < 0.05f)
                {
                    if (target != null)
                    {
                        target.TryGetComponent(out NetworkObject targetNetworkObject);
                        if (targetNetworkObject != null)
                        {
                            targetNetworkObject.TrySetParent((Transform)null);
                        }
                    }

                    Destroy(gameObject);
                }
            }
        }
    }
}

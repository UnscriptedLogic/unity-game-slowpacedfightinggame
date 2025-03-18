using DG.Tweening;
using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnscriptedEngine;

public class Hook : NetworkBehaviour
{
    [SerializeField] private float travelTime = 2f;
    [SerializeField] private float travelSpeed = 10f;
    [SerializeField] private float airborneSpeed = 20f;

    private float currentTravelSpeed;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private AudioClip hookLandedSFX;
    [SerializeField] private AudioSource audioSource;

    private bool isAirborne;
    private ulong sender;
    private Transform senderTransform;
    private Vector3 senderPos;
    private HookAbility hookAbility;

    private Transform target;
    private ClientNetworkTransform netTransform;

    public void Server_Initialize(ulong sender, Transform senderTransform, bool isAirborne, HookAbility hookAbility)
    {
        this.sender = sender;
        this.senderTransform = senderTransform;
        this.isAirborne = isAirborne;
        this.hookAbility = hookAbility;

        currentTravelSpeed = isAirborne ? airborneSpeed : travelSpeed;
    }

    internal void Client_Initialize(Transform parent)
    {
        senderTransform = parent;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (target != null) return;

        other.TryGetComponent(out NetworkObject networkObject);

        if (networkObject == null) return;
        if (networkObject.OwnerClientId == sender) return;

        if (!IsServer) return;
        if (travelTime <= 0) return;

        travelTime = 0;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            P_PlayerPawn player = other.GetComponentInParent<P_PlayerPawn>();
            PlayerStateComponent state = player.GetPlayerComponent<PlayerStateComponent>();
            if (state.HasStatusEffect(StatusEffect.Type.Invincible)) return;

            player.GetPlayerComponent<PlayerStateComponent>().Server_AddStatusEffect(new StatusEffect(StatusEffect.Type.Stun, 2f));
        }

        if (other.gameObject.TryGetComponent(out Rigidbody rb))
        {
            rb.velocity = Vector3.zero;
        }

        if (other.gameObject.activeInHierarchy)
        {
            transform.position = other.transform.position;
            networkObject.TrySetParent(transform);

            target = other.transform;

        }

        //hookAbility.OnHooked(target);
        OnHookedClientRpc(target.GetComponent<NetworkObject>());
    }

    private void DoSyncTransform(ClientNetworkTransform netTransform, bool value)
    {
        netTransform.SyncPositionX = value;
        netTransform.SyncPositionY = value;
        netTransform.SyncPositionZ = value;
        netTransform.SyncRotAngleX = value;
        netTransform.SyncRotAngleY = value;
        netTransform.SyncRotAngleZ = value;
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
            currentTravelSpeed = isAirborne ? airborneSpeed : travelSpeed;

            if (senderTransform != null)
            {
                senderPos = senderTransform.position;
            }

            if (travelTime > 0)
            {
                travelTime -= Time.deltaTime;
                transform.position += transform.forward * currentTravelSpeed * Time.deltaTime;
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

                        HookReturnedClientRpc(targetNetworkObject);
                        target = null;
                    }

                    Destroy(gameObject);
                }
            }
        }
    }

    [ClientRpc]
    private void OnHookedClientRpc(NetworkObjectReference targetRef)
    {
        audioSource.PlayOneShot(hookLandedSFX);
        targetRef.TryGet(out NetworkObject targetNetworkObject);
        target = targetNetworkObject.transform;

        Debug.Log(IsOwner);

        if (IsOwner)
        {
            DoSyncTransform(target.GetComponent<ClientNetworkTransform>(), false);
            target.SetParent(transform);
            target.position = transform.position;
        }
    }

    [ClientRpc]
    private void HookReturnedClientRpc(NetworkObjectReference targetRef)
    {
        targetRef.TryGet(out NetworkObject targetNetworkObject);
        target = targetNetworkObject.transform;

        if (target == UGameModeBase.instance.GetPlayerPawn().transform)
        {
            if (target.TryGetComponent(out MovementComponent movementComponent))
            {
                movementComponent.SetRotation(-senderTransform.forward);
            }
        }

        target.transform.SetParent(null);
        DoSyncTransform(target.GetComponent<ClientNetworkTransform>(), true);
    }
}

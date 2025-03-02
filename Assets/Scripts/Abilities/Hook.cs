using System;
using Unity.Netcode;
using UnityEngine;

public class Hook : NetworkBehaviour
{
    [SerializeField] private float travelTime = 2f;
    [SerializeField] private float travelSpeed = 10f;

    [SerializeField] private LineRenderer lineRenderer;

    private ulong sender;
    private Transform senderTransform;

    public event Action OnHooked;

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
        if (networkObject.OwnerClientId == sender) return;

        OnHooked?.Invoke();

        if (!IsServer) return;
        if (travelTime <= 0) return;

        travelTime = 0;

        if (other.gameObject.activeInHierarchy)
        {
            networkObject.TrySetParent(transform);
            networkObject.transform.localPosition = Vector3.zero;
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
            lineRenderer.SetPosition(0, senderTransform.position);
            lineRenderer.SetPosition(1, transform.position);
        }

        if (IsServer)
        {
            if (travelTime > 0)
            {
                travelTime -= Time.deltaTime;
                transform.position += transform.forward * travelSpeed * Time.deltaTime;
            }
            else
            {
                if (senderTransform == null) return;

                //return to player
                transform.position = Vector3.MoveTowards(transform.position, senderTransform.position, travelSpeed * Time.deltaTime);

                transform.forward = transform.position - senderTransform.position;

                if (Vector3.Distance(transform.position, senderTransform.position) < 0.05f)
                {
                    //remove all children
                    foreach (Transform child in transform)
                    {
                        child.TryGetComponent(out NetworkObject networkObject);
                        if (networkObject != null)
                        {
                            networkObject.TrySetParent((Transform)null);
                        }
                    }

                    Destroy(gameObject);
                }
            }
        }
    }
}

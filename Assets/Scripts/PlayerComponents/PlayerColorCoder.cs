using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerColorCoder : NetworkBehaviour
{
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private Color enemyColor;

    public override void OnNetworkSpawn()
    {
        if (IsServer) return;

        if (IsClient && !IsOwner)
        {
            meshRenderer.material.color = enemyColor;
        }
    }
}

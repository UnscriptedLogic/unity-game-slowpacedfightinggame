using Unity.Cinemachine;
using UnityEngine;
using UnscriptedEngine;

public class P_DefaultPlayerPawn : P_PlayerPawn
{
    [SerializeField] private CinemachineCamera cinemachineCam;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            cinemachineCam.Priority.Value = -100;
            cinemachineCam.gameObject.SetActive(false);
            return;
        }
    }

    public override void OnPossess(UController uController)
    {
        base.OnPossess(uController);

        cinemachineCam.transform.SetParent(null);
        this.ToggleMouse(false);

        InitializeComponents(this);
    }

    protected override void Update()
    {
        if (!IsHost)
        {
            if (!IsOwner || IsServer) return;
        }

        base.Update();
    }

    protected override void FixedUpdate()
    {
        if (!IsHost)
        {
            if (!IsOwner || IsServer) return;
        }

        base.FixedUpdate();
    }

    public override void OnUnPossess(UController uController)
    {
        if (!IsHost)
        {
            if (!IsOwner || IsServer) return;
        }

        DeInitializeComponents(this);

        base.OnUnPossess(uController);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsHost)
        {
            if (!IsOwner || IsServer) return;
        }

        DeInitializeComponents(this);

        base.OnNetworkDespawn();
    }
}

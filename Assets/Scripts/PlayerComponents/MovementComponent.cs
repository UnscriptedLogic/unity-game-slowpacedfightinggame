using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements.Experimental;
using UnscriptedEngine;

public class MovementComponent : PlayerBaseComponent
{
    private CustomGameInstance customGameInstance;

    [SerializeField] private MovementSettings movementSettings;
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private float cameraSens;
    [SerializeField] private ClientNetworkTransform cameraRefTransform;

    [SerializeField] private List<AudioClip> walkSFXes;
    [SerializeField] private float walkSFXInterval;
    private float walkIntervalTimer;

    private PlayerAudioComponent playerAudioComponent;
    private PlayerStateComponent playerStateComponent;
    private Rigidbody rb;

    private Vector3 prevPosition;
    private float unitSpeed;
    private bool shiftPressed;

    public MovementSettings MoveSettings => movementSettings;
    public float UnitSpeed => unitSpeed;
    public Transform CameraRefTransform => cameraRefTransform.transform;

    public bool isGrounded => Physics.Raycast(transform.position, Vector3.down, 1.2f);
    public bool isMoving => movementSettings.InputDir.magnitude > 0.01f && isGrounded;
    public GameObject StandingOn => isGrounded ? Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit) ? hit.collider.gameObject : null : null;
    public bool IsAirborne
    {
        get
        {
            float distance = 0f;
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
            {
                distance = hit.distance;
            }

            return distance > 4f && !isGrounded;
        }
    }

    /// <summary>
    /// This struct is a point in time in the timeline of the player.
    /// It's used to let the server remember the path the player took.
    /// How strict we reconcile is dependant on the ping of the player.
    /// The higher the ping, the more backwards in time we'll check.
    /// This is to prevent rubberbanding and overcorrecting.
    /// </summary>
    [System.Serializable]
    public struct ServerClientData
    {
        public int tick;
        public Vector3 position;
        public Vector3 rotation;
    }

    private ServerClientData[] server_clientDataBuffer = new ServerClientData[BUFFER_SIZE];

    //Server Variables
    [System.Serializable]
    private class ClientSentMovementData : INetworkSerializable
    {
        public int tick;
        public Vector3 input;
        public Vector3 rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref input);
            serializer.SerializeValue(ref rotation);
        }
    }

    [System.Serializable]
    private struct ClientReconcileData : INetworkSerializable
    {
        public int reconcileDiffTick;
        public Vector3 position;
        public Vector3 rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref reconcileDiffTick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
        }
    }

    private int currentTick = 0;
    private ClientReconcileData client_reconcileData;
    private float time;
    private float tickTime;
    private bool isReconciling;

    private const int TICKS_PER_SECOND = 60;
    private const int BUFFER_SIZE = 1024;
    private ClientSentMovementData[] clientMovementData = new ClientSentMovementData[BUFFER_SIZE];

    private bool inputToggled = true;
    private ClientSentMovementData movementData;

    private int ClientPing => customGameInstance.ClientPing(NetworkObject.OwnerClientId);
    private int serverPing => customGameInstance.ServerPing;

    /// <summary>
    /// Should we reconcile the client with the server.
    /// </summary>
    private bool ShouldReconcile => ReconcileDist > 1f;

    /// <summary>
    /// How far back in time do we check for the server to reconcile the client.
    /// </summary>
    private int ReconcileTickDiff
    {
        get
        {
            float ping = IsServer ? ClientPing : serverPing;
            return Mathf.RoundToInt(ping / 2 / TICKS_PER_SECOND * 100);
        }
    }

    /// <summary>
    /// The tick we should reconcile the client with the server.
    /// </summary>
    private int ReconcileTick => currentTick - ReconcileTickDiff;

    /// <summary>
    /// How far off is the client from the server.
    /// </summary>
    private float ReconcileDist => Vector3.Distance(GetLocalReconcileClientData.position, client_reconcileData.position);
    private int ClientReconcileTick => currentTick - client_reconcileData.reconcileDiffTick;
    private ServerClientData GetLocalReconcileClientData => server_clientDataBuffer[ClientReconcileTick % BUFFER_SIZE];

    private void Awake()
    {
        tickTime = 1f / TICKS_PER_SECOND;
    }

    private void Start()
    {
        if (IsServer)
        {
            playerStateComponent = GetComponent<PlayerStateComponent>();
            customGameInstance = UGameModeBase.instance.GetGameInstance<CustomGameInstance>();

            rb.isKinematic = false;
        }

        NetworkManager.NetworkTickSystem.Tick += OnNetworkTick;
    }

    private void OnNetworkTick()
    {
        while (time > tickTime)
        {
            if (IsServer)
            {
                Server_FixedUpdate();
            }

            if (IsClient)
            {
                Client_FixedUpdate();

                if (IsOwner)
                {
                    LocalClient_FixedUpdate();
                }
            }

            time -= tickTime;
            currentTick++;
        }
    }

    public override void Initialize(P_PlayerPawn context)
    {
        base.Initialize(context);

        customGameInstance = UGameModeBase.instance.GetGameInstance<CustomGameInstance>();

        playerAudioComponent = context.GetPlayerComponent<PlayerAudioComponent>();
        playerStateComponent = context.GetPlayerComponent<PlayerStateComponent>();

        InputPriority.MovePriority = 1;

        rb = context.Rb;
        prevPosition = transform.position;

        movementSettings.ResetJumpCounter();

        if (!IsServer)
        {
            context.GetDefaultInputMap().FindAction("MouseDelta").performed += OnMouseDelta;
        }

        cameraSens = customGameInstance.settings.mouseSensitivity.Value / 100f;
        customGameInstance.settings.mouseSensitivity.OnValueChanged += (float value) => cameraSens = value / 100f;

        MonoBehaviourExtensions.OnToggleInput += OnToggleInput;
        
        initialized = true;
    }

    private void OnToggleInput(bool obj)
    {
        inputToggled = obj;
    }

    private void OnMouseDelta(InputAction.CallbackContext context)
    {
        if (!inputToggled) return;
        if (isReconciling) return;

        Vector2 mouseDelta = context.ReadValue<Vector2>();

        transform.Rotate(Vector3.up, mouseDelta.x * cameraSens * Time.deltaTime);
        cameraAnchor.Rotate(Vector3.right, -mouseDelta.y * cameraSens * Time.deltaTime);
    }

    public override void OnMove(Vector2 inputDir, out bool swallowInput)
    {
        movementSettings.InputDir = new Vector3(inputDir.x, 0f, inputDir.y);
        swallowInput = false;
    }

    public override void OnSpace(bool pressed, out bool swallowInput)
    {
        Jump();
        swallowInput = false;
    }

    public override void UpdateTick(out bool swallowTick)
    {
        swallowTick = false;

        if (!inputToggled) return;

        unitSpeed = Vector3.Distance(prevPosition, transform.position) / Time.deltaTime;

        if (movementSettings.waitingForLanding)
        {
            if (isGrounded)
            {
                movementSettings.waitingForLanding = false;
                movementSettings.hasJumped = false;

                movementSettings.ResetJumpCounter();
            }
        }

        cameraRefTransform.transform.forward = Camera.main.transform.forward;

        if (!isGrounded && movementSettings.hasJumped)
        {
            movementSettings.waitingForLanding = true;
        }

        prevPosition = transform.position;
    }

    private void Update()
    {
        time += Time.deltaTime;
    }

    private void FixedUpdate()
    {

    }

    private void Server_FixedUpdate()
    {
        MovementLogic(movementData.input);
        transform.rotation = Quaternion.Euler(movementData.rotation);

        server_clientDataBuffer[currentTick % BUFFER_SIZE] = new ServerClientData
        {
            tick = currentTick,
            position = transform.position,
            rotation = transform.rotation.eulerAngles
        };
    }

    private void Client_FixedUpdate() { }

    private void LocalClient_FixedUpdate()
    {
        if (!inputToggled) return;

        MovementLogic(movementSettings.InputDir);

        clientMovementData[currentTick % BUFFER_SIZE] = new ClientSentMovementData
        {
            tick = currentTick,
            input = movementSettings.InputDir,
            rotation = transform.rotation.eulerAngles
        };

        server_clientDataBuffer[currentTick % BUFFER_SIZE] = new ServerClientData
        {
            tick = currentTick,
            position = transform.position,
            rotation = transform.rotation.eulerAngles
        };

        PlayerMovementServerRpc(clientMovementData[currentTick % BUFFER_SIZE]);
    }

    private void MovementLogic(Vector3 inputDir)
    {
        inputDir = inputDir.normalized;
        if (inputDir.magnitude > 0.01f)
        {
            if (!playerStateComponent.HasStatusEffect(StatusEffect.Type.Stun))
            {
                //move player using rigidbody
                float efficiency = isGrounded ? 1f : 0.5f;
                float speed = shiftPressed ? movementSettings.speed * movementSettings.sprintMultiplier : movementSettings.speed;
                rb.MovePosition(rb.position + (transform.TransformDirection(inputDir) * speed * Time.fixedDeltaTime));
            }

            if (isGrounded && walkIntervalTimer <= 0f)
            {
                walkIntervalTimer = walkSFXInterval;
                playerAudioComponent.PlayAudio(walkSFXes.GetRandomElement(), 0.5f);
            }

            walkIntervalTimer -= Time.fixedDeltaTime;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerMovementServerRpc(ClientSentMovementData currentData, ServerRpcParams serverParams = default)
    {
        movementData = currentData;

        clientMovementData[currentTick % BUFFER_SIZE] = currentData;

        ReconciliateClientRpc(new ClientReconcileData()
        {
            reconcileDiffTick = ReconcileTickDiff,
            position = server_clientDataBuffer[ReconcileTick % BUFFER_SIZE].position,
            rotation = server_clientDataBuffer[ReconcileTick % BUFFER_SIZE].rotation
        },
        new ClientReconcileData()
        {
            reconcileDiffTick = 0,
            position = server_clientDataBuffer[currentTick % BUFFER_SIZE].position,
            rotation = server_clientDataBuffer[currentTick % BUFFER_SIZE].rotation
        });
    }

    [ClientRpc]
    private void ReconciliateClientRpc(ClientReconcileData reconcileTimeData, ClientReconcileData currentTimeData, ClientRpcParams clientParams = default)
    {
        client_reconcileData = reconcileTimeData;


    }

    public override void OnShift(bool pressed, out bool swallowInput)
    {
        swallowInput = false;
        shiftPressed = pressed;
    }

    public void Jump()
    {
        if (!inputToggled) return;

        if (!movementSettings.CanJump && isGrounded)
        {
            movementSettings.ResetJumpCounter();
        }

        if (movementSettings.CanJump)
        {
            rb.AddForce(Vector3.up * movementSettings.jumpForce, ForceMode.Impulse);
            movementSettings.DecrementJumpCounter();

            movementSettings.hasJumped = true;
        }
    }

    public override void DeInitialize(P_PlayerPawn context)
    {
        context.GetDefaultInputMap().FindAction("MouseDelta").performed -= OnMouseDelta;

        base.DeInitialize(context);
    }

    internal void SetRotation(Vector3 forward)
    {
        transform.rotation = Quaternion.LookRotation(forward);
        Debug.Log("Rotation Set");
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < server_clientDataBuffer.Length; i++)
        {
            if (server_clientDataBuffer[i].tick == 0) continue;

            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(server_clientDataBuffer[i].position, 0.1f);
        }

        if (IsServer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(server_clientDataBuffer[(currentTick - ReconcileTickDiff) % BUFFER_SIZE].position, Vector3.one * 1f);
        }

        if (IsClient)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(GetLocalReconcileClientData.position, Vector3.one);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(client_reconcileData.position, Vector3.one);

            Gizmos.color = ShouldReconcile ? Color.red : Color.green;
            Gizmos.DrawLine(GetLocalReconcileClientData.position, client_reconcileData.position);
        }
    }
}

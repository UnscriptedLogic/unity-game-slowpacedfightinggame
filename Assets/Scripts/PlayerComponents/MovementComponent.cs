using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnscriptedEngine;

public class MovementComponent : PlayerBaseComponent
{
    [SerializeField] private MovementSettings movementSettings;
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private float cameraSens;

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

    public bool isGrounded => Physics.Raycast(transform.position, Vector3.down, 1.2f);
    public bool isMoving => movementSettings.InputDir.magnitude > 0.01f && isGrounded;
    public GameObject StandingOn => isGrounded ? Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit) ? hit.collider.gameObject : null : null;

    //Server Variables
    [System.Serializable]
    private class ServerMovementData : INetworkSerializable
    {
        public int tick;
        public Vector2 input;
        public Vector3 position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref input);
            serializer.SerializeValue(ref position);
        }
    }

    private int currentTick = 0;
    private float time;
    private float tickTime;

    private const int TICKS_PER_SECOND = 60;
    private const int BUFFER_SIZE = 1024;
    private ServerMovementData[] clientMovementData = new ServerMovementData[BUFFER_SIZE];

    private void Awake()
    {
        tickTime = 1f / TICKS_PER_SECOND;
    }

    private void Start()
    {
        if (IsServer)
        {
            playerStateComponent = GetComponent<PlayerStateComponent>();
        }
    }

    public override void Initialize(P_PlayerPawn context)
    {
        base.Initialize(context);

        playerAudioComponent = context.GetPlayerComponent<PlayerAudioComponent>();
        playerStateComponent = context.GetPlayerComponent<PlayerStateComponent>();

        InputPriority.MovePriority = 1;

        rb = context.Rb;
        prevPosition = transform.position;

        movementSettings.ResetJumpCounter();

        context.GetDefaultInputMap().FindAction("MouseDelta").performed += OnMouseDelta;

        initialized = true;
    }

    private void OnMouseDelta(InputAction.CallbackContext context)
    {
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

        if (!isGrounded && movementSettings.hasJumped)
        {
            movementSettings.waitingForLanding = true;
        }

        prevPosition = transform.position;
        time += Time.deltaTime;
    }

    public override void FixedUpdateTick(out bool swallowTick)
    {
        swallowTick = false;

        while (time > tickTime)
        {
            time -= tickTime;
            currentTick++;

            if (movementSettings.InputDir.magnitude > 0.01f)
            {
                if (playerStateComponent.HasStatusEffect(StatusEffect.Type.Stun)) return;

                //move player using rigidbody
                float efficiency = isGrounded ? 1f : 0.5f;
                float speed = shiftPressed ? movementSettings.speed * movementSettings.sprintMultiplier : movementSettings.speed;
                rb.MovePosition(transform.position + (transform.TransformDirection(movementSettings.InputDir) * speed * Time.fixedDeltaTime));

                if (isGrounded && walkIntervalTimer <= 0f)
                {
                    walkIntervalTimer = walkSFXInterval;

                    playerAudioComponent.PlayAudio(walkSFXes.GetRandomElement(), 0.5f);
                }

                walkIntervalTimer -= Time.fixedDeltaTime;
            }

            if (IsHost) continue;

            clientMovementData[currentTick % BUFFER_SIZE] = new ServerMovementData
            {
                tick = currentTick,
                input = movementSettings.InputDir,
                position = transform.position
            };

            if (currentTick < 2) return;

            ServerRpcParams serverParams = new ServerRpcParams
            {
                Receive = new ServerRpcReceiveParams
                {
                    SenderClientId = NetworkManager.LocalClientId
                }
            };

            PlayerMovementServerRpc(clientMovementData[currentTick % BUFFER_SIZE], clientMovementData[(currentTick - 1) % BUFFER_SIZE], serverParams);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void PlayerMovementServerRpc(ServerMovementData currentData, ServerMovementData lastData, ServerRpcParams serverParams = default)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        Vector3 startPos = transform.position;

        if (lastData == null) return;

        Vector3 moveVector = transform.TransformDirection(lastData.input) * movementSettings.speed * Time.fixedDeltaTime;
        Physics.simulationMode = SimulationMode.Script;
        transform.position = lastData.position;
        rb.MovePosition(transform.position + (transform.TransformDirection(lastData.input) * movementSettings.speed * Time.fixedDeltaTime));
        Physics.Simulate(Time.fixedDeltaTime);
        Vector3 correctPos = transform.position;
        transform.position = startPos;
        Physics.simulationMode = SimulationMode.FixedUpdate;

        float distance = Vector3.Distance(correctPos, currentData.position);
        if (distance > 2f)
        {
            Debug.Log($"Position is off: {distance}");

            ClientRpcParams clientParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { serverParams.Receive.SenderClientId }
                }
            };

            ReconciliateClientRpc(currentData.tick, clientParams);
        }
    }


    [ClientRpc]
    private void ReconciliateClientRpc(int activationTick, ClientRpcParams clientParams = default)
    {
        Vector3 correctPos = clientMovementData[(activationTick - 1) % BUFFER_SIZE].position;

        Physics.simulationMode = SimulationMode.Script;
        while (activationTick <= currentTick)
        {
            transform.position = correctPos;
            rb.MovePosition(transform.position + (transform.TransformDirection(clientMovementData[(activationTick - 1) % BUFFER_SIZE].input) * movementSettings.speed * Time.fixedDeltaTime));
            Physics.Simulate(Time.fixedDeltaTime);
            correctPos = transform.position;
            clientMovementData[activationTick % BUFFER_SIZE].position = correctPos;
            activationTick++;
        }

        Physics.simulationMode = SimulationMode.FixedUpdate;
        transform.position = correctPos;
    }


    public override void OnShift(bool pressed, out bool swallowInput)
    {
        swallowInput = false;
        shiftPressed = pressed;
    }

    public void Jump()
    {
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
}

using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
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

    //Server Variables
    [System.Serializable]
    private class ServerMovementData : INetworkSerializable
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
    private struct ClientMovementData : INetworkSerializable
    {
        public Vector3 position;
        public Vector3 rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
        }
    }

    private int currentTick = 0;
    private float time;
    private float tickTime;
    private bool reconcilliating;

    private const int TICKS_PER_SECOND = 60;
    private const int BUFFER_SIZE = 1024;
    private ServerMovementData[] clientMovementData = new ServerMovementData[BUFFER_SIZE];

    private bool inputToggled = true;
    private ServerMovementData movementData;

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

        initialized = true;
        reconcilliating = false;

        cameraSens = customGameInstance.settings.mouseSensitivity.Value / 100f;
        customGameInstance.settings.mouseSensitivity.OnValueChanged += (float value) => cameraSens = value / 100f;

        MonoBehaviourExtensions.OnToggleInput += OnToggleInput;
    }

    private void OnToggleInput(bool obj)
    {
        inputToggled = obj;
    }

    private void OnMouseDelta(InputAction.CallbackContext context)
    {
        if (!inputToggled) return;
        if (reconcilliating) return;

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
        time += Time.deltaTime;
    }

    private void Update()
    {
        if (IsServer)
        {
            while (time > tickTime)
            {
                time -= tickTime;
                currentTick++;
            }
        }
    }

    private void FixedUpdate()
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
    }

    private void Server_FixedUpdate()
    {
        MovementLogic(movementData.input);

        transform.rotation = Quaternion.Euler(movementData.rotation);
    }

    private void Client_FixedUpdate()
    {

    }

    private void LocalClient_FixedUpdate()
    {
        if (!inputToggled) return;
        if (reconcilliating) return;

        MovementLogic(movementSettings.InputDir);

        clientMovementData[currentTick % BUFFER_SIZE] = new ServerMovementData
        {
            tick = currentTick,
            input = movementSettings.InputDir,
            rotation = transform.rotation.eulerAngles
        };

        PlayerMovementServerRpc(clientMovementData[currentTick % BUFFER_SIZE]);
    }

    //public override void FixedUpdateTick(out bool swallowTick)
    //{
    //    swallowTick = false;

    //    if (!inputToggled) return;

    //    while (time > tickTime)
    //    {
    //        time -= tickTime;
    //        currentTick++;

    //        MovementLogic();

    //        if (IsHost) continue;

    //        clientMovementData[currentTick % BUFFER_SIZE] = new ServerMovementData
    //        {
    //            tick = currentTick,
    //            input = movementSettings.InputDir,
    //            position = transform.position
    //        };

    //        if (currentTick < 2) return;

    //        ServerRpcParams serverParams = new ServerRpcParams
    //        {
    //            Receive = new ServerRpcReceiveParams
    //            {
    //                SenderClientId = NetworkManager.LocalClientId
    //            }
    //        };

    //        PlayerMovementServerRpc(clientMovementData[currentTick % BUFFER_SIZE], clientMovementData[(currentTick - 1) % BUFFER_SIZE], serverParams);
    //    }
    //}

    private void MovementLogic(Vector3 inputDir)
    {
        if (inputDir.magnitude > 0.01f)
        {
            if (!playerStateComponent.HasStatusEffect(StatusEffect.Type.Stun))
            {
                //move player using rigidbody
                float efficiency = isGrounded ? 1f : 0.5f;
                float speed = shiftPressed ? movementSettings.speed * movementSettings.sprintMultiplier : movementSettings.speed;
                rb.MovePosition(transform.position + (transform.TransformDirection(inputDir) * speed * Time.fixedDeltaTime));
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
    private void PlayerMovementServerRpc(ServerMovementData currentData, ServerRpcParams serverParams = default)
    {
        movementData = currentData;

        clientMovementData[currentTick % BUFFER_SIZE] = currentData;

        if ((currentTick % 20) == 0)
        {
            ReconciliateClientRpc(new ClientMovementData()
            {
                position = transform.position,
                rotation = transform.rotation.eulerAngles
            });
        }
    }

    [ClientRpc]
    private void ReconciliateClientRpc(ClientMovementData clientData, ClientRpcParams clientParams = default)
    {
        reconcilliating = true;

        if (Vector3.Distance(clientData.position, transform.position) >= 3f)
        {
            transform.DORotate(clientData.rotation, 0.5f);
            transform.DOMove(clientData.position, 0.5f);
        }

        //should be here for some reason. soft locks if inside of dotween
        reconcilliating = false;
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
}

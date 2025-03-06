using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnscriptedEngine;

public class PlayerAttackComponent : PlayerBaseComponent
{
    public class SyncAbilitiesParams : INetworkSerializable
    {
        public NetworkObjectReference melee;
        public NetworkObjectReference ability1;
        public NetworkObjectReference ability2;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref melee);
            serializer.SerializeValue(ref ability1);
            serializer.SerializeValue(ref ability2);
        }
    }

    public enum States
    {
        Idle,
        Attacking,
        Stunned
    }

    [SerializeField] private Ability meleeAbility;
    [SerializeField] private Ability ability1;
    [SerializeField] private Ability ability2;
    [SerializeField] private TriggerHandler meleeHitbox;

    [SerializeField] private UIC_AbilityHUD abilityHUDPrefab;

    private PlayerStateComponent playerStateComponent;

    private int abilityIndex;
    private Ability currentAbility;
    private States currentState;

    private UIC_AbilityHUD abilityHUD;
    private bool toggledInput = true;

    public TriggerHandler MeleeHitbox => meleeHitbox;

    private CustomGameInstance customGameInstance;

    public event Action<Ability> OnAbilityApexed;

    private void Start()
    {
        customGameInstance = UGameModeBase.instance.GetGameInstance<CustomGameInstance>();
    }

    public override void Initialize(P_PlayerPawn context)
    {
        base.Initialize(context);

        customGameInstance = UGameModeBase.instance.GetGameInstance<CustomGameInstance>();

        SyncAbilitiesServerRpc(
            customGameInstance.AllMelee.GetIndexByAbility(customGameInstance.Melee),
            customGameInstance.AllAbilities.GetIndexByAbility(customGameInstance.Ability1), 
            customGameInstance.AllAbilities.GetIndexByAbility(customGameInstance.Ability2), 
            new ServerRpcParams()
            {
                Receive = new ServerRpcReceiveParams()
                {
                    SenderClientId = OwnerClientId
                }
            });

        playerStateComponent = GetComponent<PlayerStateComponent>();

        context.GetDefaultInputMap().FindAction("Ability1").performed += OnAbility1;
        context.GetDefaultInputMap().FindAction("Ability2").performed += OnAbility2;

        MonoBehaviourExtensions.OnToggleInput += OnToggleInput;
    }


    [ServerRpc(RequireOwnership = false)]
    private void SyncAbilitiesServerRpc(int meleeIndex, int ability1Index, int ability2Index, ServerRpcParams serverParams)
    {
        P_DefaultPlayerPawn context = NetworkManager.ConnectedClients[serverParams.Receive.SenderClientId].PlayerObject.GetComponent<C_PlayerController>().GetPossessedPawn<P_DefaultPlayerPawn>();

        if (meleeAbility != null)
        {
            meleeAbility.GetComponent<NetworkObject>().Despawn(true);
        }

        if (ability1 != null)
        {
            ability1.GetComponent<NetworkObject>().Despawn(true);
        }

        if (ability2 != null)
        {
            ability2.GetComponent<NetworkObject>().Despawn(true);
        }

        meleeAbility = Instantiate(customGameInstance.MeleeMap.GetAbilityPrefab(customGameInstance.AllMelee.List[meleeIndex]));
        NetworkObject meleeAbilityNO = meleeAbility.GetComponent<NetworkObject>();
        meleeAbilityNO.SpawnWithOwnership(serverParams.Receive.SenderClientId);
        meleeAbility.Server_Initialize(context);

        ability1 = Instantiate(customGameInstance.AbilityMap.GetAbilityPrefab(customGameInstance.AllAbilities.List[ability1Index]));
        NetworkObject ability1NO = ability1.GetComponent<NetworkObject>();
        ability1NO.SpawnWithOwnership(serverParams.Receive.SenderClientId);
        ability1.Server_Initialize(context);

        ability2 = Instantiate(customGameInstance.AbilityMap.GetAbilityPrefab(customGameInstance.AllAbilities.List[ability2Index]));
        NetworkObject ability2NO = ability2.GetComponent<NetworkObject>();
        ability2NO.SpawnWithOwnership(serverParams.Receive.SenderClientId);
        ability2.Server_Initialize(context);

        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { serverParams.Receive.SenderClientId }
            }
        };

        SyncAbilitiesClientRpc(new SyncAbilitiesParams
        {
            melee = meleeAbility.GetComponent<NetworkObject>(),
            ability1 = ability1.GetComponent<NetworkObject>(),
            ability2 = ability2.GetComponent<NetworkObject>()
        }, clientParams);
    }


    [ClientRpc]
    private void SyncAbilitiesClientRpc(SyncAbilitiesParams syncParams, ClientRpcParams clientParams)
    {
        syncParams.melee.TryGet(out NetworkObject meleeNO);
        syncParams.ability1.TryGet(out NetworkObject ability1NO);
        syncParams.ability2.TryGet(out NetworkObject ability2NO);

        meleeAbility = meleeNO.GetComponent<Ability>();
        ability1 = ability1NO.GetComponent<Ability>();
        ability2 = ability2NO.GetComponent<Ability>();

        meleeAbility.Client_Initialize(context, this);
        meleeAbility.OnStarted += OnAbilityStarted;
        meleeAbility.OnFinished += OnAbilityFinished;

        ability1.Client_Initialize(context, this);
        ability2.Client_Initialize(context, this);

        if (abilityHUD == null)
        {
            abilityHUD = context.AttachUIWidget(abilityHUDPrefab);
        }

        abilityHUD.Initialize(meleeAbility, ability1, ability2);

        customGameInstance.OnAbilitiesChanged += OnAbilitiesChanged;
    }

    private void OnAbilitiesChanged(AbilitySO ability1, AbilitySO ability2)
    {
        SyncAbilitiesServerRpc(
            customGameInstance.AllMelee.GetIndexByAbility(customGameInstance.Melee),
            customGameInstance.AllAbilities.GetIndexByAbility(customGameInstance.Ability1),
            customGameInstance.AllAbilities.GetIndexByAbility(customGameInstance.Ability2),
            new ServerRpcParams()
            {
                Receive = new ServerRpcReceiveParams()
                {
                    SenderClientId = OwnerClientId
                }
            });

        Debug.Log("Abilities ReInitialized");
    }

    private void OnToggleInput(bool obj)
    {
        toggledInput = obj;
    }

    public void AnimationApex()
    {
        OnAbilityApexed?.Invoke(currentAbility);
    }

    private void OnAbility2(InputAction.CallbackContext context)
    {
        AbilityConfig(2);
    }

    private void OnAbility1(InputAction.CallbackContext context)
    {
        AbilityConfig(1);
    }

    public override void OnDefaultLeftMouseDown(out bool swallowInput)
    {
        base.OnDefaultLeftMouseDown(out swallowInput);

        AbilityConfig(0);
    }

    private void AbilityConfig(int abilityIndex)
    {
        if (IsClient)
        {
            ServerRpcParams serverParams = new ServerRpcParams
            {
                Receive = new ServerRpcReceiveParams
                {
                    SenderClientId = OwnerClientId
                }
            };

            if (!toggledInput) return;

            RequestUseAbilityServerRpc(abilityIndex, serverParams);
        }

        if (IsServer)
        {
            if (currentState != States.Idle) return;
            UseAbilityClientRpc(abilityIndex, new ClientRpcParams());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUseAbilityServerRpc(int abilityIndex, ServerRpcParams serverParams)
    {
        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { serverParams.Receive.SenderClientId }
            }
        };

        if (currentState != States.Idle) return;

        UseAbilityClientRpc(abilityIndex, clientParams);
    }

    [ClientRpc]
    private void UseAbilityClientRpc(int abilityIndex, ClientRpcParams clientParams)
    {
        ServerRpcParams serverParams = new ServerRpcParams
        {
            Receive = new ServerRpcReceiveParams
            {
                SenderClientId = OwnerClientId
            }
        };

        switch (abilityIndex)
        {
            case 0:
                meleeAbility.RequestUseAbilityServerRpc(serverParams);
                break;
            case 1:
                ability1.RequestUseAbilityServerRpc(serverParams);
                break;
            case 2:
                ability2.RequestUseAbilityServerRpc(serverParams);
                break;
        }
    }    

    public override void UpdateTick(out bool swallowTick)
    {
        swallowTick = false;

        if (currentAbility != null)
        {
            currentAbility.UpdateTick();
        }
    }

    public override void FixedUpdateTick(out bool swallowTick)
    {
        swallowTick = false;

        if (currentAbility != null)
        {
            currentAbility.FixedUpdateTick();
        }
    }

    private void OnAbilityStarted(Ability ability)
    {
        currentAbility = ability;
        currentState = States.Attacking;
    }

    private void OnAbilityFinished(Ability ability)
    {
        currentAbility = null;
        currentState = States.Idle;
    }

    public override void OnDestroy()
    {
        if (IsServer)
        {
            ability1?.GetComponent<NetworkObject>().Despawn(true);
            ability2?.GetComponent<NetworkObject>().Despawn(true);
        }

        base.OnDestroy();
    }

    public override void DeInitialize(P_PlayerPawn context)
    {
        context.DettachUIWidget(abilityHUD);

        base.DeInitialize(context);
    }
}

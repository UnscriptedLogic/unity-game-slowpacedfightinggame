using System;
using System.Data;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnscriptedEngine;

public class PlayerAttackComponent : PlayerBaseComponent
{
    public class SyncAbilitiesParams : INetworkSerializable
    {
        public NetworkObjectReference context;
        public NetworkObjectReference melee;
        public NetworkObjectReference ability1;
        public NetworkObjectReference ability2;
        public NetworkObjectReference dodge;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref context);
            serializer.SerializeValue(ref melee);
            serializer.SerializeValue(ref ability1);
            serializer.SerializeValue(ref ability2);
            serializer.SerializeValue(ref dodge);
        }
    }

    public enum States
    {
        Idle,
        Attacking,
        Stunned
    }

    public NetworkVariable<int> ability1Id = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> ability2Id = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int dodgeId = 1999;

    [SerializeField] private Ability meleeAbility;
    private Ability ability1;
    private Ability ability2;
    private Ability dodgeAbility;

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        customGameInstance = UGameModeBase.instance.GetGameInstance<CustomGameInstance>();

        if (IsServer)
        {
            ability1Id.Value = customGameInstance.Ability1.ID;
            ability2Id.Value = customGameInstance.Ability2.ID;
        }

        if (IsClient)
        {
            ability1Id.OnValueChanged += Ability1Changed;
            ability2Id.OnValueChanged += Ability2Changed;
        }
    }

    public override void Initialize(P_PlayerPawn context)
    {
        base.Initialize(context);

        if (!IsOwner) return;

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
        context.GetDefaultInputMap().FindAction("Dodge").performed += OnAbility3;

        MonoBehaviourExtensions.OnToggleInput += OnToggleInput;
    }

    public override void OnDestroy()
    {
        ability1Id.OnValueChanged -= Ability1Changed;
        ability2Id.OnValueChanged -= Ability2Changed;

        base.OnDestroy();
    }

    private void Ability1Changed(int previousValue, int newValue)
    {
        Debug.Log(newValue);
    }

    private void Ability2Changed(int previousValue, int newValue)
    {
        Debug.Log(newValue);
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

        if (dodgeAbility != null)
        {
            dodgeAbility.GetComponent<NetworkObject>().Despawn(true);
        }

        ability1Id.Initialize(this);
        ability2Id.Initialize(this);

        ability1Id.Value = customGameInstance.AllAbilities.List[ability1Index].ID;
        ability2Id.Value = customGameInstance.AllAbilities.List[ability2Index].ID;

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

        dodgeAbility = Instantiate(customGameInstance.AbilityMap.GetAbilityPrefab(customGameInstance.AllAbilities.GetAbilityByID(1999)));
        NetworkObject dodgeAbilityNO = dodgeAbility.GetComponent<NetworkObject>();
        dodgeAbilityNO.SpawnWithOwnership(serverParams.Receive.SenderClientId);
        dodgeAbility.Server_Initialize(context);

        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { serverParams.Receive.SenderClientId }
            }
        };

        SyncAbilitiesClientRpc(new SyncAbilitiesParams
        {
            context = context.GetComponent<NetworkObject>(),
            melee = meleeAbility.GetComponent<NetworkObject>(),
            ability1 = ability1.GetComponent<NetworkObject>(),
            ability2 = ability2.GetComponent<NetworkObject>(),
            dodge = dodgeAbility.GetComponent<NetworkObject>()
        });
    }


    [ClientRpc]
    private void SyncAbilitiesClientRpc(SyncAbilitiesParams syncParams, ClientRpcParams clientParams = default)
    {
        syncParams.context.TryGet(out NetworkObject contextNO);
        syncParams.melee.TryGet(out NetworkObject meleeNO);
        syncParams.ability1.TryGet(out NetworkObject ability1NO);
        syncParams.ability2.TryGet(out NetworkObject ability2NO);
        syncParams.dodge.TryGet(out NetworkObject dodgeNO);

        P_DefaultPlayerPawn context = contextNO.GetComponent<P_DefaultPlayerPawn>();

        meleeAbility = meleeNO.GetComponent<Ability>();
        ability1 = ability1NO.GetComponent<Ability>();
        ability2 = ability2NO.GetComponent<Ability>();
        dodgeAbility = dodgeNO.GetComponent<Ability>();

        meleeAbility.Client_Initialize(context, this);
        ability1.Client_Initialize(context, this);
        ability2.Client_Initialize(context, this);
        dodgeAbility.Client_Initialize(context, this);

        meleeAbility.OnStarted += OnAbilityStarted;
        meleeAbility.OnFinished += OnAbilityFinished;

        if (IsOwner)
        {
            if (abilityHUD == null)
            {
                abilityHUD = context.AttachUIWidget(abilityHUDPrefab);
            }

            abilityHUD.Initialize(meleeAbility, ability1, ability2, dodgeAbility);

            customGameInstance.OnAbilitiesChanged += OnAbilitiesChanged;
        }
    }

    private void OnAbilitiesChanged(AbilitySO ability1, AbilitySO ability2)
    {
        SyncAbilitiesServerRpc(
            customGameInstance.AllMelee.GetIndexByAbility(customGameInstance.Melee),
            customGameInstance.AllAbilities.GetIndexByAbility(ability1),
            customGameInstance.AllAbilities.GetIndexByAbility(ability2),
            new ServerRpcParams()
            {
                Receive = new ServerRpcReceiveParams()
                {
                    SenderClientId = OwnerClientId
                }
            });
    }

    private void OnToggleInput(bool obj)
    {
        toggledInput = obj;
    }

    public void AnimationApex()
    {
        OnAbilityApexed?.Invoke(currentAbility);
    }

    private void OnAbility1(InputAction.CallbackContext context)
    {
        AbilityConfig(1);
    }

    private void OnAbility2(InputAction.CallbackContext context)
    {
        AbilityConfig(2);
    }

    private void OnAbility3(InputAction.CallbackContext context)
    {
        AbilityConfig(3);
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
            case 3:
                dodgeAbility.RequestUseAbilityServerRpc(serverParams);
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

    public int GetIndexFromAbility(Ability ability)
    {
        if (ability == meleeAbility)
        {
            return 0;
        }
        else if (ability == ability1)
        {
            return 1;
        }
        else if (ability == ability2)
        {
            return 2;
        }
        else if (ability == dodgeAbility)
        {
            return 3;
        }

        return -1;
    }

    public Ability GetAbilityFromIndex(int index)
    {
        switch (index)
        {
            case 0:
                return meleeAbility;
            case 1:
                return ability1;
            case 2:
                return ability2;
            case 3:
                return dodgeAbility;
        }
        return null;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (meleeAbility.IsSpawned)
                meleeAbility?.GetComponent<NetworkObject>().Despawn(true);

            if (ability1.IsSpawned)
                ability1?.GetComponent<NetworkObject>().Despawn(true);

            if (ability2.IsSpawned)
                ability2?.GetComponent<NetworkObject>().Despawn(true);

            if (dodgeAbility.IsSpawned)
                dodgeAbility?.GetComponent<NetworkObject>().Despawn(true);
        }

        if (IsClient)
        {
            if (context == null) return;

            context.GetDefaultInputMap().FindAction("Ability1").performed -= OnAbility1;
            context.GetDefaultInputMap().FindAction("Ability2").performed -= OnAbility2;
            context.GetDefaultInputMap().FindAction("Dodge").performed -= OnAbility3;
        }

        base.OnNetworkDespawn();
    }

    public override void DeInitialize(P_PlayerPawn context)
    {
        context.DettachUIWidget(abilityHUD);

        base.DeInitialize(context);
    }
}

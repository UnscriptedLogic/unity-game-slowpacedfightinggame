using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;
using UnscriptedEngine;

namespace UM
{
    public class LobbyManager : ULevelObject
    {
        private PayloadAllocation payloadAllocation;
        private string backfillTicketID;
        private float acceptBackFillTimer;
        private float acceptBackFillInterval = 2f;

        private GM_MultiplayerMode multiplayerGameMode;

        public static LobbyManager Instance { get; private set; }

#if DEDICATED_SERVER
        private static IServerQueryHandler serverQueryHandler;
#endif

        protected override void OnLevelStarted()
        {
            base.OnLevelStarted();

            multiplayerGameMode = GameMode as GM_MultiplayerMode;
        }

        private void Start()
        {
            Instance = this;
        }

#if DEDICATED_SERVER
        protected override IEnumerator AddToLevelLoading(UGameModeBase.LoadProcess process)
        {
            Debug.Log("[SERVER] Waiting to load...");

            process = new UGameModeBase.LoadProcess();

            process.process = WaitForInitialization();

            yield return process.process;

            GameMode.AddLoadingProcess(process);
        }

        private void Update()
        {
            if (serverQueryHandler != null)
            {
                serverQueryHandler.UpdateServerCheck();
            }
        }

        public IEnumerator WaitForInitialization()
        {
            Initialization();

            yield return new WaitUntil(() => UnityServices.State == ServicesInitializationState.Initialized);
        } 
#endif

        private void HandleBackFill()
        {
            //check if the server is full
            if (NetworkManager.Singleton.ConnectedClientsList.Count < 6)
            {
                Debug.Log($"[SERVER] Server is full, rejecting backfill ticket {backfillTicketID}");
                MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketID);
            }
        }

        private async void UpdateBackFill()
        {
            if (backfillTicketID == null) return;

            List<Player> playerList = new List<Player>();
            foreach (NetworkClient playerData in NetworkManager.Singleton.ConnectedClientsList)
            {
                playerList.Add(new Player(playerData.ClientId.ToString()));
            }

            MatchProperties matchProperties = new MatchProperties
                (
                    payloadAllocation.MatchProperties.Teams,
                    playerList,
                    payloadAllocation.MatchProperties.Region,
                    payloadAllocation.MatchProperties.BackfillTicketId
                );

            try
            {
                await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId, new BackfillTicket(
                    backfillTicketID, properties: new BackfillTicketProperties(matchProperties)
                    ));
            }
            catch (MatchmakerServiceException e)
            {
                Debug.Log("[MatchMaker Error] " + e);
                throw;
            }
        }

        public async void Initialization()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                InitializationOptions options = new InitializationOptions();
                options.SetEnvironmentName("production");

                await UnityServices.InitializeAsync(options);
                Debug.Log("[SERVER] UnityServices:" + UnityServices.State);

#if !DEDICATED_SERVER
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[Client] SignInAnonymouslyAsync failed...");
#endif
            }

#if DEDICATED_SERVER

            Debug.Log("DEDICATED SERVER: INITIALIZING");

            MultiplayEventCallbacks multiplayEventCallbacks = new MultiplayEventCallbacks();
            multiplayEventCallbacks.Allocate += MP_OnAllocate;
            multiplayEventCallbacks.Deallocate += MP_OnDeallocate;
            multiplayEventCallbacks.Error += MP_OnError;
            multiplayEventCallbacks.SubscriptionStateChanged += MP_OnSubscriptionStateChanged;
            IServerEvents serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(multiplayEventCallbacks);

            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(6, "TestServer", "TestServer", "someid", "map");

            ServerConfig serverConfig = MultiplayService.Instance.ServerConfig;
            if (serverConfig.AllocationId != "")
            {
                Debug.Log("DEDICATED SERVER: ALLOCATED");

                MP_OnAllocate(new MultiplayAllocation(
                    "",
                    serverConfig.ServerId,
                    serverConfig.AllocationId
                    ));
            }
            else
            {
                Debug.Log("DEDICATED SERVER: NOT ALLOCATED");
            }
#endif
            
        }

        private async void SetUpBackFillTickets()
        {
            Debug.Log($"[SERVER] Setting Up Back Fill Tickets...");

            payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();

            backfillTicketID = payloadAllocation.BackfillTicketId;

            acceptBackFillTimer = acceptBackFillInterval;
        }

        private void MP_OnSubscriptionStateChanged(MultiplayServerSubscriptionState state)
        {
            Debug.Log($"MULTIPLAY_SUBSCRIPTIONCHANGED: {state}");
        }

        private void MP_OnError(MultiplayError error)
        {
            Debug.Log($"MULTIPLAY_ERROR: {error.Reason}");
        }

        private void MP_OnDeallocate(MultiplayDeallocation deallocation)
        {
            Debug.Log("MULTIPLAY_DEALLOCATE");
        }

        private void MP_OnAllocate(MultiplayAllocation allocation)
        {
            ServerConfig config = MultiplayService.Instance.ServerConfig;
            Debug.Log($"Server ID[{config.ServerId}]");
            Debug.Log($"Allocation[{config.AllocationId}]");
            Debug.Log($"Port[{config.Port}]");
            Debug.Log($"QueryPort[{config.QueryPort}]");
            Debug.Log($"Log Directory[{config.ServerLogDirectory}]");

            string ipv4Address = "0.0.0.0";
            ushort port = config.Port;
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipv4Address, port, "0.0.0.0");
            NetworkManager.Singleton.StartServer();

            SetUpBackFillTickets();
        }
    }

    public class PayloadAllocation
    {
        public Unity.Services.Matchmaker.Models.MatchProperties MatchProperties;
        public string GeneratorName;
        public string QueueName;
        public string PoolName;
        public string EnvironmentId;
        public string BackfillTicketId;
        public string MatchId;
        public string PoolId;
    }
}
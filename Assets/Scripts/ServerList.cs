using DG.Tweening;
using System;
using System.Collections;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnscriptedEngine;

[Serializable]
public class ListServers
{
    public Server[] allocations;
}

[Serializable]
public class Server
{
    public string allocationId;
    public int buildConfigurationId;
    public string created;
    public string fleetId;
    public string fulfilled;
    public int gamePort;
    public string ipv4;
    public string ipv6;
    public int machineId;
    public bool readiness;
    public string ready;
    public string regionID;
    public int requestId;
    public string requested;
    public int serverId;
}

public enum ServerStatus
{
    AVAILABLE,
    ONLINE,
    ALLOCATED
}

public class ServerList : ULevelObject
{
    private CustomGameInstance gameInstance;

    [SerializeField] private Transform serverBtnParent;
    [SerializeField] private ServerBtn serverBtn;
    [SerializeField] private GameObject noServerText;
    [SerializeField] private Button refreshBtn;
    [SerializeField] private RectTransform lookingForMatch;
    [SerializeField] private Button findMatchBtn;

    private string accessToken;
    //private string secret = "vDz4Mq3LDX5Yqe5eOxiTpZuw1ztAitB9";
    private string KeyBase64 = "OTBhMTcwMmYtNWVkZi00YjRiLThlOGUtMTBhMWM5MGE4Y2M4OnZEejRNcTNMRFg1WXFlNWVPeGlUcFp1dzF6dEFpdEI5";

    private const string projectID = "1873a06d-f8ae-4133-8e20-daf42201bcbb";
    private const string environmentID = "08518fb0-ef1d-49da-938e-c1d9851d6b6b";
    private const string fleetId = "02b0f18b-b036-41a5-8026-83929a863492";
    private string listFleetURL = $"https://multiplay.services.api.unity.com/v1/allocations/projects/{projectID}/environments/{environmentID}/fleets/{fleetId}/allocations";
    private CreateTicketResponse createTicketResponse;
    private float pollTicketInveral = 1.25f;
    private float pollTicketTimer;

    protected override void Awake()
    {
        base.Awake();

        findMatchBtn.interactable = false;
        refreshBtn.interactable = false;
    }

    private void Start()
    {
        gameInstance = GameMode.GetGameInstance<CustomGameInstance>();

        StartCoroutine(ServicesInitialized());
    }

    private IEnumerator ServicesInitialized()
    {
        while (UnityServices.State != ServicesInitializationState.Initialized)
        {
            yield return null;
        }

        findMatchBtn.interactable = true;
        refreshBtn.interactable = true;

        LoadServerList();
    }

    public void LoadServerList()
    {
        StartCoroutine(TokenExchange(accessToken =>
        {
            StartCoroutine(GetServerList());
        }));
    }

    public void TemporarilyHideRefreshBtn()
    {
        refreshBtn.interactable = false;
        StartCoroutine(ShowRefreshBtn());
    }

    private IEnumerator ShowRefreshBtn()
    {
        yield return new WaitForSeconds(5f);
        refreshBtn.interactable = true;
    }

    private IEnumerator GetServerList()
    {
        Debug.Log("[SERVER] Fetching Servers...");

        //Clear all server buttons
        foreach (Transform child in serverBtnParent)
        {
            Destroy(child.gameObject);
        }

        UnityWebRequest request = UnityWebRequest.Get(listFleetURL);
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        yield return request.SendWebRequest();

        while (!request.isDone)
        {
            yield return null;
        }

        Debug.Log("[SERVER] Fetch Status: " + request.result);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("[SERVER ERROR] " + request.downloadHandler.text);
            yield break;
        }

        Debug.Log(request.downloadHandler.text);
        ListServers listServers = JsonUtility.FromJson<ListServers>(request.downloadHandler.text);

        foreach (Server server in listServers.allocations)
        {
            ServerBtn newServerBtn = Instantiate(serverBtn, serverBtnParent);
            newServerBtn.SetServer(server, OnJoinServer);
        }

        noServerText.SetActive(serverBtnParent.childCount <= 0);
    }

    private void Update()
    {
        if (createTicketResponse != null)
        {
            pollTicketTimer -= Time.deltaTime;
            if (pollTicketTimer <= 0)
            {
                PollTicket();
                pollTicketTimer = pollTicketInveral;

            }
        }
    }

    private async void PollTicket()
    {
        Debug.Log("[CLIENT] Polling...");

        TicketStatusResponse response = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);

        if (response == null)
        {
            Debug.Log("[CLIENT] Waiting...");
            return;
        }

        if (response.Type == typeof(MultiplayAssignment))
        {
            MultiplayAssignment assignment = response.Value as MultiplayAssignment;

            Debug.Log("[CLIENT] Ticket Assigned..." + assignment.Status);

            switch (assignment.Status)
            {
                case MultiplayAssignment.StatusOptions.Timeout:
                    createTicketResponse = null;
                    lookingForMatch.gameObject.SetActive(false);
                    Debug.Log("[CLIENT] Timeout boss, I'm tired...");

                    NotificationManager.Create("TimeOut boss, I'm tired. (Ticket Time Out)").SetDuration(3f).SetSeverity(Notification.Severity.Warning).Show();

                    break;
                case MultiplayAssignment.StatusOptions.Failed:
                    createTicketResponse = null;
                    lookingForMatch.gameObject.SetActive(false);
                    Debug.Log("[CLIENT] Failed to create server! What the hell did you do??");
                    NotificationManager.Create("Failed to find a match").SetDuration(3f).SetSeverity(Notification.Severity.Error).Show();

                    break;
                case MultiplayAssignment.StatusOptions.InProgress:
                    Debug.Log("[CLIENT] Still Waiting...");
                    NotificationManager.Create("Finding Match...").SetDuration(1.5f).SetSeverity(Notification.Severity.Info).Show();

                    break;
                case MultiplayAssignment.StatusOptions.Found:
                    NotificationManager.Create("Match Found").SetDuration(1f).SetSeverity(Notification.Severity.Success).Show();

                    createTicketResponse = null;

                    gameInstance.IP = assignment.Ip;
                    gameInstance.Port = (ushort)assignment.Port;
                    gameInstance.SetStartAsClient();
                    GameMode.LoadScene(1);

                    break;
                default:
                    break;
            }
        }
    }

    private class Token
    {
        public string accessToken;
    }

    private class TokenExchangePayload
    {
        public string projectId;
        public string environmentId;
    }

    private IEnumerator TokenExchange(Action<string> OnSuccess = null)
    {
        Debug.Log("[SERVER] EXCHANGING TOKEN...");

        string requestURI = "https://services.api.unity.com/auth/v1/token-exchange";
        UriBuilder uriBuilder = new UriBuilder(requestURI);
        uriBuilder.Query += $"projectId={projectID}";
        uriBuilder.Query += $"&environmentId={environmentID}";

        UnityWebRequest request = new UnityWebRequest(uriBuilder.Uri, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Basic " + KeyBase64);
        yield return request.SendWebRequest();

        while (!request.isDone)
        {   
            yield return null;
        }
        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.downloadHandler.text == null)
            {
                Debug.Log("[SERVER ERROR] " + request.downloadHandler.error);
                accessToken = string.Empty;
                yield break;
            }

            Debug.Log("[SERVER ERROR] " + request.downloadHandler.text);
            accessToken = string.Empty;
            yield break;
        }

        Token token = JsonUtility.FromJson<Token>(request.downloadHandler.text);
        accessToken = token.accessToken;
        OnSuccess?.Invoke(JsonUtility.ToJson(request.downloadHandler.text));

        Debug.Log("[SERVER] Access Toke: " + accessToken);
    }

    private class AllocationPayload
    {
        public string allocationId;
        public int buildConfigurationId;
        public string payload;
        public string regionId;
        public bool restart;
    }

    private void OnJoinServer(Server server)
    {
        gameInstance = GameMode.GetGameInstance<CustomGameInstance>();
        gameInstance.server = server;
        gameInstance.IP = server.ipv4;
        gameInstance.Port = (ushort)server.gamePort;
        gameInstance.SetStartAsClient();
        GameMode.LoadScene(1);
    }

    public async void FindMatch()
    {
        Debug.Log("[CLIENT] Finding Match...");

        lookingForMatch.gameObject.SetActive(true);

        createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(new System.Collections.Generic.List<Player>
        {
            new Unity.Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId),
        }, new CreateTicketOptions
        {
            QueueName = "DefaultQueue",
        });

        Debug.Log("[CLIENT] Ticket Created...");

        pollTicketTimer = pollTicketInveral;
    }
}
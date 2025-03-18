using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using UnityEngine;

public class MultiplayHandler : MonoBehaviour
{
    private enum LogSender
    {
        Server,
        Client
    }

    private IServerQueryHandler serverQueryHandler;

    private bool doLogging = true;

    private async void Start()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Application.targetFrameRate = 60;
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            ServerConfig serverConfig = MultiplayService.Instance.ServerConfig;

            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(6, "TestServer", "TestServer", "0", "TestMap");

            if (serverConfig.AllocationId != string.Empty)
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", serverConfig.Port, "0.0.0.0");
                NetworkManager.Singleton.StartServer();

                await MultiplayService.Instance.ReadyServerForPlayersAsync();
            }
        }
    }

    private async void Update()
    {

        if (serverQueryHandler != null)
        {
            serverQueryHandler.CurrentPlayers = (ushort)NetworkManager.Singleton.ConnectedClientsList.Count;
            serverQueryHandler.UpdateServerCheck();
            await Task.Delay(100);
        }
    }

    private void Log(LogSender sender, object message)
    {
        if (!doLogging) return;

        string senderStr = sender == LogSender.Server ? "SERVER" : "CLIENT";
        Debug.Log($"[{senderStr}] " + message);
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PingCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textTMP;

    private void Update()
    {
		try
		{
            textTMP.text = $"Ping: {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId)}";

        }
        catch (System.Exception) { }
    }
}

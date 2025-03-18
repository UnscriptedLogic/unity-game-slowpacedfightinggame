using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] private TextMeshProUGUI capacityRegionTMP;
    [SerializeField] private Button joinBtn;

    public void SetServer(Server server, Action<Server> OnClick)
    {
        nameTMP.text = server.serverId.ToString();
        capacityRegionTMP.text = $"Unknown Planet";

        //TODO: JOIN SERVER
        joinBtn.onClick.AddListener(() => OnClick(server));
    }

    private void OnDestroy()
    {
        joinBtn.onClick.RemoveAllListeners();
    }
}

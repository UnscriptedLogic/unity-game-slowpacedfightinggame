using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnscriptedEngine;

public class GM_MainMenu : UGameModeBase
{
    [Header("Extensions")]
    [SerializeField] private UIC_NotificationsHUD notificationPrefab;
    private UIC_NotificationsHUD _notificationsHUD;

    protected override IEnumerator Start()
    {
        yield return UnityServices.InitializeAsync();
        yield return AuthenticationService.Instance.SignInAnonymouslyAsync();

        yield return base.Start();

        _playerPawn.AttachUIWidget(notificationPrefab);

    }
}

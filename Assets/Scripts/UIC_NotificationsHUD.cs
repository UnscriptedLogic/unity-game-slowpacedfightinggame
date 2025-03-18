using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnscriptedEngine;

public class UIC_NotificationsHUD : UCanvasController
{
    private class NotificationSet
    {
        public RectTransform followTransform;
        public NotificationUI notificationUI;
        public float currentDuration;
    }

    [SerializeField] private Color infoColor;
    [SerializeField] private Color warningColor;
    [SerializeField] private Color errorColor;
    [SerializeField] private Color successColor;

    [SerializeField] private NotificationUI notificationPrefab;
    [SerializeField] private RectTransform emptyUIAnchorPrefab;
    [SerializeField] private Transform listParent;

    private List<NotificationSet> notifications = new List<NotificationSet>();

    public Color InfoColor => infoColor;
    public Color WarningColor => warningColor;
    public Color ErrorColor => errorColor;

    public override void OnWidgetAttached(ILevelObject context)
    {
        base.OnWidgetAttached(context);

        NotificationManager.OnNotificationDeployed += OnNotificationDeployed;
    }

    public Color GetColor(Notification.Severity type)
    {
        switch (type)
        {
            case Notification.Severity.Info:
                return infoColor;
            case Notification.Severity.Warning:
                return warningColor;
            case Notification.Severity.Error:
                return errorColor;
            case Notification.Severity.Success:
                return successColor;
            default:
                return Color.white;
        }
    }

    private void Update()
    {
        for (int i = notifications.Count; i-- > 0;)
        {
            NotificationSet notificationSet = notifications[i];
            if (notificationSet.currentDuration == notificationSet.notificationUI.Notification.duration)
            {

            }

            notificationSet.currentDuration -= Time.deltaTime;
            notificationSet.notificationUI.Slider.value = notificationSet.currentDuration;
            if (notificationSet.currentDuration <= 0)
            {
                RemoveNotification(notificationSet);

                notifications.RemoveAt(i);
                continue;
            }

            if (notificationSet.followTransform != null)
            {
                notificationSet.notificationUI.RectTransform.position = Vector3.Lerp(notificationSet.notificationUI.RectTransform.position, notificationSet.followTransform.position, Time.deltaTime * 10);
            }
        }
    }

    private void RemoveNotification(NotificationSet notificationSet)
    {
        ObjectPooler.DespawnObject(notificationSet.notificationUI.gameObject);
        ObjectPooler.DespawnObject(notificationSet.followTransform.gameObject);
    }

    private void OnNotificationDeployed(Notification notification)
    {
        RectTransform followTransform = ObjectPooler.SpawnObject(emptyUIAnchorPrefab, Vector3.zero, Quaternion.Euler(Vector3.zero), parent: listParent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(listParent as RectTransform);

        NotificationUI notificationUI = ObjectPooler.SpawnObject(notificationPrefab, followTransform.position, Quaternion.Euler(Vector3.zero), parent: transform);
        notificationUI.SetNotification(this, notification);


        NotificationSet notificationSet = new NotificationSet
        {
            followTransform = followTransform,
            notificationUI = notificationUI,
            currentDuration = notification.duration
        };

        notifications.Add(notificationSet);
    }
}
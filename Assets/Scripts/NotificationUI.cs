using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descTMP;
    [SerializeField] private Slider slider;
    [SerializeField] private List<Image> images;

    public Notification Notification { get; private set; }
    public Slider Slider => slider;
    public RectTransform RectTransform => transform as RectTransform;

    public void SetNotification(UIC_NotificationsHUD context, Notification notification)
    {
        Notification = notification;

        descTMP.text = notification.message;
        foreach (Image image in images)
        {
            image.color = context.GetColor(notification.type);
        }

        slider.maxValue = notification.duration;
        slider.value = notification.duration;
    }
}
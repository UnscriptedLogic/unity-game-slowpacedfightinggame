using System;

public class Notification
{
    public enum Severity
    {
        Info,
        Warning,
        Error,
        Success
    }

    public string message;
    public float duration;
    public Severity type;

    public Notification(string message)
    {
        this.message = message;
    }
}

public class NotificationManager
{
    public Notification notification;
    public static Action<Notification> OnNotificationDeployed;

    public static NotificationManager Create(string message)
    {
        NotificationManager notificationManager = new NotificationManager();

        notificationManager.notification = new Notification(message);
        return notificationManager;
    }

    public NotificationManager SetDuration(float duration)
    {
        notification.duration = duration;
        return this;
    }

    public NotificationManager SetSeverity(Notification.Severity severity)
    {
        notification.type = severity;
        return this;
    }

    public void Show()
    {
        OnNotificationDeployed?.Invoke(notification);
        notification = null;
    }
}
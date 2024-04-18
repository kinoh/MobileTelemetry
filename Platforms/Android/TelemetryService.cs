using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;

namespace mobiletelemetry;

[Service]
public class TelemetryService : Service
{
    private string NOTIFICATION_CHANNEL_ID = "7224141";
    private int NOTIFICATION_ID = 1;
    private string NOTIFICATION_CHANNEL_NAME = "notification";

    private void StartForegroundService()
    {
        var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            CreateNotificationChannel(notifcationManager);
        }

        var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
        notification.SetAutoCancel(false);
        notification.SetOngoing(true);
        notification.SetSmallIcon(Resource.Mipmap.appicon);
        notification.SetContentTitle("ForegroundService");
        notification.SetContentText("Foreground Service is running");
        StartForeground(NOTIFICATION_ID, notification.Build());
    }

    private void CreateNotificationChannel(NotificationManager notificationMnaManager)
    {
        NotificationChannel channel = new(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME,
        NotificationImportance.Low);
        notificationMnaManager.CreateNotificationChannel(channel);
    }

    public override IBinder OnBind(Intent? intent)
    {
        return null;
    }


    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        StartForegroundService();

        Task.Run(async () => {
            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(10000);

                TimeSpan timeout = TimeSpan.FromSeconds(10);
                GeolocationRequest request = new(GeolocationAccuracy.Medium, timeout);
                Location? location = await Geolocation.Default.GetLocationAsync(request);

                var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
                notification.SetAutoCancel(false);
                notification.SetOngoing(true);
                notification.SetSmallIcon(Resource.Mipmap.appicon);
                notification.SetContentTitle("ForegroundService");
                notification.SetContentText($"location: {location.Latitude} {location.Longitude}");

                var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                notifcationManager?.Notify(NOTIFICATION_ID, notification.Build());
            }
        });

        return StartCommandResult.Sticky;
    }
}

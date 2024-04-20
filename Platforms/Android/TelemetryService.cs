using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;

namespace mobiletelemetry;

[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class TelemetryService : Service, IRecipient<LogRequestMessage>
{
    private string NOTIFICATION_CHANNEL_ID = "7224141";
    private int NOTIFICATION_ID = 1;
    private string NOTIFICATION_CHANNEL_NAME = "notification";

    private DateTime? lastTick = null;

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
        notification.SetContentTitle("Running");
        notification.SetContentText("Telemetry Service is running");
        StartForeground(NOTIFICATION_ID, notification.Build(), ForegroundService.TypeLocation);
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

    public override void OnCreate()
    {
        base.OnCreate();

        WeakReferenceMessenger.Default.Register(this);
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        StartForegroundService();

        Task.Run(async () => {
    		Secrets secrets = new();
            InfluxDBClient client = new(
                secrets.InfluxDBUrl(),
                secrets.InfluxDBToken(),
                "kinon",
                "geo-tracking",
                new(){
                    { "device_name", DeviceInfo.Current.Name },
                }
            );

            while (true)
            {
                DateTime now = DateTime.Now!;
                if (lastTick != null)
                {
                    TimeSpan elapsed = new(0);
                    elapsed = (DateTime)now - (DateTime)lastTick;
                    await Task.Delay(60000 - elapsed.Milliseconds);
                }
                lastTick = now;

                string status = await Tick(client);

                var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
                notification.SetAutoCancel(false);
                notification.SetOngoing(true);
                notification.SetSmallIcon(Resource.Mipmap.appicon);
                notification.SetContentTitle("Status");
                notification.SetContentText(status);

                var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                notifcationManager?.Notify(NOTIFICATION_ID, notification.Build());
            }
        });

        return StartCommandResult.Sticky;
    }

    private async Task<string> Tick(InfluxDBClient client)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        GeolocationRequest request = new(GeolocationAccuracy.Medium, timeout);
        Location? location = await Geolocation.Default.GetLocationAsync(request);

        if (location == null) {
            return $"error: no location";
        }

        Dictionary<string, string> data = new(){
            { "lat", location.Latitude.ToString() },
            { "lon", location.Longitude.ToString() },
            { "alt", (location.Altitude ?? 0).ToString() },
        };

        try
        {
            await client.Send("gps", data);
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }

        return string.Join(" ", data.Select(pair => pair.Value));
    }

	public void Receive(LogRequestMessage message)
	{
        message.Reply(logs.ToArray());
	}
}

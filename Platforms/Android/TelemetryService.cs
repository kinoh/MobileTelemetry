using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;

namespace mobiletelemetry;

[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class TelemetryService : Service, IRecipient<LogRequestMessage>
{
    private string NOTIFICATION_CHANNEL_ID = "7224141";
    private int NOTIFICATION_ID = 1;
    private string NOTIFICATION_CHANNEL_NAME = "notification";
    private const int MaxLogCount = 1000;

    private DateTime? lastTick = null;
    private List<string> logs = new();

    private void StartForegroundService()
    {
        var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;

        if (notifcationManager == null)
        {
            throw new Exception("notification manager not found");
        }

        CreateNotificationChannel(notifcationManager);

        var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
        notification.SetAutoCancel(false);
        notification.SetOngoing(true);
        notification.SetSmallIcon(Resource.Drawable.AppIcon);
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
        #pragma warning disable CS8603
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

                string status = string.Join(" ", new[]{
                    await TickGps(client),
                    await TickLight(client),
                });

                string timestamp = now.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                logs.Add($"[{timestamp}] {status}");
                if (logs.Count > MaxLogCount)
                {
                    logs.RemoveAt(0);
                }

                var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
                notification.SetAutoCancel(false);
                notification.SetOngoing(true);
                notification.SetSmallIcon(Resource.Drawable.AppIcon);
                notification.SetContentTitle("Status");
                notification.SetContentText(status);

                var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                notifcationManager?.Notify(NOTIFICATION_ID, notification.Build());
            }
        });

        return StartCommandResult.Sticky;
    }

    private async Task<string> TickGps(InfluxDBClient client)
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

    private async Task<string> TickLight(InfluxDBClient client)
    {
		SensorValueRequestMessage message = WeakReferenceMessenger.Default.Send<SensorValueRequestMessage>();

		if (!message.HasReceivedResponse)
        {
            return "no response";
        }

        Dictionary<string, string> data = new(){
            { "light", message.Response.ToString() },
        };

        try
        {
            await client.Send("light", data);
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

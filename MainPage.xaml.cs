using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace mobiletelemetry;

public partial class MainPage : ContentPage
{
	private InfluxDBClient _client;

	public MainPage()
	{
		InitializeComponent();

		Secrets secrets = new();

		_client = new(
			secrets.InfluxDBUrl(),
			secrets.InfluxDBToken(),
			"kinon",
			"geo-tracking",
			new(){
				{ "device_name", "test" },
			}
		);
	}

	private async void OnCounterClicked(object sender, EventArgs e)
	{
		#if ANDROID
			if (!AndroidX.Core.App.NotificationManagerCompat.From(Platform.CurrentActivity).AreNotificationsEnabled())
			{
				await Toast.Make("notification unavailable", ToastDuration.Short, 14).Show();
				return;
			}

			Android.Content.Intent intent = new(Android.App.Application.Context, typeof(TelemetryService));
			Android.App.Application.Context.StartForegroundService(intent);
			await Toast.Make("foreground service started", ToastDuration.Short, 14).Show();
		#endif
		return;

		var result = await GetCurrentLocation();

		if (result == null)
		{
			MessageLabel.Text = "N/A";
			SemanticScreenReader.Announce("N/A");
		}
		else
		{
			string s = $"lat={result.Latitude:F6} long={result.Longitude:F6} alt={result.Altitude:F6}";

			MessageLabel.Text = s;
			SemanticScreenReader.Announce(s);

			Dictionary<string, string> data = new(){
				{ "lat", result.Latitude.ToString() },
				{ "long", result.Longitude.ToString() },
				{ "alt", (result.Altitude ?? 0).ToString() },
			};

			try
			{
				await _client.Send("gps", data);
			}
			catch (Exception ex)
			{
				NotifyError(ex);
			}
		}
	}

	public async Task<Location?> GetCurrentLocation()
	{
		try
		{
			TimeSpan timeout = TimeSpan.FromSeconds(10);
			GeolocationRequest request = new(GeolocationAccuracy.Medium, timeout);

			Location? location = await Geolocation.Default.GetLocationAsync(request);

			if (location != null)
				return location;
		}
		catch (Exception ex)
		{
			NotifyError(ex);
		}

		return null;
	}

	private async void NotifyError(Exception ex)
	{
		string text = $"error: {ex.Message}";
		await Toast.Make(text, ToastDuration.Short, 14).Show();
		SemanticScreenReader.Announce(text);
	}
}


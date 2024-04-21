using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;

namespace mobiletelemetry;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void OnStartServiceClicked(object sender, EventArgs e)
	{
		#if ANDROID
			Android.Content.Context? context = Platform.CurrentActivity;
			if (context == null)
			{
				Toast.Make("no activity found", ToastDuration.Short, 14).Show();
				return;
			}

			if (!AndroidX.Core.App.NotificationManagerCompat.From(context).AreNotificationsEnabled())
			{
				Toast.Make("notification unavailable", ToastDuration.Short, 14).Show();
				return;
			}

			Android.Content.Intent intent = new(Android.App.Application.Context, typeof(TelemetryService));
			Android.App.Application.Context.StartForegroundService(intent);
			Toast.Make("foreground service started", ToastDuration.Short, 14).Show();
		#endif
	}

	private void OnGetLogsClicked(object sender, EventArgs e)
	{
		LogRequestMessage logs = WeakReferenceMessenger.Default.Send<LogRequestMessage>();

		if (logs.HasReceivedResponse)
		{
			MessageLabel.Text = string.Join("\n", logs.Response);
		}
		else
		{
			MessageLabel.Text = "no response";
		}
	}

	private async void NotifyError(Exception ex)
	{
		string text = $"error: {ex.Message}";
		await Toast.Make(text, ToastDuration.Short, 14).Show();
		SemanticScreenReader.Announce(text);
	}
}


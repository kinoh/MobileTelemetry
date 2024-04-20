﻿using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;

namespace mobiletelemetry;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
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
	}

	private async void OnGetLogsClicked(object sender, EventArgs e)
	{
		LogRequestMessage logs = WeakReferenceMessenger.Default.Send<LogRequestMessage>();

		if (logs.HasReceivedResponse)
		{
			MessageLabel.Text = string.Join("\n", logs.Response);
		}
		else
		{
			await Toast.Make("no response", ToastDuration.Short, 14).Show();
		}
	}

	private async void NotifyError(Exception ex)
	{
		string text = $"error: {ex.Message}";
		await Toast.Make(text, ToastDuration.Short, 14).Show();
		SemanticScreenReader.Announce(text);
	}
}


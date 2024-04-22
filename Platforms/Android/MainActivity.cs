using Android.App;
using Android.Content.PM;
using Android.OS;

namespace mobiletelemetry;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public partial class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        InitializeLightSensor();

        if ((int)Build.VERSION.SdkInt >= 33)
        {
            const int requestNotification = 0;
            string[] notificationPermission = {
                Android.Manifest.Permission.PostNotifications
            };

            if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
            {
                RequestPermissions(notificationPermission, requestNotification);
            }
        }
    }
}

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Hardware;
using CommunityToolkit.Mvvm.Messaging;

namespace mobiletelemetry;

public partial class MainActivity : Android.Hardware.ISensorEventListener, IRecipient<SensorValueRequestMessage>
{
    float? _lastValue;

    public void InitializeLightSensor()
    {
        if (GetSystemService(Context.SensorService) is not SensorManager manager)
        {
            throw new Exception("failed to get sensor manager");
        }

        Sensor sensor = manager.GetDefaultSensor(SensorType.Light);
        manager.RegisterListener(this, sensor, SensorDelay.Normal);

        WeakReferenceMessenger.Default.Register(this);
    }

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e is not null)
        {
            _lastValue = e.Values[0];
        }
    }

    public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy)
    {
    }

	public void Receive(SensorValueRequestMessage message)
	{
        if (_lastValue is not null)
        {
            message.Reply((float) _lastValue);
        }
	}
}

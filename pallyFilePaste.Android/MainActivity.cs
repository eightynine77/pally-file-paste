using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace pallyFilePaste.Android;

[Activity(
    Label = "pally file paste",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        pallyFilePaste.Views.MainView.NativeAndroidSaver = new AndroidImageSaver();

        // Request write permission only on Android 6–9.
        // Android 10+ uses MediaStore and does not need this permission.
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M &&
            Build.VERSION.SdkInt <= BuildVersionCodes.P)
        {
            if (CheckSelfPermission(
                    global::Android.Manifest.Permission.WriteExternalStorage)
                != Permission.Granted)
            {
                RequestPermissions(
                    new[]
                    {
                        global::Android.Manifest.Permission.WriteExternalStorage
                    },
                    0);
            }
        }
    }
}
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
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        pallyFilePaste.Views.MainView.NativeAndroidSaver = new AndroidImageSaver();

        // Request file permissions dynamically on Android 6.0 and above
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            if (this.CheckSelfPermission(global::Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted ||
                this.CheckSelfPermission(global::Android.Manifest.Permission.ReadExternalStorage) != Permission.Granted)
            {
                this.RequestPermissions(new[] 
                { 
                    global::Android.Manifest.Permission.WriteExternalStorage, 
                    global::Android.Manifest.Permission.ReadExternalStorage 
                }, 0);
            }
        }
    }
}
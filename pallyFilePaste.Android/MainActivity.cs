using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Android.Runtime; // Add this

namespace pallyFilePaste.Android;

[Activity(
    Label = "pally file paste",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private const int STORAGE_PERMISSION_CODE = 1001;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        pallyFilePaste.Views.MainView.NativeAndroidSaver = new AndroidImageSaver();
        
        // Link the Avalonia button to our Android request function
        pallyFilePaste.Views.MainView.RequestPermissionAction = RequestStoragePermission;

        RequestStoragePermission();
    }

    private void RequestStoragePermission()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M &&
            Build.VERSION.SdkInt <= BuildVersionCodes.P)
        {
            if (CheckSelfPermission(global::Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                RequestPermissions(new[] { global::Android.Manifest.Permission.WriteExternalStorage }, STORAGE_PERMISSION_CODE);
            }
            else
            {
                pallyFilePaste.Views.MainView.Instance?.HidePermissionWarning();
            }
        }
    }

    // Capture the result of the permission prompt
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == STORAGE_PERMISSION_CODE)
        {
            if (grantResults.Length > 0 && grantResults[0] == Permission.Denied)
            {
                // User tapped "no" or tapped away, show the blocking overlay
                pallyFilePaste.Views.MainView.Instance?.ShowPermissionWarning();
            }
            else if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
            {
                // Permission granted, hide the overlay just in case it was open
                pallyFilePaste.Views.MainView.Instance?.HidePermissionWarning();
            }
        }
    }
}
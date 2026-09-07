using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace pallyFilePaste.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(
            nint javaReference,
            JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            AndroidBulkPasteTextBoxHandler.Register();

            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }
    }
}
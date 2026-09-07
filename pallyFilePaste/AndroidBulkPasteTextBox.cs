using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace pallyFilePaste;

public class AndroidBulkPasteTextBox : NativeControlHost
{
    public event EventHandler<Bitmap>? ImagePasted;
    public event EventHandler? TextPasted;

    public void RaiseImagePasted(Bitmap bitmap)
    {
        ImagePasted?.Invoke(this, bitmap);
    }

    public void RaiseTextPasted()
    {
        TextPasted?.Invoke(this, EventArgs.Empty);
    }

    public static Func<
        AndroidBulkPasteTextBox,
        Avalonia.Platform.IPlatformHandle,
        Avalonia.Platform.IPlatformHandle>?
        NativeControlFactory { get; set; }

    protected override Avalonia.Platform.IPlatformHandle
        CreateNativeControlCore(
            Avalonia.Platform.IPlatformHandle parent)
    {
        if (NativeControlFactory == null)
        {
            return base.CreateNativeControlCore(parent);
        }

        return NativeControlFactory(this, parent);
    }
}
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
using System.Threading.Tasks;
#endif

namespace pallyFilePaste.Views;

public partial class MainView : UserControl
{
    private bool _isProcessingPaste = false;

    public ObservableCollection<PastedImage> BulkImages { get; } = new();

    public MainView()
    {
        InitializeComponent();

        BulkPasteListBox.ItemsSource = BulkImages;

        AndroidBulkPasteHost.ImagePasted +=
            AndroidBulkPasteHost_ImagePasted;

        AndroidBulkPasteHost.TextPasted +=
            AndroidBulkPasteHost_TextPasted;
    }

    public static IAndroidImageSaver? NativeAndroidSaver { get; set; }

    private async void SinglePasteButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is null) return;

            var bitmap = await topLevel.Clipboard.TryGetBitmapAsync();
            if (bitmap != null)
            {
                if (OperatingSystem.IsAndroid() && NativeAndroidSaver != null)
                {
                    SingleStatusText.Text = "Saving to Native MediaStore...";
                    SingleStatusText.Foreground = Brushes.Yellow;

                    await NativeAndroidSaver.SaveImageAsync(bitmap);
                
                    SingleStatusText.Text = "Success! Saved image to Pictures/pallyFilePaste";
                    SingleStatusText.Foreground = Brushes.Green;
                }
                else
                {
                    SingleStatusText.Text = "Saved using Desktop StorageProvider.";
                    SingleStatusText.Foreground = Brushes.Green;
                }
            }       
            else
            {
                SingleStatusText.Text = "No image found in clipboard!";
                SingleStatusText.Foreground = Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            SingleStatusText.Text = $"Error: {ex.Message}";
            SingleStatusText.Foreground = Brushes.Red;
        }
    
        SingleStatusText.IsVisible = true;
    }

    private void BulkPasteListBox_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        #if ANDROID
            // Android uses the native EditText.
        #else
            BulkPasteTextBox.Focus();
        #endif
    }

    private async void BulkPasteTextBox_PastingFromClipboard(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        await AddImageFromClipboardAsync();
    }

    private async void BulkPasteTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isProcessingPaste || string.IsNullOrEmpty(BulkPasteTextBox.Text)) return;

        _isProcessingPaste = true;
        BulkPasteTextBox.Text = string.Empty;
        await AddImageFromClipboardAsync();
        _isProcessingPaste = false;
    }

    private async void AndroidBulkPasteHost_TextPasted(
        object? sender,
        EventArgs e)
    {
        await AddImageFromClipboardAsync();
    }

    private async void AndroidBulkPasteHost_ImagePasted(
        object? sender,
        Bitmap bitmap)
    {
        await AddBitmapToBulkListAsync(bitmap);
    }

    private async Task AddBitmapToBulkListAsync(Bitmap bitmap)
    {
        void Add()
        {
            BulkImages.Add(new PastedImage
            {
                Image = bitmap,
                FileName = $"img_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"
            });

            BulkStatusText.Text = $"Pasted item #{BulkImages.Count}";
            BulkStatusText.Foreground = Brushes.Green;
            BulkStatusText.IsVisible = true;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Add();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(Add);
        }
    }

    private void HamburgerButton_Click(object? sender, RoutedEventArgs e)
    {
        // Toggles the menu open and closed
        NavSplitView.IsPaneOpen = !NavSplitView.IsPaneOpen;
    }

    private void NavSplitView_PaneOpening(
        object? sender,
        CancelRoutedEventArgs e)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        var paneWidth = NavSplitView.OpenPaneLength;

        // Native Android views cannot be layered beneath Avalonia content.
        // Offset the entire content layout (without reducing its width) so the
        // native EditText moves clear of the overlay drawer.
        MainContentGrid.Margin = new Thickness(paneWidth, 0, -paneWidth, 0);
    }

    private void NavSplitView_PaneClosing(
        object? sender,
        CancelRoutedEventArgs e)
    {
        if (OperatingSystem.IsAndroid())
            MainContentGrid.Margin = default;
    }

    private void NavHome_Click(object? sender, RoutedEventArgs e)
    {
        NavSplitView.IsPaneOpen = false;
        HomeContentView.IsVisible = true;
        AboutContentView.IsVisible = false;
    }

    private void NavAbout_Click(object? sender, RoutedEventArgs e)
    {
        NavSplitView.IsPaneOpen = false;
        HomeContentView.IsVisible = false;
        AboutContentView.IsVisible = true;
    }

    private async Task AddImageFromClipboardAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is null) return;

            var bitmap = await topLevel.Clipboard.TryGetBitmapAsync();
            if (bitmap != null)
            {
                await AddBitmapToBulkListAsync(bitmap);
            }
            else
            {
                BulkStatusText.Text = "No image in clipboard to paste!";
                BulkStatusText.Foreground = Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            BulkStatusText.Text = $"Error: {ex.Message}";
            BulkStatusText.Foreground = Brushes.Red;
        }

        BulkStatusText.IsVisible = true;
    }

    private async void BulkSaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (BulkImages.Count == 0)
        {
            BulkStatusText.Text = "No images to save!";
            BulkStatusText.Foreground = Brushes.Red;
            BulkStatusText.IsVisible = true;
            return;
        }

        try
        {
            if (OperatingSystem.IsAndroid() && NativeAndroidSaver != null)
            {
                BulkStatusText.Text = "Saving bulk to Native MediaStore...";
                BulkStatusText.Foreground = Brushes.Yellow;
                BulkStatusText.IsVisible = true;

                var bitmaps = BulkImages.Where(b => b.Image != null).Select(b => b.Image!).ToList();
                await NativeAndroidSaver.SaveImagesAsync(bitmaps);

                BulkStatusText.Text = $"Success! Saved {bitmaps.Count} images to Pictures/pallyFilePaste";
                BulkStatusText.Foreground = Brushes.Green;
                BulkImages.Clear();
            }
        }
        catch (Exception ex)
        {
            BulkStatusText.Text = $"Error: {ex.Message}";
            BulkStatusText.Foreground = Brushes.Red;
            BulkStatusText.IsVisible = true;
        }
    }

    public class PastedImage
    {
        public Bitmap? Image { get; set; }
        public string? FileName { get; set; }
    }
}

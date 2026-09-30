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
using System.IO;
#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
using System.Threading.Tasks;
#endif

namespace pallyFilePaste.Views;

public partial class MainView : UserControl
{
    private const string AppFolderName = "pallyFilePaste";
    private bool _isProcessingPaste = false;
    private const string SaveFolderFileName = "save-folder.txt";

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

    private static string GetAppDataFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create),
            AppFolderName);
    }

    private static string GetFallbackImageFolder()
    {
        return Path.Combine(
            GetAppDataFolder(),
            "Images");
    }

    private static void RememberSaveFolder(string folder)
    {
        var appDataFolder = GetAppDataFolder();

        Directory.CreateDirectory(appDataFolder);

        var settingsPath = Path.Combine(
            appDataFolder,
            SaveFolderFileName);

        File.WriteAllText(settingsPath, folder);
    }

    private static string? GetRememberedSaveFolder()
    {
        var settingsPath = Path.Combine(
            GetAppDataFolder(),
            SaveFolderFileName);

        if (!File.Exists(settingsPath))
            return null;

        var folder = File.ReadAllText(settingsPath).Trim();

        return string.IsNullOrWhiteSpace(folder)
            ? null
            : folder;
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
                    var savedImage = SaveDesktopImage(bitmap);

                    SingleStatusText.Text = savedImage.UsedFallback
                    ? $"Saved to AppData:\n{savedImage.FilePath}"
                    : $"Saved to Documents:\n{savedImage.FilePath}";
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

    private static (string FilePath, bool UsedFallback)
        SaveDesktopImage(Bitmap bitmap)
    {
        var fileName =
            $"image_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";

        // 1. Check whether we already decided where to save.
        var rememberedFolder = GetRememberedSaveFolder();

        if (rememberedFolder != null)
        {
            try
            {
                var filePath = SaveBitmapToFolder(
                    bitmap,
                    rememberedFolder,
                    fileName);

                var usedFallback =
                    string.Equals(
                        rememberedFolder,
                        GetFallbackImageFolder(),
                        StringComparison.OrdinalIgnoreCase);

                return (filePath, usedFallback);
            }
            catch (UnauthorizedAccessException)
                when (OperatingSystem.IsWindows())
            {
                // The remembered folder is no longer usable.
                // Fall through and choose a new location.
            }
        }

        // 2. No remembered location yet (or it stopped working).
        var documentsFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments),
            AppFolderName);

        try
        {
            var filePath = SaveBitmapToFolder(
                bitmap,
                documentsFolder,
                fileName);

            // Documents worked, so remember it.
            RememberSaveFolder(documentsFolder);

            return (filePath, false);
        }
        catch (UnauthorizedAccessException)
            when (OperatingSystem.IsWindows())
        {
            // Windows rejected Documents.
            // Switch permanently to our per-user AppData location.
            var fallbackFolder = GetFallbackImageFolder();

            var filePath = SaveBitmapToFolder(
                bitmap,
                fallbackFolder,
                fileName);

            // Remember the fallback location.
            RememberSaveFolder(fallbackFolder);

            return (filePath, true);
        }
    }
    private static string SaveBitmapToFolder(Bitmap bitmap, string folder, string fileName)
    {
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, fileName);
        bitmap.Save(filePath);
        return filePath;
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
        NavHomeButton.Background = Brushes.LightGray;
        NavAboutButton.Background = Brushes.Transparent;
    }

    private void NavAbout_Click(object? sender, RoutedEventArgs e)
    {
        NavSplitView.IsPaneOpen = false;
        HomeContentView.IsVisible = false;
        AboutContentView.IsVisible = true;
        NavHomeButton.Background = Brushes.Transparent;
        NavAboutButton.Background = Brushes.LightGray;
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

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace pallyFilePaste.Views;

public partial class MainWindow : Window
{
    private const string AppFolderName = "pallyFilePaste";
    private bool _isProcessingPaste = false;

    private const string SaveFolderFileName = "save-folder.txt";
    
    public ObservableCollection<PastedImage> BulkImages { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        BulkPasteListBox.ItemsSource = BulkImages;
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

    private static (string FilePath, bool UsedFallback) SaveDesktopImage(Bitmap bitmap)
    {
        var fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";

        var rememberedFolder = GetRememberedSaveFolder();

        if (rememberedFolder != null)
        {
            try
            {
                var filePath = SaveBitmapToFolder(
                    bitmap,
                    rememberedFolder,
                    fileName);

                var usedFallback = string.Equals(
                    rememberedFolder,
                    GetFallbackImageFolder(),
                    StringComparison.OrdinalIgnoreCase);

                return (filePath, usedFallback);
            }
            catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
            {
                // Fallback if remembered folder fails
            }
        }

        var picturesFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyPictures),
            AppFolderName);

        try
        {
            var filePath = SaveBitmapToFolder(
                bitmap,
                picturesFolder,
                fileName);

            RememberSaveFolder(picturesFolder);
            return (filePath, false);
        }
        catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
        {
            var fallbackFolder = GetFallbackImageFolder();

            var filePath = SaveBitmapToFolder(
                bitmap,
                fallbackFolder,
                fileName);

            RememberSaveFolder(fallbackFolder);
            return (filePath, true);
        }
    }

    private void HamburgerButton_Click(object? sender, RoutedEventArgs e)
    {
        // Toggles the menu open and closed
        NavSplitView.IsPaneOpen = !NavSplitView.IsPaneOpen;
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

    private static string SaveBitmapToFolder(Bitmap bitmap, string folder, string fileName)
    {
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);
        bitmap.Save(filePath);
        return filePath;
    }

    private void DiscardButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is PastedImage image)
        {
            BulkImages.Remove(image);
            image.Image?.Dispose();

            if (BulkImages.Count == 0)
            {
                BulkStatusText.IsVisible = false;
            }
            else
            {
                BulkStatusText.Text = $"Pasted item #{BulkImages.Count}";
            }
        }
    }

    private async void SinglePasteButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is null) return;

            var bitmap = await topLevel.Clipboard.TryGetBitmapAsync();
            
            if (bitmap != null)
            {
                var savedImage = SaveDesktopImage(bitmap);
                SingleStatusText.Text = savedImage.UsedFallback
                    ? $"Saved to AppData:\n{savedImage.FilePath}"
                    : $"Saved to Pictures:\n{savedImage.FilePath}";
                SingleStatusText.Foreground = Brushes.Green;
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

    private async Task AddImageFromClipboardAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is null) return;

            var bitmap = await topLevel.Clipboard.TryGetBitmapAsync();
            if (bitmap != null)
            {
                BulkImages.Add(new PastedImage
                {
                    Image = bitmap,
                    FileName = $"img_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"
                });
                BulkStatusText.Text = $"Pasted item #{BulkImages.Count}";
                BulkStatusText.Foreground = Brushes.Green;
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

    private void BulkSaveButton_Click(object? sender, RoutedEventArgs e)
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
            int savedCount = 0;
            foreach (var item in BulkImages)
            {
                if (item.Image != null)
                {
                    SaveDesktopImage(item.Image);
                    savedCount++;
                }
            }

            BulkStatusText.Text = $"Successfully saved {savedCount} images!";
            BulkStatusText.Foreground = Brushes.Green;
            BulkImages.Clear();
        }
        catch (Exception ex)
        {
            BulkStatusText.Text = $"Error: {ex.Message}";
            BulkStatusText.Foreground = Brushes.Red;
        }

        BulkStatusText.IsVisible = true;
    }

    public class PastedImage
    {
        public Bitmap? Image { get; set; }
        public string? FileName { get; set; }
    }
}
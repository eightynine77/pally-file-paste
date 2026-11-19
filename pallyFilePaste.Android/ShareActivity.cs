using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;

namespace pallyFilePaste.Android;

[Activity(
    Label = "pally file paste",
    Icon = "@mipmap/icon",
    Theme = "@android:style/Theme.DeviceDefault.NoActionBar", // Fixes Issue 3: Bypasses custom splash screen
    Exported = true,
    ExcludeFromRecents = true,
    NoHistory = true, // Fixes Issue 1: Removed MainLauncher = true
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
[IntentFilter(
    new[] { Intent.ActionSend, Intent.ActionSendMultiple },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "image/*")]
public class ShareActivity : Activity // Fixes Issue 2: Uses native Android Activity instead of AvaloniaActivity
{
    private TextView? _titleText;
    private TextView? _subtitleText;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Build the minimalistic native Android UI
        var layout = new LinearLayout(this)
        {
            Orientation = global::Android.Widget.Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 
                ViewGroup.LayoutParams.MatchParent)
        };
        layout.SetBackgroundColor(
        global::Android.Graphics.Color.ParseColor("white"));
        layout.SetGravity(GravityFlags.Center);

        _titleText = new TextView(this)
        {
            Text = "pasting your image into file...",
            TextSize = 23f
        };
        _titleText.SetSingleLine(false);
        _titleText.SetHorizontallyScrolling(false);
        _titleText.SetTextColor(
        global::Android.Graphics.Color.ParseColor("black"));
        _titleText.SetTypeface(null, TypefaceStyle.Bold);
        _titleText.Gravity = GravityFlags.Center;

        _subtitleText = new TextView(this)
        {
            Text = "please wait.",
            TextSize = 18f,
        };
        _subtitleText.SetSingleLine(false);
        _subtitleText.SetHorizontallyScrolling(false);
        _subtitleText.SetTextColor(
        global::Android.Graphics.Color.ParseColor("black"));
        _subtitleText.Gravity = GravityFlags.Center;
        _subtitleText.SetPadding(0, 16, 0, 0);

        layout.AddView(_titleText);
        layout.AddView(_subtitleText);
        SetContentView(layout);

        _ = HandleSharedIntentAsync(Intent);
    }

    private async Task HandleSharedIntentAsync(Intent? intent)
    {
        var uris = ExtractImageUris(intent);

        if (uris.Count == 0)
        {
            await UpdateUiAndCloseAsync("failed to save image", "no image received.", 2000);
            return;
        }

        try
        {
            int savedCount = 0;

            await Task.Run(() =>
            {
                foreach (var uri in uris)
                {
                    using var inputStream = ContentResolver?.OpenInputStream(uri);

                    if (inputStream == null)
                        continue;

                    if (SaveNativeStream(inputStream))
                        savedCount++;
                }
            });

            if (savedCount == 0)
            {
                await UpdateUiAndCloseAsync("failed to save image", "could not read image stream.", 2000);
                return;
            }

            string subtitleMsg = savedCount == 1
                ? "saved image to Pictures/pallyFilePaste"
                : $"saved {savedCount} images to Pictures/pallyFilePaste";

            await UpdateUiAndCloseAsync("done!", subtitleMsg, 3000);
        }
        catch (Exception ex)
        {
            await UpdateUiAndCloseAsync("failed to save image", ex.Message, 3000);
        }
    }


    private bool SaveNativeStream(System.IO.Stream inputStream)
    {
        // Decode the original image into a bitmap.
        using var bitmap = BitmapFactory.DecodeStream(inputStream);

        if (bitmap == null)
            return false;

        // Generate a unique filename.
        var fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.png";

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            var resolver = ContentResolver;

            if (resolver == null)
                return false;

            using var values = new ContentValues();

            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "image/png");
            values.Put(
                MediaStore.IMediaColumns.RelativePath,
                global::Android.OS.Environment.DirectoryPictures + "/pallyFilePaste/");

            values.Put(MediaStore.IMediaColumns.IsPending, 1);

            var collection = MediaStore.Images.Media.GetContentUri(
                MediaStore.VolumeExternalPrimary);

            var savedUri = resolver.Insert(collection, values);

            if (savedUri == null)
                return false;

            try
            {
                using (var outputStream = resolver.OpenOutputStream(savedUri))
                {
                    if (outputStream == null)
                        throw new IOException("Could not open image output stream.");

                    // Actually encode the bitmap as PNG.
                    if (!bitmap.Compress(Bitmap.CompressFormat.Png, 100, outputStream))
                        throw new IOException("Could not convert image to PNG.");
                }

                // Make the saved image visible to other applications.
                using var updateValues = new ContentValues();

                updateValues.Put(MediaStore.IMediaColumns.IsPending, 0);

                if (resolver.Update(savedUri, updateValues, null, null) <= 0)
                    throw new IOException("Could not publish the saved image.");

                return true;
            }
            catch
            {
                // Remove an incomplete image if saving failed.
                resolver.Delete(savedUri, null, null);
                throw;
            }
        }
        else
        {
            // Android 9 and older.
            var picturesDirectory =
                global::Android.OS.Environment.GetExternalStoragePublicDirectory(
                    global::Android.OS.Environment.DirectoryPictures);

            var appDirectory = new Java.IO.File(
                picturesDirectory,
                "pallyFilePaste");

            if (!appDirectory.Exists() && !appDirectory.Mkdirs())
                throw new IOException("Could not create image directory.");

            var file = new Java.IO.File(appDirectory, fileName);

            try
            {
                using (var outputStream = System.IO.File.Create(file.AbsolutePath))
                {
                    // Actually encode the bitmap as PNG.
                    if (!bitmap.Compress(Bitmap.CompressFormat.Png, 100, outputStream))
                        throw new IOException("Could not convert image to PNG.");
                }

                MediaScannerConnection.ScanFile(
                    this,
                    new[] { file.AbsolutePath },
                    new[] { "image/png" },
                    null);

                return true;
            }
            catch
            {
                if (file.Exists())
                    file.Delete();

                throw;
            }
        }
    }

    private async Task UpdateUiAndCloseAsync(string title, string subtitle, int delayMs)
    {
        RunOnUiThread(() =>
        {
            if (_titleText != null) _titleText.Text = title;
            if (_subtitleText != null) _subtitleText.Text = subtitle;
        });

        await Task.Delay(delayMs);

        RunOnUiThread(() =>
        {
            if (IsTaskRoot) FinishAndRemoveTask();
            else Finish();
        });
    }

    private static List<global::Android.Net.Uri> ExtractImageUris(Intent? intent)
    {
        var result = new List<global::Android.Net.Uri>();
        if (intent == null) return result;

        if (intent.Action == Intent.ActionSend)
        {
            var uri = GetParcelableUri(intent, Intent.ExtraStream);
            if (uri != null) result.Add(uri);
        }
        else if (intent.Action == Intent.ActionSendMultiple)
        {
            var uris = GetParcelableUriList(intent, Intent.ExtraStream);
            if (uris != null) result.AddRange(uris);
        }

        if (result.Count == 0 && intent.ClipData != null)
        {
            for (int i = 0; i < intent.ClipData.ItemCount; i++)
            {
                var itemUri = intent.ClipData.GetItemAt(i)?.Uri;
                if (itemUri != null) result.Add(itemUri);
            }
        }

        return result;
    }

    private static global::Android.Net.Uri? GetParcelableUri(Intent intent, string key)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return intent.GetParcelableExtra(key, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri))) as global::Android.Net.Uri;

#pragma warning disable CA1422
        return intent.GetParcelableExtra(key) as global::Android.Net.Uri;
#pragma warning restore CA1422
    }

    private static IList<global::Android.Net.Uri>? GetParcelableUriList(Intent intent, string key)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var list = intent.GetParcelableArrayListExtra(key, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri)));
            if (list == null) return null;
            var uris = new List<global::Android.Net.Uri>(list.Count);
            foreach (var item in list) if (item is global::Android.Net.Uri uri) uris.Add(uri);
            return uris;
        }

#pragma warning disable CA1422
        var legacyList = intent.GetParcelableArrayListExtra(key);
#pragma warning restore CA1422
        if (legacyList == null) return null;
        var legacyUris = new List<global::Android.Net.Uri>(legacyList.Count);
        foreach (var item in legacyList) if (item is global::Android.Net.Uri uri) legacyUris.Add(uri);
        return legacyUris;
    }
}
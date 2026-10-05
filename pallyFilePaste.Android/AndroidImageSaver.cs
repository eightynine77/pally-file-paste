using System.Runtime.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Android.Provider;
using Avalonia.Media.Imaging;
using pallyFilePaste;

namespace pallyFilePaste.Android;

public class AndroidImageSaver : IAndroidImageSaver
{
    public async Task SaveImageAsync(Bitmap bitmap)
    {
        var context = global::Android.App.Application.Context;

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            await SaveImageModernAsync(context, bitmap);
        }
        else
        {
            await SaveImageLegacyAsync(context, bitmap);
        }
    }

    [SupportedOSPlatform("android29.0")]
    private static async Task SaveImageModernAsync(
        global::Android.Content.Context context,
        Bitmap bitmap)
    {
        var resolver = context.ContentResolver;

        if (resolver == null)
            throw new InvalidOperationException("ContentResolver is null.");

        var fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        var picturesDir =
            global::Android.OS.Environment.DirectoryPictures;

        var relativePath = $"{picturesDir}/pallyFilePaste/";

        using var values = new ContentValues();

        values.Put(
            MediaStore.IMediaColumns.DisplayName,
            fileName);

        values.Put(
            MediaStore.IMediaColumns.MimeType,
            "image/png");

        values.Put(
            MediaStore.IMediaColumns.RelativePath,
            relativePath);

        values.Put(
            MediaStore.IMediaColumns.IsPending,
            1);

        var collection =
            MediaStore.Images.Media.GetContentUri(
                MediaStore.VolumeExternalPrimary);

        if (collection == null)
            throw new InvalidOperationException("Collection URI is null.");

        var uri = resolver.Insert(collection, values);

        if (uri == null)
            throw new IOException("MediaStore insert failed.");

        using (var stream = resolver.OpenOutputStream(uri))
        {
            if (stream == null)
                throw new IOException("Could not open output stream.");

            bitmap.Save(
                stream,
                PngBitmapEncoderOptions.Default);
        }

        using var updateValues = new ContentValues();

        updateValues.Put(
            MediaStore.IMediaColumns.IsPending,
            0);

        resolver.Update(
            uri,
            updateValues,
            null,
            null);

        await Task.CompletedTask;
    }

    private static async Task SaveImageLegacyAsync(
        global::Android.Content.Context context,
        Bitmap bitmap)
    {
        var picturesDirectory =
        global::Android.OS.Environment.GetExternalStoragePublicDirectory(
            global::Android.OS.Environment.DirectoryPictures);

        if (picturesDirectory == null)
            throw new IOException("Pictures directory is unavailable.");

        var appDirectory =
            new Java.IO.File(
                picturesDirectory,
                "pallyFilePaste");

        if (!appDirectory.Exists() &&
            !appDirectory.Mkdirs())
        {
            throw new IOException("Could not create Pictures/pallyFilePaste.");
        }

        var fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        var file =
            new Java.IO.File(
                appDirectory,
                fileName);

        using (var stream = File.Create(file.AbsolutePath))
        {
            bitmap.Save(
                stream,
                PngBitmapEncoderOptions.Default);
        }

        MediaScannerConnection.ScanFile(
            context,
            new[] { file.AbsolutePath },
            new[] { "image/png" },
            null);

        await Task.CompletedTask;
    }

    public async Task SaveImagesAsync(IEnumerable<Bitmap> bitmaps)
    {
        foreach (var bitmap in bitmaps)
        {
            await SaveImageAsync(bitmap);
        }
    }
}
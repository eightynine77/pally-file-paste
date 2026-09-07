using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using System.Collections.Generic;
using Android.OS;
using Android.Provider;
using Avalonia.Media.Imaging;
using pallyFilePaste;

namespace pallyFilePaste.Android;

public class AndroidImageSaver : IAndroidImageSaver
{
    public async Task SaveImageAsync(Bitmap bitmap)
    {
        var context = global::Android.App.Application.Context;
        var resolver = context.ContentResolver;

        if (resolver == null)
            throw new InvalidOperationException("ContentResolver is null.");

        var fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        
        // Extracting path into a variable avoids string interpolation formatting syntax traps
        var picturesDir = global::Android.OS.Environment.DirectoryPictures;
        var relativePath = $"{picturesDir}/pallyFilePaste/";

        using var values = new ContentValues();
        values.Put(MediaStore.MediaColumns.DisplayName, fileName);
        values.Put(MediaStore.MediaColumns.MimeType, "image/png");
        values.Put(MediaStore.MediaColumns.RelativePath, relativePath);
        values.Put(MediaStore.MediaColumns.IsPending, 1);

        var collection = MediaStore.Images.Media.GetContentUri(MediaStore.VolumeExternalPrimary);

        if (collection == null)
            throw new InvalidOperationException("Collection URI is null.");

        var uri = resolver.Insert(collection, values);

        if (uri == null)
            throw new IOException("MediaStore insert failed.");

        using (var stream = resolver.OpenOutputStream(uri))
        {
            if (stream == null)
                throw new IOException("Could not open output stream.");

            bitmap.Save(stream);
        }

        using var updateValues = new ContentValues();
        updateValues.Put(MediaStore.MediaColumns.IsPending, 0);
        resolver.Update(uri, updateValues, null, null);
    }

    public async Task SaveImagesAsync(IEnumerable<Bitmap> bitmaps)
    {
        foreach (var bitmap in bitmaps)
        {
            await SaveImageAsync(bitmap);
        }
    }
}
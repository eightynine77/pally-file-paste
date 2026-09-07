using System;
using System.IO;
using System.Threading.Tasks;

using Android.Graphics;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

using Avalonia.Android;
using Avalonia.Platform;

using pallyFilePaste;

namespace pallyFilePaste.Android;

public static class AndroidBulkPasteTextBoxHandler
{
    public static void Register()
    {
        AndroidBulkPasteTextBox.NativeControlFactory =
            CreateNativeControl;
    }

    private static IPlatformHandle CreateNativeControl(
        AndroidBulkPasteTextBox host,
        IPlatformHandle parent)
    {
        var parentHandle =
            parent as AndroidViewControlHandle;

        var context =
            parentHandle?.View.Context
            ?? global::Android.App.Application.Context;

        var editText =
            new RichEditText(context, host);

        editText.Focusable = true;
        editText.FocusableInTouchMode = true;
        editText.Clickable = true;
        editText.Enabled = true;

        editText.SetSingleLine(false);
        editText.SetMinLines(8);
        editText.Gravity = GravityFlags.Top;
        editText.SetPadding(20, 20, 20, 20);
        editText.Hint =
            "Tap here to focus & paste from clipboard...";

        editText.TextChanged += (_, e) =>
        {
            var text = e.Text?.ToString();

            if (string.IsNullOrEmpty(text))
                return;

            editText.Text = string.Empty;

            host.RaiseTextPasted();
        };

        return new AndroidViewControlHandle(editText);
    }

    private sealed class RichEditText : EditText
    {
        public override bool OnTouchEvent(MotionEvent? e)
        {
            var handled = base.OnTouchEvent(e);

            if (e?.Action == MotionEventActions.Up)
            {
                Post(() =>
                {
                    RequestFocus();

                    if (!IsFocused)
                    {
                        return;
                    }

                    var imm =
                        (InputMethodManager?)
                            Context?.GetSystemService(
                                global::Android.Content.Context.InputMethodService);

                    if (imm == null)
                    {
                        return;
                    }
                });
            }

            return handled;
        }

        private readonly AndroidBulkPasteTextBox _host;

        public RichEditText(global::Android.Content.Context context, AndroidBulkPasteTextBox host) : base(context)
        {
            _host = host;
        }

            public override IInputConnection? OnCreateInputConnection(
                EditorInfo? outAttrs)
            {

                var inputConnection =
                    base.OnCreateInputConnection(outAttrs);

            if (inputConnection == null ||
                outAttrs == null)
            {
                return inputConnection;
            }

            // Tell Gboard that this editor accepts images.
            outAttrs.ContentMimeTypes = new[]
            {
                "image/*"
            };

            return new RichInputConnection(
                inputConnection,
                _host);
        }
    }

    private sealed class RichInputConnection
        : InputConnectionWrapper
    {
        private readonly AndroidBulkPasteTextBox _host;

        public RichInputConnection(
            IInputConnection target,
            AndroidBulkPasteTextBox host)
            : base(target, true)
        {
            _host = host;
        }

        public override bool CommitContent(
            InputContentInfo? inputContentInfo,
            InputContentFlags flags,
            global::Android.OS.Bundle? opts)
        {
            if (inputContentInfo == null)
            {
                return false;
            }

            _ = ReceiveImageAsync(
                inputContentInfo,
                flags);

            return true;
        }

        private async Task ReceiveImageAsync(
            InputContentInfo inputContentInfo,
            InputContentFlags flags)
        {
            try
            {
                if ((flags &
                    InputContentFlags.GrantReadUriPermission) != 0)
                {
                    inputContentInfo.RequestPermission();
                }

                var uri = inputContentInfo.ContentUri;

                if (uri == null)
                {
                    return;
                }

                var resolver =
                    global::Android.App.Application.Context
                        .ContentResolver;

                using var inputStream =
                    resolver.OpenInputStream(uri);

                if (inputStream == null)
                {
                    return;
                }

                using var androidBitmap =
                    global::Android.Graphics.BitmapFactory
                        .DecodeStream(inputStream);

                if (androidBitmap == null)
                {
                    return;
                }

                using var memoryStream =
                    new MemoryStream();

                var compressed =
                    androidBitmap.Compress(
                        global::Android.Graphics.Bitmap.CompressFormat.Png,
                        100,
                        memoryStream);

                memoryStream.Position = 0;

                var bitmap =
                    new Avalonia.Media.Imaging.Bitmap(
                        memoryStream);

                _host.RaiseImagePasted(bitmap);
            }
            catch
            {
                return;
            }
            finally
            {
                if ((flags &
                    InputContentFlags.GrantReadUriPermission) != 0)
                {
                    try
                    {
                        inputContentInfo.ReleasePermission();
                    }
                    catch
                    {
                        
                    }
                }
            }
            await Task.CompletedTask;
        }
    }
}
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace pallyFilePaste
{
    public interface IAndroidImageSaver
    {
        Task SaveImageAsync(Bitmap bitmap);
        Task SaveImagesAsync(IEnumerable<Bitmap> bitmaps);
    }
}
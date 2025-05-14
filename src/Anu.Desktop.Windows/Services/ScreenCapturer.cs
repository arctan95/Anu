using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Anu.Core.Services;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using SysBitmap = System.Drawing.Bitmap;

namespace Anu.Desktop.Windows.Services;

public class ScreenCapturer : IScreenCapturer
{
    public Task<Bitmap?> CaptureScreen(int width, int height)
    {
        var tcs = new TaskCompletionSource<Bitmap?>();
        using var bmp = new SysBitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
        }

        tcs.SetResult(ToAvaloniaBitmap(bmp));
        return tcs.Task;
    }

    private static Bitmap? ToAvaloniaBitmap(SysBitmap bitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return new Bitmap(memoryStream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to convert System.Drawing.Bitmap to Avalonia.Bitmap: {ex.Message}");
            return null;
        }
    }
}
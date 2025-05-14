using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Anu.Core.Services;
using ScreenCaptureKit;

namespace Anu.Desktop.MacOS.Services;

public class ScreenCapturer : IScreenCapturer
{
    [SupportedOSPlatform("macos14.4")]
    public Task<Bitmap?> CaptureScreen(int width, int height)
    {
        var tcs = new TaskCompletionSource<Bitmap?>();
        if (OperatingSystem.IsMacOSVersionAtLeast(14, 4))
        {
            SCShareableContent.GetCurrentProcessShareableContent((content, error) =>
            {
                if (error != null)
                {
                    Console.WriteLine("Error fetching shareable content:", error);
                    return;
                }

                SCDisplay display = content.Displays.First();

                if (display == null)
                {
                    Console.WriteLine("No display found.");
                    return;
                }

                SCContentFilter filter = new SCContentFilter(display, [], SCContentFilterOption.Exclude);
                SCStreamConfiguration config = new SCStreamConfiguration();
                config.CapturesAudio = false;
                config.ExcludesCurrentProcessAudio = true;
                config.PreservesAspectRatio = true;
                config.ShowsCursor = false;
                config.CaptureResolution = SCCaptureResolutionType.Best;
                config.Width = (UIntPtr)width;
                config.Height = (UIntPtr)height;


                SCScreenshotManager.CaptureImage(filter, config, (image, error) =>
                {
                    if (error != null)
                    {
                        Console.WriteLine("Error capturing image: %@", error);
                    }

                    if (image == null)
                    {
                        Console.WriteLine("No image captured.");
                    }

                    tcs.SetResult(CGImageToBitmap(image));
                });
            });
        }
        else
        {
            CGImage? image = CGImage.ScreenImage(0, new CGRect(0, 0, width, height), CGWindowListOption.All, CGWindowImageOption.BestResolution);
            tcs.SetResult(CGImageToBitmap(image));
        }

        return tcs.Task;
    }


    private static Bitmap? CGImageToBitmap(CGImage? cgImage)
    {
        try
        {
            if (cgImage == null)
            {
                Console.WriteLine("No image captured.");
                return null;
            }

            int width = (int)cgImage.Width;
            int height = (int)cgImage.Height;
            int stride = (int)cgImage.BytesPerRow;

            var dataProvider = cgImage.DataProvider;
            var dataRef = dataProvider.CopyData();

            if (dataRef == null)
            {
                throw new Exception("Failed to copy image data.");
            }

            IntPtr dataPtr = dataRef.Bytes;
            Vector dpi = new Vector(96, 96);

            Bitmap bitmap = new Bitmap(
                PixelFormat.Bgra8888, // Pixel format
                AlphaFormat.Premul,   // Alpha format
                dataPtr,              // Raw pixel data
                new PixelSize(width, height), // Image size
                dpi,                  // DPI
                stride
            );

            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to convert CGImage to Avalonia.Bitmap: {ex.Message}");
            return null;
        }
    }


}
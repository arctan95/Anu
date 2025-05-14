using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Anu.Core.Services;

public interface IScreenCapturer
{
    public Task<Bitmap?> CaptureScreen(int width, int height);
}
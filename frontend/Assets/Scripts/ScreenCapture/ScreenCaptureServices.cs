using System.Threading;
using Cysharp.Threading.Tasks;

namespace DA.ScreenCapture
{
    public interface IScreenCaptureService
    {
        MonitorDescriptor[] GetMonitors();
        UniTask<CapturedFrame> CaptureAsync(int monitorNumber, CancellationToken cancellationToken);
    }

    public interface IImageDifferenceService
    {
        double Calculate(CapturedFrame previous, CapturedFrame current);
    }
}

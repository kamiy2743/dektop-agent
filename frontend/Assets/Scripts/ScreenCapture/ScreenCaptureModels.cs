using System;

namespace DA.ScreenCapture
{
    public readonly struct MonitorDescriptor
    {
        public int Number { get; }
        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public bool Primary { get; }
        public MonitorDescriptor(int number, string name, int width, int height, bool primary) =>
            (Number, Name, Width, Height, Primary) = (number, name, width, height, primary);
        public override string ToString() => $"{Number}: {Name} ({Width}x{Height}){(Primary ? " [Primary]" : string.Empty)}";
    }

    public sealed class CapturedFrame
    {
        public byte[] Bgra32 { get; }
        public int Width { get; }
        public int Height { get; }
        public DateTime CapturedAt { get; }
        public int MonitorNumber { get; }
        public string ActiveWindowName { get; }
        public string Id { get; }
        public CapturedFrame(byte[] bgra32, int width, int height, DateTime capturedAt, int monitorNumber, string id, string activeWindowName = "") =>
            (Bgra32, Width, Height, CapturedAt, MonitorNumber, Id, ActiveWindowName) = (bgra32, width, height, capturedAt, monitorNumber, id, activeWindowName);
    }
}

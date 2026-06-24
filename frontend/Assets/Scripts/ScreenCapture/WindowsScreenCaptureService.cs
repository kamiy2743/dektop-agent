using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DA.ScreenCapture
{
    public sealed class WindowsScreenCaptureService : IScreenCaptureService
    {
        public MonitorDescriptor[] GetMonitors()
        {
            var result = new List<MonitorDescriptor>();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr _, ref Rect _, IntPtr _) =>
            {
                var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
                if (GetMonitorInfo(monitor, ref info))
                {
                    result.Add(new MonitorDescriptor(result.Count + 1, info.Device, info.Monitor.Right - info.Monitor.Left, info.Monitor.Bottom - info.Monitor.Top, (info.Flags & 1) != 0));
                }

                return true;
            }, IntPtr.Zero);
#endif
            return result.ToArray();
        }

        public async UniTask<CapturedFrame> CaptureAsync(int monitorNumber, CancellationToken cancellationToken)
        {
            return await UniTask.RunOnThreadPool(() => Capture(monitorNumber, cancellationToken), cancellationToken: cancellationToken);
        }

        static CapturedFrame Capture(int monitorNumber, CancellationToken cancellationToken)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var handles = new List<IntPtr>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr _, ref Rect _, IntPtr _) => { handles.Add(monitor); return true; }, IntPtr.Zero);
            if (monitorNumber < 1 || monitorNumber > handles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(monitorNumber), $"Monitor must be between 1 and {handles.Count}.");
            }

            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
            if (!GetMonitorInfo(handles[monitorNumber - 1], ref info))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var width = info.Monitor.Right - info.Monitor.Left;
            var height = info.Monitor.Bottom - info.Monitor.Top;
            var screenDc = GetDC(IntPtr.Zero);
            var memoryDc = CreateCompatibleDC(screenDc);
            var bitmap = CreateCompatibleBitmap(screenDc, width, height);
            var oldBitmap = SelectObject(memoryDc, bitmap);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!BitBlt(memoryDc, 0, 0, width, height, screenDc, info.Monitor.Left, info.Monitor.Top, 0x40CC0020))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var pixels = new byte[checked(width * height * 4)];
                // GDIのbottom-up配列をそのまま使う。Texture2Dのraw dataも左下原点なので、
                // top-down（負のHeight）で取得すると表示・Ollama送信画像が上下反転する。
                var bitmapInfo = new BitmapInfo { Header = new BitmapInfoHeader { Size = 40, Width = width, Height = height, Planes = 1, BitCount = 32 } };
                if (GetDIBits(memoryDc, bitmap, 0, (uint)height, pixels, ref bitmapInfo, 0) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return new CapturedFrame(pixels, width, height, DateTime.Now, monitorNumber, Guid.NewGuid().ToString("N"), GetActiveWindowTitle());
            }
            finally
            {
                SelectObject(memoryDc, oldBitmap); DeleteObject(bitmap); DeleteDC(memoryDc); ReleaseDC(IntPtr.Zero, screenDc);
            }
#else
            throw new PlatformNotSupportedException("Screen capture is supported only on Windows.");
#endif
        }

        delegate bool MonitorEnumProcedure(IntPtr monitor, IntPtr dc, ref Rect rect, IntPtr data);
        [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] struct MonitorInfoEx { public int Size; public Rect Monitor, Work; public uint Flags; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Device; }
        [StructLayout(LayoutKind.Sequential)] struct BitmapInfoHeader { public uint Size; public int Width, Height; public ushort Planes, BitCount; public uint Compression, ImageSize; public int XPelsPerMeter, YPelsPerMeter; public uint ColorsUsed, ColorsImportant; }
        [StructLayout(LayoutKind.Sequential)] struct BitmapInfo { public BitmapInfoHeader Header; public uint Colors; }
        static string GetActiveWindowTitle()
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(256);
            return GetWindowText(handle, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
        }

        [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorEnumProcedure callback, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int maxCount);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll", SetLastError = true)] static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);
        [DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] bits, ref BitmapInfo info, uint usage);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    }
}

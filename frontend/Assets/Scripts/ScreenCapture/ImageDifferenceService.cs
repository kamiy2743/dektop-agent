using System;

namespace DA.ScreenCapture
{
    public sealed class ImageDifferenceService : IImageDifferenceService
    {
        const int SampleWidth = 160;
        const int SampleHeight = 90;

        public double Calculate(CapturedFrame previous, CapturedFrame current)
        {
            if (previous.Width != current.Width || previous.Height != current.Height)
            {
                return 100d;
            }

            long difference = 0;
            for (var y = 0; y < SampleHeight; y++)
            for (var x = 0; x < SampleWidth; x++)
            {
                difference += Math.Abs(Gray(previous, x, y) - Gray(current, x, y));
            }

            return (double)difference / (SampleWidth * SampleHeight);
        }

        static int Gray(CapturedFrame frame, int sampleX, int sampleY)
        {
            var x = sampleX * frame.Width / SampleWidth;
            var y = sampleY * frame.Height / SampleHeight;
            var index = (y * frame.Width + x) * 4;
            return (frame.Bgra32[index + 2] * 299 + frame.Bgra32[index + 1] * 587 + frame.Bgra32[index] * 114) / 1000;
        }
    }
}

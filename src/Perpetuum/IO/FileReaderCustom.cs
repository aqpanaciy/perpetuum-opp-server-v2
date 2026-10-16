using Perpetuum.Log;
using Perpetuum.Zones.Terrains;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Perpetuum.IO
{
    public static class FileReaderCustom
    {
        public static string? ResourcePath { get; set; }

        public static ushort[] ReadJpegLayer(string path)
        {
            var prefix = Path.GetFileName(path) + ".";
            string imagePath = null;
            ushort maximum = 0;
            foreach (var candidate in Directory.EnumerateFiles(Path.GetDirectoryName(Path.GetFullPath(path))))
            {
                var name = Path.GetFileName(candidate);
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var numberLength = name.Length - prefix.Length - ".jpeg".Length;
                if (numberLength <= 0 || !ushort.TryParse(name.AsSpan(prefix.Length, numberLength),
                        NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                {
                    continue;
                }

                if (imagePath != null)
                {
                    throw new IOException($"Multiple JPEG layers found for '{path}'.");
                }

                imagePath = candidate;
                maximum = value;
            }

            if (imagePath == null)
            {
                throw new FileNotFoundException($"Layer and fallback '{path}.<0-65535>.jpeg' not found.", path);
            }

            using var image = new Bitmap(imagePath);
            var bounds = new Rectangle(0, 0, image.Width, image.Height);
            using var bitmap = image.Clone(bounds, PixelFormat.Format24bppRgb);
            var result = new ushort[bitmap.Width * bitmap.Height];
            var grayscale = new byte[result.Length];
            var pixels = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                var row = new byte[bitmap.Width * 3];
                for (var y = 0; y < bitmap.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(pixels.Scan0, y * pixels.Stride), row, 0, row.Length);
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        var offset = x * 3;
                        // Keep the source pixels separate so smoothing is independent of traversal order.
                        var gray = (299 * row[offset + 2] + 587 * row[offset + 1] + 114 * row[offset] + 500) / 1000;
                        grayscale[y * bitmap.Width + x] = (byte)gray;
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(pixels);
            }

            for (var y = 0; y < bitmap.Height; y++)
            {
                var minY = Math.Max(0, y - 2);
                var maxY = Math.Min(bitmap.Height - 1, y + 2);
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var minX = Math.Max(0, x - 2);
                    var maxX = Math.Min(bitmap.Width - 1, x + 2);
                    var sum = 0;
                    for (var neighborY = minY; neighborY <= maxY; neighborY++)
                    {
                        var rowOffset = neighborY * bitmap.Width;
                        for (var neighborX = minX; neighborX <= maxX; neighborX++)
                        {
                            sum += grayscale[rowOffset + neighborX];
                        }
                    }

                    // Average the available 5x5 neighbors and scale before rounding to ushort.
                    var divisor = 255 * (maxX - minX + 1) * (maxY - minY + 1);
                    result[y * bitmap.Width + x] = (ushort)((sum * maximum + divisor / 2) / divisor);
                }
            }

            if (!string.IsNullOrEmpty(ResourcePath))
            {
                string resourcePath = Path.Combine(ResourcePath, Path.GetFileName(path));
                ReadOnlySpan<byte> layerData = MemoryMarshal.AsBytes(result.AsSpan());
                using (var fileStream = new FileStream(resourcePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fileStream.Write(layerData);
                }

                Logger.Info($"[FileReaderCustom] Layer saved. ({resourcePath})");
            }

            return result;
        }
    }
}

// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace labs_coordinate_pictures
{
    public static class ClassImageOps
    {
        public static Bitmap ResizeImage(Bitmap bitmapFull,
            int newWidth, int newHeight, bool resizeToFit, string pathForLogging)
        {
            // Kris Erickson, stackoverflow 87753.
            // also considered pixelformat Imaging.PixelFormat.Format32bppPArgb
            // GDI seems to have a lock, so we don't get great concurrency.
            Bitmap bitmapResized = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bitmapResized))
            {
                if (!resizeToFit)
                {
                    // center onto image
                    g.DrawImageUnscaled(bitmapFull,
                        (newWidth - bitmapFull.Width) / 2,
                        (newHeight - bitmapFull.Height) / 2);
                }
                else if (newWidth != bitmapFull.Width || newHeight != bitmapFull.Height)
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(bitmapFull, new Rectangle(0, 0, newWidth, newHeight));
                }
                else
                {
                    g.DrawImageUnscaled(bitmapFull, 0, 0);
                }
            }

            if (bitmapResized == null)
            {
                throw new CoordinatePicturesException(
                    "do not expect newImage to be null. " + pathForLogging);
            }
            else
            {
                return bitmapResized;
            }
        }

        public static Bitmap ResizeImageByFactor(Bitmap bitmap, int mult, int movevertically)
        {
            if (mult <= 1 || (bitmap.Width == 1 && bitmap.Height == 1) ||
                (bitmap.Width > 2000 || bitmap.Height > 2000))
            {
                /* let's disallow resizing large images, to keep good responsiveness */
                return bitmap;
            }

            Bitmap bitmapResized = new Bitmap(bitmap.Width * mult, bitmap.Height * mult, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bitmapResized))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.None;
                g.DrawImage(bitmap, new Rectangle(0, movevertically, bitmapResized.Width, bitmapResized.Height));
            }

            if (bitmapResized == null)
            {
                Utils.MessageErr("resized image is null");
                return bitmap;
            }

            bitmap.Dispose();
            return bitmapResized;
        }

        public static Bitmap TileImage(Bitmap bitmap, bool tileImages, int maxWidth, int maxHeight)
        {
            if (!tileImages || (bitmap.Width >= maxWidth && bitmap.Height >= maxHeight))
            {
                return bitmap;
            }

            Bitmap bitmapTiled = new Bitmap(maxWidth, maxHeight, PixelFormat.Format32bppPArgb);
            int countTilesW = Math.Max(2, maxWidth / bitmap.Width) + 1;
            int countTilesH = Math.Max(2, maxHeight / bitmap.Height) + 1;
            using (Graphics g = Graphics.FromImage(bitmapTiled))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.None;
                for (int y = 0; y < countTilesH; y++)
                {
                    for (int x = 0; x < countTilesW; x++)
                    {
                        g.DrawImageUnscaled(bitmap, x * bitmap.Width, y * bitmap.Height);
                    }
                }
            }

            bitmap.Dispose();
            return bitmapTiled;
        }
    }

    // show a full-resolution excerpt of a large image
    public sealed class ImageViewExcerpt : IDisposable
    {
        public ImageViewExcerpt(int maxWidth, int maxHeight)
        {
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            Bmp = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);
        }

        public int MaxWidth { get; private set; }
        public int MaxHeight { get; private set; }
        public Bitmap Bmp { get; private set; }

        public void MakeBmp(string path, int clickX, int clickY,
            int widthOfResizedImage, int heightOfResizedImage, JpegRotationFinder shouldRotateImages)
        {
            Bmp.Dispose();
            Bmp = new Bitmap(MaxWidth, MaxHeight, PixelFormat.Format32bppPArgb);
            if (path == null || !FilenameUtils.LooksLikeImage(path) || !File.Exists(path))
            {
                return;
            }

            // we can disregard bitmapWillLockFile because we'll quickly dispose bitmapFull.
            using (Bitmap bitmapFull = ImageCache.GetBitmap(path, shouldRotateImages, out bool bitmapWillLockFile))
            {
                if (bitmapFull.Width == 1 || bitmapFull.Height == 1)
                {
                    return;
                }

                GetShiftAmount(bitmapFull, clickX, clickY,
                    widthOfResizedImage, heightOfResizedImage, out int shiftX, out int shiftY);

                // draw the entire image, but pushed off to the side
                using (Graphics g = Graphics.FromImage(Bmp))
                {
                    g.FillRectangle(Brushes.White, 0, 0, MaxWidth, MaxHeight);
                    g.DrawImageUnscaled(bitmapFull, -shiftX, -shiftY);
                }
            }
        }

        public void GetShiftAmount(Bitmap fullImage, int clickX, int clickY,
            int widthOfResizedImage, int heightOfResizedImage, out int shiftX, out int shiftY)
        {
            // find where the user clicked, then show that place in the center at full resolution.
            var centerX = (int)(fullImage.Width * (clickX / ((double)widthOfResizedImage)));
            var centerY = (int)(fullImage.Height * (clickY / ((double)heightOfResizedImage)));
            shiftX = centerX - (MaxWidth / 2);
            shiftY = centerY - (MaxHeight / 2);
        }

        public void Dispose()
        {
            Bmp.Dispose();
        }
    }

    public static class FormatConversions
    {
        public static void RunImageConversion(string pathInput, string pathOutput,
            string resizeSpec, int jpgQuality)
        {
            if (File.Exists(pathOutput))
            {
                Utils.MessageBox("File already exists, " + pathOutput);
                return;
            }

            // send the working directory for the script so that it can find options.ini
            var workingDir = Path.Combine(Configs.Current.Directory,
                "ben_python_img");
            var script = Path.Combine(Configs.Current.Directory,
                "ben_python_img", "img_convert_resize.py");
            var args = new string[] { "convert_resize",
                pathInput, pathOutput, resizeSpec, jpgQuality.ToString() };
            var stderr = Utils.RunPythonScript(script, args,
                createWindow: false, warnIfStdErr: false, workingDir: workingDir);

            if (!string.IsNullOrEmpty(stderr) || !File.Exists(pathOutput))
            {
                Utils.MessageBox("RunImageConversion failed, " + Utils.FormatPythonError(stderr));
            }
        }

        public static string RunM4aConversion(string path, string qualitySpec)
        {
            var qualities = new string[] { "16", "24", "96", "128", "144",
                "160", "192", "224", "256", "288", "320", "640", "flac" };
            if (Array.IndexOf(qualities, qualitySpec) == -1)
            {
                throw new CoordinatePicturesException("Unsupported bitrate.");
            }
            else if (!path.EndsWith(".wav", StringComparison.Ordinal) &&
                !path.EndsWith(".flac", StringComparison.Ordinal))
            {
                throw new CoordinatePicturesException("Unsupported input format.");
            }
            else
            {
                var encoder = Configs.Current.Get(ConfigKey.FilepathM4aEncoder);
                if (!File.Exists(encoder))
                {
                    Utils.MessageErr("M4a encoder not found, use Options->Set m4a encoder.");
                    throw new CoordinatePicturesException("");
                }

                var pathOutput = Path.GetDirectoryName(path) + Utils.Sep +
                    Path.GetFileNameWithoutExtension(path) +
                    (qualitySpec == "flac" ? ".flac" : ".m4a");
                var script = Path.GetDirectoryName(encoder) + Utils.Sep +
                    "dropq" + qualitySpec + ".py";
                var args = new string[] { path };
                var stderr = Utils.RunPythonScript(
                    script, args, createWindow: false, warnIfStdErr: false);

                if (!File.Exists(pathOutput))
                {
                    Utils.MessageErr("RunM4aConversion failed, " + Utils.FormatPythonError(stderr));
                    return null;
                }
                else
                {
                    return pathOutput;
                }
            }
        }

        public static void PlayMedia(string path)
        {
            if (path == null)
                path = Path.Combine(Configs.Current.Directory, "silence.flac");

            var player = Configs.Current.Get(ConfigKey.FilepathAudioPlayer);
            if (string.IsNullOrEmpty(player) || !File.Exists(player))
            {
                Utils.MessageBox("Media player not found. Go to the main screen " +
                    "and to the option menu and click Options->Set media player location...");
                return;
            }

            var args = player.ToLower().Contains("foobar") ? new string[] { "/playnow", path } :
                new string[] { path };

            Utils.Run(player, args, hideWindow: true, waitForExit: false, shellExecute: false);
        }
    }

    /**
     * Important to dispose, and other tips:
     * http://www.nathanaeljones.com/blog/2009/20-image-resizing-pitfalls
     *
     * Should I use WIC to load images?
     * - Would automatically detect rotated jpgs in a faster way
     * - Would support HEIC images and any new upcoming image formats
     * - Potentially faster than current methods. Might be able to multithread it,
     * whereas current resizing uses gdi and the gdi lock seems to prevents multithreading.
     *
     * Could run Install-Package stakx.WIC -Version 0.1.0 in Nuget console
     * (WIC.DotNet is slightly newer but needs to use .NET standard)
     * and
     * github.com/ReneSlijkhuis/example-wic-applications/blob/master/example_6/Program.cs
     * and
     * using stakx.WIC;
     * and
     * new System.Drawing.Bitmap(width, height, stride, format, scan)
     *
     * In the end, though there's not enough reason to add this.
     */
}

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
}

// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;

namespace labs_coordinate_pictures
{
    public sealed class ImageCache : IDisposable
    {
        // list of images, one entry per file.
        // all images are owned by and disposed from this cache.
        ListOfCachedImages _list = new ListOfCachedImages();

        // lock protecting _cache. doesn't need to be a RW lock,
        // as there won't usually be multiple readers.
        object _lock = new object();

        // rough limit on length of _cache.
        int _cacheSize;

        // when removing an entry from the cache, we can't remove the
        // entry that is currently shown by a form.
        Func<Action, bool> _callbackOnUiThread;
        Func<Bitmap, bool> _canDisposeBitmap;

        public ImageCache(
            int maxWidth,
            int maxHeight,
            int cacheSize,
            Func<Action, bool> callbackOnUiThread,
            Func<Bitmap, bool> canDisposeBitmap)
        {
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            _cacheSize = cacheSize;
            _callbackOnUiThread = callbackOnUiThread;
            _canDisposeBitmap = canDisposeBitmap;
            Excerpt = new ImageViewExcerpt(maxWidth, maxHeight);
        }

        public ImageViewExcerpt Excerpt { get; private set; }
        public int MaxHeight { get; private set; }
        public int MaxWidth { get; private set; }

        public void Dispose()
        {
            lock (_lock)
            {
                Excerpt.Dispose();
                _list.Dispose();
            }
        }

        // get image for this path. an image is created synchronously if not in the cache.
        public Bitmap Get(string path, out int originalWidth, out int originalHeight)
        {
            lock (_lock)
            {
                int index = _list.SearchForUpToDateCacheEntry(path);
                if (index == -1)
                {
                    SimpleLog.Current.WriteVerbose("adding to cache " + path);

                    Add(new string[] { path });
                    index = _list.SearchForUpToDateCacheEntry(path);
                    if (index == -1)
                    {
                        Utils.MessageErr("did not find image that was just cached. " +
                            " this can happen if an image is changed very quickly");
                        originalWidth = originalHeight = 0;
                        return null;
                    }
                }

                originalWidth = _list[index].Item3;
                originalHeight = _list[index].Item4;
                return _list[index].Item2;
            }
        }

        // add paths to cache, and then checks for images to remove.
        public void Add(string[] paths)
        {
            bool checkIfTooManyInCache = false;
            foreach (var path in paths)
            {
                if (path == null)
                {
                    continue;
                }

                lock (_lock)
                {
                    // skip if it's already in the cache
                    if (_list.SearchForUpToDateCacheEntry(path) != -1)
                    {
                        continue;
                    }

                    // reading and resizing the bitmap can be done outside the lock,
                    // but this might do redundant work.
                    int originalWidth = 0, originalHeight = 0;
                    var bitmap = GetResizedBitmap(path, out originalWidth, out originalHeight);
                    var lastModified = File.Exists(path) ?
                        new FileInfo(path).LastWriteTimeUtc :
                        DateTime.MinValue;

                    _list.Add(new Tuple<string, Bitmap, int, int, DateTime>(
                        path, bitmap, originalWidth, originalHeight, lastModified));

                    checkIfTooManyInCache = _list.Count > _cacheSize;
                }
            }

            if (checkIfTooManyInCache)
            {
                // ask form before we call Dispose() in case form is currently using this image.
                _callbackOnUiThread.Invoke(new Action(() =>
                {
                    lock (_lock)
                    {
                        // it's possible there is no work left to do, due to another thread.
                        // iterate backwards, since RemoveAt repositions subsequent elements
                        var howManyToRemove = _list.Count - _cacheSize;
                        for (int i = howManyToRemove - 1; i >= 0; i--)
                        {
                            if (i <= _list.Count - 1 && _canDisposeBitmap(_list[i].Item2))
                            {
                                _list.RemoveAt(i);
                            }
                        }
                    }
                }));
            }
        }

        public void AddAsync(List<string> listPaths)
        {
            ThreadPool.QueueUserWorkItem(
            delegate
            {
                Add(listPaths.ToArray());
            });
        }

        // bitmapWillLockFile indicates whether holding onto Bitmap will hold lock on a file.
        public static Bitmap GetBitmap(string path, out bool bitmapWillLockFile)
        {
            Bitmap bitmap = null;
            try
            {
                if (path.ToLowerInvariant().EndsWith(".webp", StringComparison.Ordinal))
                {
                    byte[] bytesData = File.ReadAllBytes(path);
                    var decoder = new Imazen.WebP.SimpleDecoder();
                    bitmap = decoder.DecodeFromBytes(bytesData, bytesData.LongLength);
                    bitmapWillLockFile = false;
                }
                else
                {
                    bitmap = new Bitmap(path);
                    bitmapWillLockFile = true;

                    // some image files have custom resolutions,
                    // I prefer seeing all files at the same resolution.
                    bitmap.SetResolution(96.0f, 96.0f);
                }
            }
            catch (Exception e)
            {
                if (path.ToLowerInvariant().EndsWith(".webp", StringComparison.Ordinal) &&
                    e.ToString().ToUpperInvariant().Contains("0x8007007E"))
                {
                    Utils.MessageErr("It appears that the Visual C++ Redistributable " +
                       "Packages for Visual Studio 2013 are not installed; please run " +
                       "vcredist_x64.exe so that libwebp.dll can be used.");
                }

                Utils.MessageErr("Could not load the image " + path +
                    Utils.NL + Utils.NL + Utils.NL + "Details: " +
                    e.ToString(), true);

                if (bitmap != null)
                {
                    bitmap.Dispose();
                }

                bitmapWillLockFile = true;
                bitmap = new Bitmap(1, 1);
            }

            return bitmap;
        }

        public Bitmap GetResizedBitmap(string path, out int originalWidth, out int originalHeight)
        {
            if (!FilenameUtils.LooksLikeImage(path) || !File.Exists(path))
            {
                originalWidth = 0;
                originalHeight = 0;
                return new Bitmap(1, 1);
            }

            bool bitmapWillLockFile;
            Bitmap bitmapFull = GetBitmap(path, out bitmapWillLockFile);

            // resize and preserve ratio
            originalWidth = bitmapFull.Width;
            originalHeight = bitmapFull.Height;
            if (bitmapFull.Width > MaxWidth || bitmapFull.Height > MaxHeight)
            {
                using (bitmapFull)
                {
                    var ratio = Math.Min((double)MaxWidth / bitmapFull.Width,
                        (double)MaxHeight / bitmapFull.Height);

                    int newWidth = (int)(bitmapFull.Width * ratio);
                    int newHeight = (int)(bitmapFull.Height * ratio);
                    return ResizeImage(bitmapFull, newWidth, newHeight, path);
                }
            }
            else if (bitmapWillLockFile)
            {
                // make a copy of the bitmap, otherwise the file remains locked
                using (bitmapFull)
                {
                    return new Bitmap(bitmapFull);
                }
            }
            else
            {
                return bitmapFull;
            }
        }

        public static Bitmap ResizeImage(Bitmap bitmapFull,
            int newWidth, int newHeight, string pathForLogging)
        {
            // Kris Erickson, stackoverflow 87753.
            // also considered pixelformat Imaging.PixelFormat.Format32bppPArgb
            // GDI seems to have a lock, so we don't get great concurrency.
            Bitmap bitmapResized = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(bitmapResized))
            {
                if (newWidth != bitmapFull.Width || newHeight != bitmapFull.Height)
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
    }

    // show a full-resolution excerpt of a large image
    public sealed class ImageViewExcerpt : IDisposable
    {
        public ImageViewExcerpt(int maxWidth, int maxHeight)
        {
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            Bmp = new Bitmap(1, 1);
        }

        public int MaxWidth { get; private set; }
        public int MaxHeight { get; private set; }
        public Bitmap Bmp { get; private set; }

        public void MakeBmp(string path, int clickX, int clickY,
            int widthOfResizedImage, int heightOfResizedImage)
        {
            Bmp.Dispose();
            Bmp = new Bitmap(MaxWidth, MaxHeight);
            if (path == null || !FilenameUtils.LooksLikeImage(path) || !File.Exists(path))
            {
                return;
            }

            // we can disregard bitmapWillLockFile because we'll quickly dispose bitmapFull.
            bool bitmapWillLockFile;
            using (Bitmap bitmapFull = ImageCache.GetBitmap(path, out bitmapWillLockFile))
            {
                if (bitmapFull.Width == 1 || bitmapFull.Height == 1)
                {
                    return;
                }

                int shiftX, shiftY;
                GetShiftAmount(bitmapFull, clickX, clickY,
                    widthOfResizedImage, heightOfResizedImage, out shiftX, out shiftY);

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

    // enforces that Dispose() when removing images from the cache.
    public sealed class ListOfCachedImages : IDisposable
    {
        // tuple of path/image/width/height/modified-time.
        List<Tuple<string, Bitmap, int, int, DateTime>> _cache =
            new List<Tuple<string, Bitmap, int, int, DateTime>>();

        public int Count
        {
            get { return _cache.Count; }
        }

        public Tuple<string, Bitmap, int, int, DateTime> this[int index]
        {
            get { return _cache[index]; }
        }

        // returns index into _cache if found and up to date, or otherwise -1.
        public int SearchForUpToDateCacheEntry(string path)
        {
            // linear search is fine as we only have a few entries.
            int indexFound = _cache.FindIndex((tuple) => tuple.Item1 == path);
            if (indexFound != -1)
            {
                // is it up to date though? if it is out of date, invalidate the cached entry.
                var isCurrent = File.Exists(path) ?
                    new FileInfo(path).LastWriteTimeUtc == _cache[indexFound].Item5 :
                    false;

                if (!isCurrent)
                {
                    RemoveAt(indexFound);
                    indexFound = -1;
                }
            }

            return indexFound;
        }

        public void Add(Tuple<string, Bitmap, int, int, DateTime> tuple)
        {
            _cache.Add(tuple);
        }

        public void RemoveAt(int index)
        {
            _cache[index].Item2.Dispose();
            _cache.RemoveAt(index);
        }

        public void Dispose()
        {
            foreach (var tuple in _cache)
            {
                if (tuple.Item2 != null)
                {
                    tuple.Item2.Dispose();
                }
            }

            _cache.Clear();
        }
    }
}

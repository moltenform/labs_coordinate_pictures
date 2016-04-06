// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public sealed class ImageCache : IDisposable
    {
        // cache of images, one entry per file.
        // all images are owned by and disposed from the cache.
        // tuple of path/image/width/height/modified-time.
        List<Tuple<string, Bitmap, int, int, DateTime>> _cache;

        // lock protecting _cache. doesn't need to be a RW lock,
        // as there won't usually be multiple readers.
        object _lock = new object();

        // not a hard constraint but rough limit on length of _cache.
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
            _cache = new List<Tuple<string, Bitmap, int, int, DateTime>>();
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
                foreach (var tuple in _cache)
                {
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.Dispose();
                    }
                }
            }
        }

        // returns index into _cache if found and up to date, or otherwise -1.
        int SearchForUpToDateCacheEntry(string path)
        {
            lock (_lock)
            {
                int index = -1;
                for (int i = 0; i < _cache.Count; i++)
                {
                    // linear search is fine as we only have a few entries.
                    if (_cache[i].Item1 == path)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    // is it up to date though? if it's been written to, invalidate cache.
                    var isCurrent = File.Exists(path) ?
                        new FileInfo(path).LastWriteTimeUtc == _cache[index].Item5 :
                        false;
                    if (!isCurrent)
                    {
                        _cache.RemoveAt(index);
                        index = -1;
                    }
                }

                return index;
            }
        }

        // get image for this path. an image is created synchronously if not in the cache.
        public Bitmap Get(string path, out int nOrigW, out int nOrigH)
        {
            lock (_lock)
            {
                int index = SearchForUpToDateCacheEntry(path);
                if (index == -1)
                {
                    SimpleLog.Current.WriteVerbose("adding to cache " + path);

                    Add(new string[] { path });
                    index = SearchForUpToDateCacheEntry(path);
                    if (index == -1)
                    {
                        MessageBox.Show("did not find image that was just cached. this can happen if an image is changed very quickly");
                        nOrigW = nOrigH = 0;
                        return null;
                    }
                }

                nOrigW = _cache[index].Item3;
                nOrigH = _cache[index].Item4;
                return _cache[index].Item2;
            }
        }

        // add paths to cache, and then checks for images to remove.
        public void Add(string[] paths)
        {
            bool checkTooBig = false;
            foreach (var path in paths)
            {
                if (path == null)
                    continue;

                lock (_lock)
                {
                    // skip if it's already in the cache
                    if (SearchForUpToDateCacheEntry(path) != -1)
                        continue;

                    // reading and resizing the bitmap can be done outside the lock,
                    // but this might do redundant work.
                    int nOrigW = 0, nOrigH = 0;
                    var b = GetResizedBitmap(path, out nOrigW, out nOrigH);
                    var lastModified = File.Exists(path) ?
                        new FileInfo(path).LastWriteTimeUtc :
                        DateTime.MaxValue;
                    _cache.Add(new Tuple<string, Bitmap, int, int, DateTime>(
                        path, b, nOrigW, nOrigH, lastModified));
                    checkTooBig = _cache.Count > _cacheSize;
                }
            }

            if (checkTooBig)
            {
                // ask our owner before we call Dispose() in case the owner is currently using this image.
                _callbackOnUiThread.Invoke(new Action(() =>
                {
                    lock (_lock)
                    {
                        // now that we've acquired the lock it's possible there is no work left to do.
                        // iterate backwards, since RemoveAt repositions subsequent elements
                        var howManyToRemove = _cache.Count - _cacheSize;
                        for (int i = howManyToRemove - 1; i >= 0; i--)
                        {
                            if (i <= _cache.Count - 1 && _canDisposeBitmap(_cache[i].Item2))
                            {
                                _cache[i].Item2.Dispose();
                                _cache.RemoveAt(i);
                            }
                        }
                    }
                }));
            }
        }

        public void AddAsync(List<string> arList, PictureBox mainThread)
        {
            ThreadPool.QueueUserWorkItem(
            delegate
            {
                Add(arList.ToArray());
            });
        }

        public static Bitmap GetBitmap(string path)
        {
            // load from disk
            Bitmap imFromFile = null;
            try
            {
                if (path.ToLowerInvariant().EndsWith(".webp"))
                {
                    byte[] bytesData = File.ReadAllBytes(path);
                    var decoder = new Imazen.WebP.SimpleDecoder();
                    imFromFile = decoder.DecodeFromBytes(bytesData, bytesData.LongLength);
                }
                else
                {
                    imFromFile = new Bitmap(path);

                    // some image files, especially jpgs from a scanner, have custom resolutions,
                    // I prefer overriding the resolution.
                    imFromFile.SetResolution(96.0f, 96.0f);
                }
            }
            catch (Exception)
            {
                if (!Configs.Current.SupressDialogs)
                {
                    MessageBox.Show("Could not show the image " + path);
                }

                if (imFromFile != null)
                    imFromFile.Dispose();
                imFromFile = new Bitmap(1, 1);
            }

            return imFromFile;
        }

        public Bitmap GetResizedBitmap(string path, out int nOrigW, out int nOrigH)
        {
            if (!FilenameUtils.LooksLikeImage(path) || !File.Exists(path))
            {
                nOrigW = 0;
                nOrigH = 0;
                return new Bitmap(1, 1);
            }

            Bitmap imFromFile = GetBitmap(path);

            // resize and preserve ratio
            using (imFromFile)
            {
                nOrigW = imFromFile.Width;
                nOrigH = imFromFile.Height;
                if (imFromFile.Width > MaxWidth || imFromFile.Height > MaxHeight)
                {
                    var ratio = Math.Min((double)MaxWidth / imFromFile.Width,
                        (double)MaxHeight / imFromFile.Height);
                    int newwidth = (int)(imFromFile.Width * ratio);
                    int newheight = (int)(imFromFile.Height * ratio);
                    return ResizeImage(imFromFile, newwidth, newheight, path);
                }
                else
                {
                    // make a copy of the bitmap, otherwise the file remains locked
                    return new Bitmap(imFromFile);
                }
            }
        }

        public static Bitmap ResizeImage(Bitmap srcImage, int newWidth, int newHeight, string pathForLogging)
        {
            // Kris Erickson, stackoverflow 87753.
            // also considered pixelformat Imaging.PixelFormat.Format32bppPArgb
            // GDI seems to have a lock, so we don't get great concurrency.
            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                if (newWidth != srcImage.Width || newHeight != srcImage.Height)
                {
                    gr.SmoothingMode = SmoothingMode.HighQuality;
                    gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    gr.DrawImage(srcImage, new Rectangle(0, 0, newWidth, newHeight));
                }
                else
                {
                    gr.DrawImageUnscaled(srcImage, 0, 0);
                }
            }

            if (newImage == null)
            {
                throw new CoordinatePicturesException("do not expect newImage to be null. " + pathForLogging);
            }
            else
            {
                return newImage;
            }
        }
    }

    // show a full-resolution excerpt of a large image
    public sealed class ImageViewExcerpt : IDisposable
    {
        public ImageViewExcerpt(int maxwidth, int maxheight)
        {
            MaxWidth = maxwidth;
            MaxHeight = maxheight;
            Bmp = new Bitmap(1, 1);
        }

        public int MaxWidth { get; private set; }
        public int MaxHeight { get; private set; }
        public Bitmap Bmp { get; private set; }

        public void MakeBmp(string path, int clickX, int clickY, int wasWidth, int wasHeight)
        {
            Bmp.Dispose();
            Bmp = new Bitmap(MaxWidth, MaxHeight);
            if (path == null || !FilenameUtils.LooksLikeImage(path) || !File.Exists(path))
                return;

            using (Bitmap fullImage = ImageCache.GetBitmap(path))
            {
                if (fullImage.Width == 1 || fullImage.Height == 1)
                    return;

                int shiftx, shifty;
                GetShiftAmount(fullImage, clickX, clickY, wasWidth, wasHeight, out shiftx, out shifty);

                // draw the entire image, but pushed off to the side
                using (Graphics gr = Graphics.FromImage(Bmp))
                {
                    gr.FillRectangle(Brushes.White, 0, 0, MaxWidth, MaxHeight);
                    gr.DrawImageUnscaled(fullImage, -shiftx, -shifty);
                }
            }
        }

        public void GetShiftAmount(Bitmap fullImage, int clickX, int clickY, int wasWidth, int wasHeight, out int shiftx, out int shifty)
        {
            // find where the user clicked, and then show that place in the center at full resolution.
            var xcenter = (int)(fullImage.Width * (clickX / ((double)wasWidth)));
            var ycenter = (int)(fullImage.Height * (clickY / ((double)wasHeight)));
            shiftx = xcenter - (MaxWidth / 2);
            shifty = ycenter - (MaxHeight / 2);
        }

        public void Dispose()
        {
            Bmp.Dispose();
        }
    }
}

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
        public ImageViewExcerpt Excerpt { get; private set; }
        public int MaxHeight { get; private set; }
        public int MaxWidth { get; private set; }
        List<Tuple<string, Bitmap, int, int, DateTime>> _cache;
        object _lock = new object();
        int _cacheSize;
        Func<Action, bool> _callbackOnUiThread;
        Func<Bitmap, bool> _canDisposeBitmap;

        public ImageCache(int maxwidth, int maxheight, int cacheSize,
            Func<Action, bool> callbackOnUiThread, Func<Bitmap, bool> canDisposeBitmap)
        {
            MaxWidth = maxwidth;
            MaxHeight = maxheight;
            _cacheSize = cacheSize;
            _callbackOnUiThread = callbackOnUiThread;
            _canDisposeBitmap = canDisposeBitmap;
            _cache = new List<Tuple<string, Bitmap, int, int, DateTime>>();
            Excerpt = new ImageViewExcerpt(maxwidth, maxheight);
        }

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

        int SearchForUpToDateCacheEntry(string path)
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
                var dtNow = File.Exists(path) ? new FileInfo(path).LastWriteTimeUtc : System.DateTime.MaxValue;
                if (dtNow != _cache[index].Item5)
                {
                    _cache.RemoveAt(index);
                    index = -1;
                }
            }

            return index;
        }

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
                        throw new CoordinatePicturesException("did not find image we just cached");
                }

                nOrigW = _cache[index].Item3;
                nOrigH = _cache[index].Item4;
                return _cache[index].Item2;
            }
        }

        public void Add(string[] paths)
        {
            bool checkTooBig = false;
            foreach (var path in paths)
            {
                if (path == null)
                    continue;

                lock (_lock)
                {
                    if (SearchForUpToDateCacheEntry(path) != -1)
                        continue;

                    // could get the bitmap out of lock... but that risks redundant work
                    int nOrigW = 0, nOrigH = 0;
                    var b = GetResizedBitmap(path, out nOrigW, out nOrigH);
                    var lastModified = File.Exists(path) ? new FileInfo(path).LastWriteTimeUtc : DateTime.MaxValue;
                    _cache.Add(new Tuple<string, Bitmap, int, int, DateTime>(
                        path, b, nOrigW, nOrigH, lastModified));
                    checkTooBig = _cache.Count > _cacheSize;
                }
            }

            // we don't want to Dispose() the currently shown image.
            // note that since checkTooBig is outside the lock, it might have false negatives, but that's ok.
            if (checkTooBig)
            {
                _callbackOnUiThread.Invoke(new Action(() =>
                {
                    lock (_lock)
                    {
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
                    // I've found that I get best results when overriding the resolution here.
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
                    var ratio = Math.Min((double)MaxWidth / imFromFile.Width, (double)MaxHeight / imFromFile.Height);
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
            // use pixelformat, System.Drawing.Imaging.PixelFormat.Format32bppPArgb
            // unfortunately, GDI seems to have a lock, so we don't get great concurrency.
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

    public sealed class ImageViewExcerpt : IDisposable
    {
        public int MaxWidth { get; private set; }
        public int MaxHeight { get; private set; }
        public Bitmap Bmp { get; private set; }

        public ImageViewExcerpt(int maxwidth, int maxheight)
        {
            MaxWidth = maxwidth;
            MaxHeight = maxheight;
            Bmp = new Bitmap(1, 1);
        }

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

                // find where the user clicked, and then show that place in the center at full resolution.
                var xcenter = (int)(fullImage.Width * (clickX / ((double)wasWidth)));
                var ycenter = (int)(fullImage.Height * (clickY / ((double)wasHeight)));
                var shiftx = xcenter - (MaxWidth / 2);
                var shifty = ycenter - (MaxHeight / 2);

                // draw the entire image, but pushed off to the side
                using (Graphics gr = Graphics.FromImage(Bmp))
                {
                    gr.FillRectangle(Brushes.White, 0, 0, MaxWidth, MaxHeight);
                    gr.DrawImageUnscaled(fullImage, -shiftx, -shifty);
                }
            }
        }

        public void Dispose()
        {
            Bmp.Dispose();
        }
    }
}

// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace labs_coordinate_pictures
{
    public struct CacheEntry
    {
        public string Path;
        public Bitmap Image;
        public int Width;
        public int Height;
        public DateTime Modtime;
        public long Filesize;

        public CacheEntry(string path, Bitmap image, int width,
            int height, DateTime modtime, long filesize)
        {
            this.Path = path;
            this.Image = image;
            this.Width = width;
            this.Height = height;
            this.Modtime = modtime;
            this.Filesize = filesize;
        }
    }

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

        // if asked, we'll read exif metadata to rotate jpg images
        JpegRotationFinder _shouldRotateThisImage;

        public ImageCache(
            int maxWidth,
            int maxHeight,
            int cacheSize,
            Func<Action, bool> callbackOnUiThread,
            Func<Bitmap, bool> canDisposeBitmap,
            JpegRotationFinder shouldRotateThisImage)
        {
            ResizeToFit = true;
            ResizeFactor = 1;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            _cacheSize = cacheSize;
            _callbackOnUiThread = callbackOnUiThread;
            _canDisposeBitmap = canDisposeBitmap;
            _shouldRotateThisImage = shouldRotateThisImage;
            Excerpt = new ImageViewExcerpt(maxWidth, maxHeight);
        }

        public ImageViewExcerpt Excerpt { get; private set; }
        public int MaxHeight { get; private set; }
        public int MaxWidth { get; private set; }
        public int ResizeFactor { get; set; }
        public bool ResizeToFit { get; set; }
        public int VerticalScrollFactor { get; set; }

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

                originalWidth = _list[index].Width;
                originalHeight = _list[index].Height;
                return _list[index].Image;
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
                    var bitmap = GetResizedBitmap(path, out int originalWidth, out int originalHeight);
                    DateTime lastmod = DateTime.MinValue;
                    long filesize = 0;
                    if (File.Exists(path))
                    {
                        var fileInfo = new FileInfo(path);
                        lastmod = fileInfo.LastWriteTimeUtc;
                        filesize = fileInfo.Length;
                    }

                    bitmap = ClassImageOps.ResizeImageByFactor(bitmap, ResizeFactor, VerticalScrollFactor);
                    _list.Add(new CacheEntry(
                        path, bitmap, originalWidth, originalHeight, lastmod, filesize));

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
                            if (i <= _list.Count - 1 && _canDisposeBitmap(_list[i].Image))
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
        public static Bitmap GetBitmap(string path, JpegRotationFinder shouldrotate, out bool bitmapWillLockFile)
        {
            Bitmap bitmap = null;
            try
            {
                if (ModeUtils.IsWebp(path))
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
                if (ModeUtils.IsWebp(path) &&
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
                bitmap = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);
            }

            if (shouldrotate != null && shouldrotate.ShouldRotate(path))
            {
                bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            }

            return bitmap;
        }

        public Bitmap GetResizedBitmap(string path, out int originalWidth, out int originalHeight)
        {
            if (!FilenameUtils.LooksLikeImage(path) || !File.Exists(path))
            {
                originalWidth = 0;
                originalHeight = 0;
                return new Bitmap(1, 1, PixelFormat.Format32bppPArgb);
            }

            Bitmap bitmapFull = GetBitmap(path, _shouldRotateThisImage, out bool bitmapWillLockFile);

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
                    return ClassImageOps.ResizeImage(bitmapFull, newWidth, newHeight, this.ResizeToFit, path);
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

        public void InvalidateCache()
        {
            _list = new ListOfCachedImages();
        }
    }

    // enforces that Dispose() when removing images from the cache.
    public sealed class ListOfCachedImages : IDisposable
    {
        List<CacheEntry> _cache =
            new List<CacheEntry>();

        public int Count
        {
            get { return _cache.Count; }
        }

        public CacheEntry this[int index]
        {
            get { return _cache[index]; }
        }

        // returns index into _cache if found and up to date, or otherwise -1.
        public int SearchForUpToDateCacheEntry(string path)
        {
            // linear search is fine as we only have a few entries.
            int indexFound = _cache.FindIndex((entry) => entry.Path == path);
            if (indexFound != -1)
            {
                // is it up to date though? if it is out of date, invalidate the cached entry.
                DateTime lastmod = DateTime.MinValue;
                long filesize = 0;
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    lastmod = fileInfo.LastWriteTimeUtc;
                    filesize = fileInfo.Length;
                }

                var isCurrent = lastmod == _cache[indexFound].Modtime &&
                    filesize == _cache[indexFound].Filesize;

                if (!isCurrent)
                {
                    RemoveAt(indexFound);
                    indexFound = -1;
                }
            }

            return indexFound;
        }

        public void Add(CacheEntry entry)
        {
            _cache.Add(entry);
        }

        public void RemoveAt(int index)
        {
            if (_cache[index].Image != null)
            {
                _cache[index].Image.Dispose();
            }

            _cache.RemoveAt(index);
        }

        public void Dispose()
        {
            foreach (var entry in _cache)
            {
                if (entry.Image != null)
                {
                    entry.Image.Dispose();
                }
            }

            _cache.Clear();
        }
    }
}

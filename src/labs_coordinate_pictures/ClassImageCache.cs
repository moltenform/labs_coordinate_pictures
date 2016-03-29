using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public class ImageCache : IDisposable
    {
        public static Bitmap BitmapBlank = new Bitmap(1, 1);
        public int MaxHeight { get; private set; }
        public int MaxWidth { get; private set; }
        List<Tuple<string, Bitmap, int, int, DateTime>> _cache;
        object _lock = new object();
        int _cacheSize;
        
        public ImageCache(int maxwidth, int maxheight, int cacheSize)
        {
            MaxWidth = maxwidth;
            MaxHeight = maxheight;
            _cacheSize = cacheSize;
            _cache = new List<Tuple<string, Bitmap, int, int, DateTime>>();
            if (_cacheSize <= 1)
            {
                throw new CoordinatePicturesException("cache size too small");
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var tuple in _cache)
                    if (tuple.Item2 != null)
                        tuple.Item2.Dispose();
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
                var dtNow = File.Exists(path) ? new FileInfo(path).LastWriteTimeUtc : new System.DateTime();
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

                    Add(path);
                    index = SearchForUpToDateCacheEntry(path);
                    if (index == -1)
                        throw new CoordinatePicturesException("did not find image we just cached");
                }

                nOrigW = _cache[index].Item3;
                nOrigH = _cache[index].Item4;
                return _cache[index].Item2;
            }
        }

        public void Add(string path)
        {
            if (path == null)
                return;

            lock (_lock)
            {
                if (SearchForUpToDateCacheEntry(path) != -1)
                    return;

                // could get the bitmap out of lock... but that risks redundant work
                int nOrigW = 0, nOrigH = 0;
                var b = GetBitmap(path, out nOrigW, out nOrigH);
                var lastModified = File.Exists(path) ? new FileInfo(path).LastWriteTimeUtc : new System.DateTime();
                _cache.Add(new Tuple<string, Bitmap, int, int, DateTime>(
                    path, b, nOrigW, nOrigH, lastModified));

                if (_cache.Count > _cacheSize)
                {
                    _cache[0].Item2.Dispose();
                    _cache.RemoveAt(0);
                }
            }
        }

        public void AddAsync(List<string> arList)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                foreach (var s in arList)
                    Add(s);
            }, null);
        }

        public Bitmap GetBitmap(string path, out int nOrigW, out int nOrigH)
        {
            if (!FilenameUtils.LooksLikeImage(path) || !File.Exists(path))
            {
                nOrigW = 0;
                nOrigH = 0;
                return BitmapBlank;
            }

            // load from disk
            Bitmap imFromFile;
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
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception loading "+path+"\r\n"+e);
                imFromFile = new Bitmap(1, 1);
            }

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
}

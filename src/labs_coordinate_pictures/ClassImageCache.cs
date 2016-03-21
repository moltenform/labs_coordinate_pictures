using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;


namespace labs_coordinate_pictures
{
    public class ImageCache
    {
        List<Tuple<string, Bitmap, int, int, DateTime>> _cache;
        int _height;
        int _width;
        int _cacheSize;
        public ImageCache(int width, int height, int cacheSize)
        {
            _height = height;
            _width = width;
            _cacheSize = cacheSize;
            _cache = new List<Tuple<string, Bitmap, int, int, DateTime>>();
            if (_cacheSize <= 1)
            {
                throw new CoordinatePicturesException("cache size too small");
            }
        }
        private int SearchFor(string s)
        {
            int nFound = -1;
            for (int i = 0; i < _cache.Count; i++)
                if (_cache[i].Item1 == s)
                {
                    nFound = i;
                    break;
                }

            if (nFound != -1)
            {
                var dtNow = File.Exists(s) ? new FileInfo(s).LastWriteTimeUtc : new System.DateTime();
                if (dtNow != _cache[nFound].Item5)
                {
                    _cache.RemoveAt(nFound);
                    nFound = -1;
                }
            }

            return nFound;
        }
        public Bitmap Get(string s, out int nOrigW, out int nOrigH)
        {
            lock (_cache)
            {
                int index = SearchFor(s);
                if (index != -1)
                {
                    nOrigW = _cache[index].Item3;
                    nOrigH = _cache[index].Item4;
                    if (_cache[index].Item2 == null)
                        System.Windows.Forms.MessageBox.Show("null");
                    return _cache[index].Item2;
                }

                SimpleLog.Current.WriteVerbose("adding to cache " + s);
                Add(s);
                index = SearchFor(s);
                nOrigW = _cache[index].Item3;
                nOrigH = _cache[index].Item4;
                if (_cache[index].Item2 == null)
                    System.Windows.Forms.MessageBox.Show("null");
                return _cache[index].Item2;
            }
        }
        public void Add(string s)
        {
            if (s == null) return;
            lock (_cache)
            {
                if (SearchFor(s) != -1)
                    return;

                int nOrigW = 0, nOrigH = 0;
                var b = GetBitmap(s, out nOrigW, out nOrigH); // note, could do it out of lock but that risks redundant work
                _cache.Add(new Tuple<string, Bitmap, int, int, DateTime>(s, b, nOrigW, nOrigH,
                    File.Exists(s) ? new FileInfo(s).LastWriteTimeUtc : new System.DateTime()));
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

        public Bitmap GetBitmap(string s, out int nOrigW, out int nOrigH)
        {
            if (!FilenameUtils.LooksLikeImage(s) || !File.Exists(s))
            {
                nOrigW = 0;
                nOrigH = 0;
                return new Bitmap(1, 1);
            }

            Bitmap imFromFile;
            byte[] bytesData;
            if (s.ToLowerInvariant().EndsWith(".webp"))
            {
                bytesData = File.ReadAllBytes(s);
                var decoder = new Imazen.WebP.SimpleDecoder();
                imFromFile = decoder.DecodeFromBytes(bytesData, bytesData.LongLength);
            }
            else
            {
                imFromFile = new Bitmap(s);
            }
            nOrigW = imFromFile.Width;
            nOrigH = imFromFile.Height;
            if (imFromFile.Width > _width || imFromFile.Height > _height)
            {
                var ratio = Math.Min((double)_width / imFromFile.Width, (double)_height / imFromFile.Height);
                int newwidth = (int)(imFromFile.Width * ratio);
                int newheight = (int)(imFromFile.Height * ratio);
                Bitmap bmpNew = ResizeImage(imFromFile, newwidth, newheight);
                imFromFile.Dispose();
                return bmpNew;
            }
            else
            {
                // make a copy of the bitmap, otherwise the file remains locked
                Bitmap bmpNew = ResizeImage(imFromFile, imFromFile.Width, imFromFile.Height);
                imFromFile.Dispose();
                return bmpNew;
            }
        }

        public static Bitmap ResizeImage(Bitmap srcImage, int newWidth, int newHeight)
        {
            //Kris Erickson, stackoverflow 87753.
            // use pixelformat , System.Drawing.Imaging.PixelFormat.Format32bppPArgb?
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

            return newImage;
        }
    }
}

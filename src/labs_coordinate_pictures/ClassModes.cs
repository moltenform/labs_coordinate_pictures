// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    // a 'category' is a string and associated keyboard shortcut, the string can be
    // appended to a filename to indicate that the file belongs to the category.
    // see SortingImages.md to read more about modes and categories.
    public static class ModeUtils
    {
        // "A/categoryReadable/categoryId" to Tuple("A", "categoryReadable", "categoryId")
        // input must be in the form A/categoryReadable/categoryId
        // A is the keyboard shortcut,
        // categoryReadable is the readable name, and
        // categoryId will be appended to the filename.
        public static Tuple<string, string, string>[] CategoriesStringToTuple(string s)
        {
            var tuples = new List<Tuple<string, string, string>>();
            if (string.IsNullOrWhiteSpace(s))
            {
                // valid to have no categories defined
                return tuples.ToArray();
            }

            var categories = s.Split(new char[] { '|' });
            foreach (var category in categories)
            {
                var parts = category.Split(new char[] { '/' });
                string explain = "category must be in form A/categoryReadable/categoryId, where " +
                    "A is a single numeral or capital letter, but got " + category;

                // there should be three parts separated by /
                if (parts.Length != 3)
                {
                    throw new CoordinatePicturesException(explain);
                }

                // first part must be one digit
                var isValidDigit = (parts[0][0] >= 'A' && parts[0][0] <= 'Z') ||
                    (parts[0][0] >= '0' && parts[0][0] <= '9');
                if (parts[0].Length != 1 || !isValidDigit)
                {
                    throw new CoordinatePicturesException(explain);
                }

                // second and third parts must be non-empty
                if (parts[1].Length == 0 || parts[2].Length == 0)
                {
                    throw new CoordinatePicturesException(explain);
                }

                tuples.Add(Tuple.Create(parts[0], parts[1], parts[2]));
            }

            return tuples.ToArray();
        }

        public static Tuple<string, string, string>[] ModeToTuples(ModeBase mode)
        {
            var categoriesString = Configs.Current.Get(mode.GetCategories());
            return ModeUtils.CategoriesStringToTuple(categoriesString);
        }

        public static void UseDefaultCategoriesIfFirstRun(ModeBase mode)
        {
            if (mode.GetDefaultCategories() != null &&
                string.IsNullOrEmpty(Configs.Current.Get(mode.GetCategories())))
            {
                Configs.Current.Set(mode.GetCategories(), mode.GetDefaultCategories());
            }
        }

        public static bool IsWebp(string s)
        {
            return FilenameUtils.IsExtensionInList(s, new string[] { ".webp" });
        }

        public static bool IsJpg(string s)
        {
            return FilenameUtils.IsExtensionInList(s, new string[] { ".jpg", ".jpeg" });
        }
    }

    // a 'mode' specifies a list of supported file extensions and can provide custom features.
    // for example a mode for sorting images could have different actions than a mode for audio.
    // a mode also defines what happens when the user signals "completion" by pressing Ctrl+Enter.
    // see SortingImages.md to read more about modes and categories.
    // these are essentially callbacks provided to a FormGallery form.
    public abstract class ModeBase
    {
        public abstract bool SupportsRename();
        public abstract bool SupportsCompletionAction();
        public abstract void OnOpenItem(string path, FormGallery obj);
        public abstract ConfigKey GetCategories();
        public abstract string GetDefaultCategories();
        public abstract string[] GetFileTypes();
        public abstract void OnBeforeAssignCategory();

        public virtual void OnCompletionAction(string baseDirectory, string path,
            string pathWithoutCategory, Tuple<string, string, string> category)
        {
        }

        // list of (keyboard shortcut, label text).
        // shortcut is not automatically bound, must be implemented in OnCustomCommand.
        public virtual Tuple<string, string>[] GetDisplayCustomCommands()
        {
            return new Tuple<string, string>[] { };
        }

        // modes can perform actions on keyup events in FormGallery.
        public virtual void OnCustomCommand(FormGallery form,
            bool shift, bool alt, bool control, Keys keys)
        {
        }

        // is it ok if a file has a category that's not in the list?
        public virtual bool AllowUnknownCategories()
        {
            return false;
        }
    }

    // modes that support rename and view image files.
    public abstract class ModeCategorizeAndRenameBase : ModeBase
    {
        public override bool SupportsRename()
        {
            return true;
        }

        public override bool SupportsCompletionAction()
        {
            return true;
        }

        public override void OnBeforeAssignCategory()
        {
        }

        public override void OnOpenItem(string path, FormGallery obj)
        {
        }

        public override string[] GetFileTypes()
        {
            if (FilenameUtils.EnableWebp())
            {
                return new string[] { ".jpg", ".png", ".gif", ".bmp", ".webp" };
            }
            else
            {
                return new string[] { ".jpg", ".png", ".gif", ".bmp" };
            }
        }
    }

    // a mode that simply moves images into subdirectories when complete.
    public sealed class ModeCategorizeAndRename : ModeCategorizeAndRenameBase
    {
        public override ConfigKey GetCategories()
        {
            return ConfigKey.CategoriesModeCategorizeAndRename;
        }

        public override string GetDefaultCategories()
        {
            return "A/art/art|C/comedy/comedy|R/serious/serious|Q/other/other|T/twitter/twitter";
        }

        public override void OnCompletionAction(string baseDirectory,
            string path, string pathWithoutCategory, Tuple<string, string, string> category)
        {
            // create a directory <baseDirectory>/<categoryname>
            var targetDir = Path.Combine(baseDirectory, category.Item3);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // move the file to <baseDirectory>/<categoryname>/file.jpg
            var newPath = Path.Combine(targetDir, Path.GetFileName(pathWithoutCategory));
            if (File.Exists(newPath))
            {
                Utils.MessageErr("File already exists " + newPath);
                return;
            }

            SimpleLog.Current.WriteLog("File.Move " + path + " to " + newPath);
            File.Move(path, newPath);
        }

        public override bool AllowUnknownCategories()
        {
            return true;
        }
    }

    // a mode that prepares files for img_resize_keep_exif.py.
    public sealed class ModeResizeKeepExif : ModeCategorizeAndRenameBase
    {
        bool _hasRunCompletionAction = false;

        public override ConfigKey GetCategories()
        {
            return ConfigKey.CategoriesModeResizeKeepExif;
        }

        public override string GetDefaultCategories()
        {
            // filename suffixes that img_resize_keep_exif.py will recognize
            return "Q/288 typ 10%/288h|W/432 typ 15%/432h|E/576 typ 20%/576h|" +
                "1/720 typ 25%/720h|3/864 typ 35%/864h|4/1008 typ 40%/1008h|" +
                "5/1152 typ 45%/1152h|6/1296 typ 50%/1296h|7/1440 typ 55%/1440h|" +
                "8/1584 typ 60%/1584h|9/1728 typ 65%/1728h|P/1872 typ 75%/1872h|0/100%/100%";
        }

        public override bool SupportsRename()
        {
            return false;
        }

        public override bool SupportsCompletionAction()
        {
            return Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures);
        }

        public override void OnCompletionAction(string baseDirectory,
            string path, string pathWithoutCategory, Tuple<string, string, string> category)
        {
            if (_hasRunCompletionAction ||
                !Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures))
            {
                return;
            }

            // we could start the script directly, but I currently prefer the
            // traditional workflow of manually running the python script.
            var url = "https://moltenform.com/page/labs-coordinate-pictures" +
                "/doc/keeping-metadata.html";
            Utils.MessageBox("Currently, resize+keep exif is done manually by " +
                "running a python script." + Utils.NL + Utils.NL +
                "See the online documentation (copied to clipboard) at " + url);
            Clipboard.SetText(url);

            _hasRunCompletionAction = true;
        }
    }

    // a mode that simply stamps 'size is good' on files when the user presses Shift+A.
    // when complete, user presses Ctrl+Enter and the 'size is good' is removed from filenames.
    public sealed class ModeCheckFilesizes : ModeCategorizeAndRenameBase
    {
        public override ConfigKey GetCategories()
        {
            return ConfigKey.CategoriesModeCheckFilesizes;
        }

        public override string GetDefaultCategories()
        {
            return "A/size is good/size is good";
        }

        public override Tuple<string, string>[] GetDisplayCustomCommands()
        {
            return new Tuple<string, string>[]
            {
                new Tuple<string, string>("Ctrl-2", "Mark small files as finished")
            };
        }

        public override void OnCustomCommand(FormGallery form,
            bool shift, bool alt, bool control, Keys keys)
        {
            if (!shift && control && !alt && keys == Keys.D2)
            {
                AutoAcceptSmallFiles(form);
            }
        }

        public void AutoAcceptSmallFiles(FormGallery form, int capJpg = 0, int capWebp = 0)
        {
            var list = form.GetFilelist().GetList();

            // first, check for duplicate names
            foreach (var path in list)
            {
                var similar = FindSimilarFilenames.FindSimilarNames(
                    path, GetFileTypes(), list, out bool nameHasSuffix, out string pathWithoutSuffix);
                if (similar.Count != 0)
                {
                    Utils.MessageErr("the file " + path + " has similar name(s) "
                        + string.Join(Utils.NL, similar));
                    return;
                }
            }

            // then, accept the small files
            if (capJpg == 0)
            {
                var optCapWebp = InputBoxForm.GetInteger("Accept webp files less than this many Kb:", 50);
                if (!optCapWebp.HasValue)
                {
                    return;
                }

                var optCapJpg = InputBoxForm.GetInteger("Accept jpg files less than this many Kb:", 100);
                if (!optCapJpg.HasValue)
                {
                    return;
                }

                capWebp = 1024 * optCapWebp.Value;
                capJpg = 1024 * optCapJpg.Value;
            }

            int countAccepted = 0;
            var sizeIsGoodCategory = GetDefaultCategories().Split(new char[] { '/' })[2];
            foreach (var path in list)
            {
                var fileLength = new FileInfo(path).Length;
                if (fileLength > 0 &&
                    ((ModeUtils.IsWebp(path) &&
                    fileLength < capWebp) ||
                    (ModeUtils.IsJpg(path) &&
                    fileLength < capJpg)))
                {
                    countAccepted++;
                    var newPath = FilenameUtils.AddCategoryToFilename(path, sizeIsGoodCategory);
                    form.WrapMoveFile(path, newPath);
                }
            }

            Utils.MessageBox("Accepted for " + countAccepted + " images.", true);
        }

        public override void OnCompletionAction(string baseDirectory,
            string path, string pathWithoutCategory, Tuple<string, string, string> category)
        {
            // just remove the mark from the file, don't need to do anything else.
            if (File.Exists(pathWithoutCategory))
            {
                Utils.MessageBox("File already exists + " + pathWithoutCategory);
            }
            else
            {
                SimpleLog.Current.WriteLog("File.Move " + path + " to " + pathWithoutCategory);
                File.Move(path, pathWithoutCategory);
            }
        }
    }

    public abstract class ModeAudioBase : ModeBase
    {
        public override bool SupportsRename()
        {
            return false;
        }

        // for every category there is a corresponding dropq*.py script.
        public override string GetDefaultCategories()
        {
            return "Q/Enc 16 (del)/16|W/Enc 24 (lnk)/24|E/Encode 96/96|" +
                "1/Encode 128/128|2/Encode 144/144|3/Encode 160/160|" +
                "4/Encode 192/192|5/Encode 224/224|6/Encode 256/256|" +
                "7/Encode 288/288|8/Encode 320/320|9/Encode 640/640|0/Encode Flac/flac";
        }

        public override void OnBeforeAssignCategory()
        {
            // some audio players hold a lock on the file while playing.
            // so, before renaming the file, tell the audio player to play silence.flac.
            FormatConversions.PlayMedia(null);
        }

        public override void OnOpenItem(string path, FormGallery obj)
        {
            FormatConversions.PlayMedia(path);
            Thread.Sleep(1000);
        }
    }

    public sealed class ModeMarkWavQuality : ModeAudioBase
    {
        public override bool SupportsCompletionAction()
        {
            return true;
        }

        public override ConfigKey GetCategories()
        {
            return ConfigKey.CategoriesModeMarkWavQuality;
        }

        public override string[] GetFileTypes()
        {
            return new string[] { ".wav", ".flac" };
        }

        // use dropq*.py scripts to convert wav to m4a.
        public override void OnCompletionAction(string baseDirectory,
            string path, string pathWithoutCategory, Tuple<string, string, string> category)
        {
            if (path.ToLowerInvariant().EndsWith(".wav", StringComparison.Ordinal) ||
                path.ToLowerInvariant().EndsWith(".flac", StringComparison.Ordinal))
            {
                if (path.ToLowerInvariant().EndsWith(".flac", StringComparison.Ordinal) &&
                    category.Item3 == "flac")
                {
                    // no conversion needed
                    SimpleLog.Current.WriteLog("File.Move " + path + " to " + pathWithoutCategory);
                    File.Move(path, pathWithoutCategory);
                    return;
                }

                // 1) convert song__MARKAS__144.wav to song__MARKAS__144.m4a
                var pathM4a = FormatConversions.RunM4aConversion(path, category.Item3);
                if (!string.IsNullOrEmpty(pathM4a))
                {
                    // 2) see that song__MARKAS__144.m4a should be renamed to song.m4a
                    var newPathM4a = Path.GetDirectoryName(pathWithoutCategory) +
                        Utils.Sep + Path.GetFileNameWithoutExtension(pathWithoutCategory) +
                        Path.GetExtension(pathM4a);

                    if (File.Exists(newPathM4a))
                    {
                        Utils.MessageErr("already exists. could not move " +
                            pathM4a + " to " + newPathM4a);
                    }
                    else
                    {
                        // 3) move song__MARKAS__144.m4a to song.m4a
                        // 4) delete song__MARKAS__144.wav
                        SimpleLog.Current.WriteLog("File.Move " + pathM4a + " to " + newPathM4a);
                        File.Move(pathM4a, newPathM4a);
                        Utils.SoftDelete(path);
                    }
                }
            }
        }
    }

    public sealed class ModeMarkMp3Quality : ModeAudioBase
    {
        public override bool SupportsCompletionAction()
        {
            if (Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures))
            {
                string explanation = @"Here's a description of the completion action. 

If a song is marked with __MARKAS__16, it will be 'deleted' (sent to the raw material dir).
If a song is marked with __MARKAS__24, it will be marked as (vv) and a copy sent to raw material.
If a song is marked with __MARKAS__144, it will be marked as (14).
If a song is marked with __MARKAS__flac, it will be marked as (^).

Continue?";

                if (Utils.AskToConfirm(explanation))
                {
                    return true;
                }
            }

            return false;
        }

        public override ConfigKey GetCategories()
        {
            return ConfigKey.CategoriesModeMarkMp3Quality;
        }

        public override string[] GetFileTypes()
        {
            return new string[] { ".wav", ".mp3", ".mp4", ".m4a", ".ogg", ".wma", ".flac" };
        }

        

        
    }
}

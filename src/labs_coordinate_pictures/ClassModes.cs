using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public static class ModeUtils
    {
        public static Tuple<string, string, string>[] CategoriesStringToTuple(string s)
        {
            var ret = new List<Tuple<string, string, string>>();
            if (string.IsNullOrWhiteSpace(s))
                return ret.ToArray();

            var categories = s.Split(new char[] { '|' });
            string explain = "category must be in form A/categoryReadable/categoryId, where A is a single numeral or capital letter, but got ";
            foreach (var category in categories)
            {
                var parts = category.Split(new char[] { '/' });
                if (parts.Length != 3)
                    throw new CoordinatePicturesException(explain + category);

                var validDigit = (parts[0][0] >= 'A' && parts[0][0] <= 'Z') ||
                    (parts[0][0] >= '0' && parts[0][0] <= '9');
                if (parts[0].Length != 1 || !validDigit)
                    throw new CoordinatePicturesException(explain + category);

                if (parts[1].Length == 0 || parts[2].Length == 0)
                    throw new CoordinatePicturesException(explain + category);

                ret.Add(new Tuple<string, string, string>(parts[0], parts[1], parts[2]));
            }
            return ret.ToArray();
        }

        public static Tuple<string, string, string>[] ModeToTuples(ModeBase mode)
        {
            var categoriesString = Configs.Current.Get(mode.GetCategories());
            return ModeUtils.CategoriesStringToTuple(categoriesString);
        }

        public static void UseDefaultCategoriesIfFirstRun(ModeBase mode)
        {
            if (mode.GetDefaultCategories() != null && Configs.Current.Get(mode.GetCategories()) == "")
            {
                Configs.Current.Set(mode.GetCategories(), mode.GetDefaultCategories());
            }
        }
    }

    public abstract class ModeBase
    {
        public abstract bool SupportsRename();
        public abstract bool SupportsCompletionAction();
        public abstract void OnOpenItem(string sPath, FormGallery obj);
        public abstract ConfigKey GetCategories();
        public abstract string GetDefaultCategories();
        public abstract void OnCompletionAction(string sBaseDir, string sPath, string sPathNoMark, Tuple<string, string, string> chosen);
        public abstract string[] GetFileTypes();
        public abstract void OnBeforeAssignCategory();
        public virtual Tuple<string, string>[] GetDisplayCustomCommands() { return new Tuple<string, string>[] { }; }
        public virtual void OnCustomCommand(FormGallery form, bool shift, bool alt, bool control, Keys keys) { }
        public virtual bool SupportsFileType(string s)
        {
            return FilenameUtils.IsExtensionInList(s, GetFileTypes());
        }
    }

    public abstract class ModeCategorizeAndRenameBase : ModeBase
    {
        public override bool SupportsRename() { return true; }
        public override bool SupportsCompletionAction() { return true; }
        public override void OnBeforeAssignCategory() { }
        public override void OnOpenItem(string sPath, FormGallery obj) { }
        public override string[] GetFileTypes()
        {
            return new string[] { ".jpg", ".png", ".gif", ".bmp", ".webp" };
        }
    }

    public class ModeResizeKeepExif : ModeCategorizeAndRenameBase
    {
        public override ConfigKey GetCategories()
        {
            return ConfigKey.CategoriesModeResizeKeepExif;
        }
        public override string GetDefaultCategories()
        {
            return "Q/288 typ 10%/288h|W/432 typ 15%/432h|E/576 typ 20%/576h|1/720 typ 25%/720h|3/864 typ 35%/864h|4/1008 typ 40%/1008h|5/1152 typ 45%/1152h|6/1296 typ 50%/1296h|7/1440 typ 55%/1440h|8/1584 typ 60%/1584h|9/1728 typ 65%/1728h|P/1872 typ 75%/1872h|0/100%/100%";
        }
        public override bool SupportsRename()
        {
            return false;
        }
        public override bool SupportsCompletionAction()
        {
            MessageBox.Show("Currently, resize+keep exif is done manually by running a python script, ./tools/ben_python_img/img_convert_resize.py");
            return false;
        }
        public override void OnCompletionAction(string sBaseDir, string sPath, string sPathNoMark, Tuple<string, string, string> chosen) { }
    }

    public class ModeCategorizeAndRename : ModeCategorizeAndRenameBase
    {
        public override ConfigKey GetCategories()
        {
            return ConfigKey.CategoriesModeCategorizeAndRename;
        }
        public override string GetDefaultCategories()
        {
            return "A/art/art|C/comedy/comedy|R/serious/serious|Q/other/other";
        }
        public override void OnCompletionAction(string sBaseDir, string sPath, string sPathNoMark, Tuple<string, string, string> chosen)
        {
            // create a directory <base>/<categoryname>
            var targetdir = Path.Combine(sBaseDir, chosen.Item3);
            if (!Directory.Exists(targetdir))
            {
                Directory.CreateDirectory(targetdir);
            }

            // simply move the file to <base>/<categoryname>/file.jpg
            var newpath = Path.Combine(targetdir, Path.GetFileName(sPathNoMark));
            if (File.Exists(newpath))
            {
                MessageBox.Show("File already exists " + newpath);
                return;
            }
            File.Move(sPath, newpath);
        }
    }

    public class ModeCheckFilesizes : ModeCategorizeAndRenameBase
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
            return new Tuple<string, string>[] {
                new Tuple<string, string>("Ctrl-2", "Mark small files as finished")
            };
        }

        public override void OnCustomCommand(FormGallery form, bool shift, bool alt, bool control, Keys keys)
        {
            if (!shift && control && !alt && keys == Keys.D2)
            {
                AutoAcceptSmall(form);
            }
        }

        void AutoAcceptSmall(FormGallery form)
        {
            const int autoAcceptibleSize = 1024 * 45;
            var list = form.nav.GetList();
            bool hasMiddleName;
            string newname;

            // first, check for duplicate names
            foreach (var path in list)
            {
                var similar = FilenameFindSimilarFilenames.FindSimilarNames(path, GetFileTypes(), list, out hasMiddleName, out newname);
                if (similar.Count != 0)
                {
                    MessageBox.Show("the file " + path + " has similar name(s) " + string.Join("\r\n", similar));
                    return;
                }
            }

            // then, accept the small files
            int nAccepted = 0;
            foreach (var path in list)
            {
                if ((path.EndsWith(".webp") || path.EndsWith(".jpg"))
                    && new FileInfo(path).Length < autoAcceptibleSize)
                {
                    nAccepted++;
                    var sNewName = FilenameUtils.AddMarkToFilename(path, "size is good");
                    form.WrapMoveFile(path, sNewName);
                }
            }
            MessageBox.Show("Accepted for " + nAccepted + " images.");
        }

        public override void OnCompletionAction(string sBaseDir, string sPath, string sPathNoMark, Tuple<string, string, string> chosen)
        {
            // just remove the mark from the file, don't need to do anything else.
            if (File.Exists(sPathNoMark))
            {
                MessageBox.Show("File already exists + " + sPathNoMark);
            }
            else
            {
                File.Move(sPath, sPathNoMark);
            }
        }
    }

    public class ModeWavToM4a : ModeBase
    {
        public override bool SupportsRename() { return false; }
        public override bool SupportsCompletionAction() { return true; }
        public override ConfigKey GetCategories()
        {
            return ConfigKey.CategoriesModeWavToM4a;
        }
        public override string GetDefaultCategories()
        {
            return "Q/Enc 16 (del)/16|W/Enc 24 (lnk)/24|E/Encode 96/96|1/Encode 128/128|2/Encode 144/144|3/Encode 160/160|4/Encode 192/192|5/Encode 224/224|6/Encode 256/256|7/Encode 288/288|8/Encode 320/320|9/Encode 640/640|0/Encode Flac/flac";
        }
        public override string[] GetFileTypes()
        {
            return new string[] { ".wav" };
        }
        public override void OnBeforeAssignCategory()
        {
            // must play another song so that file can be renamed.
            Utils.PlayMedia(null /*silence*/);
        }
        public override void OnOpenItem(string sPath, FormGallery obj)
        {
            Utils.PlayMedia(sPath);
        }
        public override void OnCompletionAction(string sBaseDir, string sPath, string sPathNoMark, Tuple<string, string, string> chosen)
        {
            if (sPath.ToLowerInvariant().EndsWith(".wav"))
            {
                var newfile = Utils.RunQaacConversion(sPath, chosen.Item3);
                // todo: rename newfile, delete old file
                // todo: retcode, not presence of stderr, should indicate failure
            }
        }
    }
 }

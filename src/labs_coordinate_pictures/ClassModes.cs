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
            string explain = "category must be in form A/categoryReadable/categoryId, where A is a single capital letter, but got ";
            foreach (var category in categories)
            {
                var parts = category.Split(new char[] { '/' });
                if (parts.Length != 3)
                    throw new CoordinatePicturesException(explain + category);

                if (parts[0].Length != 1 || parts[0][0] < 'A' || parts[0][0] > 'Z')
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
        public override void OnCompletionAction(string sBaseDir, string sPath, string sPathNoMark, Tuple<string, string, string> chosen)
        {
            // create a directory <base>/<categoryname>
            var targetdir = Path.Combine(sBaseDir, chosen.Item2);
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
        }
    }
}

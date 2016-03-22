using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public abstract class ModeBase
    {
        public abstract bool SupportsRename();
        public abstract bool SupportsCompletionAction();
        public abstract void OnOpenItem(string sPath, FormGallery obj);
        public abstract ConfigsPersistedKeys GetCategories();
        public abstract void OnCompletionAction(string sBaseDir, string sPath, string sPathNoMark, Tuple<string, string, string> chosen);
        public abstract string[] GetFileTypes();
        public abstract void OnBeforeAssignCategory();
        public virtual KeyValuePair<string, string>[] GetDisplayCustomCommands() { return null; }
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
        public override ConfigsPersistedKeys GetCategories()
        {
            return ConfigsPersistedKeys.CategoriesModeCategorizeAndRename;
        }
    }

    public class ModeCheckFilesizes : ModeCategorizeAndRenameBase
    {
        public override ConfigsPersistedKeys GetCategories()
        {
            return ConfigsPersistedKeys.CategoriesModeCheckFilesizes;
        }
        public override KeyValuePair<string, string>[] GetDisplayCustomCommands()
        {
            return new KeyValuePair<string, string>[] {
                new KeyValuePair<string, string>("Mark small files as finished", "Ctrl-2")
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

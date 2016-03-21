using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public abstract class ModeBase
    {
        public abstract bool SupportsRename();
        public abstract bool SupportsAction();
        public abstract void OnAction(string sBaseDir, string sPath, string sPathNoMark, Tuple<string, string, string> chosen);
        public abstract void OnOpenItem(string sPath, FormGallery obj);
        public abstract Tuple<string, string, string>[] GetCategoriesList();
        public abstract string[] GetFileTypes();
        public abstract void OnBeforeKeyCommand();
        public virtual KeyValuePair<string, string>[] GetCustomCommands() { return null; }
        public virtual void OnCustomCommand(FormGallery form, bool shift, bool alt, bool control, Keys keys) { }

        public virtual bool SupportsFileType(string s)
        {
            return FilenameUtils.IsExtensionInList(s, GetFileTypes());
        }
    }
}

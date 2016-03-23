using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace labs_coordinate_pictures
{
    public enum ConfigsPersistedKeys
    {
        /* nb: unlike a C++ enum, it's changing the names and 
        not the order that will cause compat issues. */
        None,
        Version,
        EnablePersonalFeatures,
        EnableVerboseLogging,
        FilepathTrash,
        FilepathAltEditorImage,
        FilepathPython,
        FilepathWinMerge,
        FilepathJpegCrop,
        FilepathMozJpeg,
        FilepathWebp,
        FilepathMediaPlayer,
        FilepathMediaEditor,
        FilepathCreateSync,
        FilepathCoordMusicDirectory,
        FilepathEncodeMusicDropQDirectory,
        CategoriesModeCategorizeAndRename,
        CategoriesModeCheckFilesizes,
        GalleryViewCategories,
        MRUOpenImageDirectory,
        MRUOpenMusicDirectory,
        MRURenameImage,
        MRURenameWavAudio,
        MRURenameOther,
        MRUEditCategoriesString,
    }

    public enum InputBoxHistory
    {
        None,
        OpenImageDirectory,
        OpenMusicDirectory,
        RenameImage,
        RenameWavAudio,
        RenameOther,
        EditCategoriesString,
    }

    public class Configs
    {
        public static ConfigsPersistedKeys ConfigsPersistedKeysFromString(string s)
        {
            ConfigsPersistedKeys e = ConfigsPersistedKeys.None;
            return Enum.TryParse(s, out e) ? e : ConfigsPersistedKeys.None;
        }

        private static Configs _instance;
        private static object locker = new Object();
        internal Configs(string path) { _path = path; }
        Dictionary<ConfigsPersistedKeys, string> _persisted = new Dictionary<ConfigsPersistedKeys, string>();
        string _path;
        public static void Init(string path)
        {
            _instance = new Configs(path);
            _instance.Directory = Path.GetDirectoryName(path);
        }
        public static Configs Current
        {
            get
            {
                return _instance;
            }
        }

        public void LoadPersisted()
        {
            if (!File.Exists(_path))
                return;

            var lines = File.ReadAllLines(_path);
            for (int i=0; i<lines.Length; i++)
            {
                var line = lines[i];
                var split = line.Split(new char[] { '=' }, 2, StringSplitOptions.None);
                if (split.Length != 2)
                {
                    if (line.Trim() != "")
                    {
                        SimpleLog.Current.WriteWarning("malformed config, missing = on line " + i);
                    }
                    continue;
                }
                ConfigsPersistedKeys key = ConfigsPersistedKeysFromString(split[0]);
                if (key == ConfigsPersistedKeys.None)
                {
                    SimpleLog.Current.WriteWarning("unrecognized config key on line " + i + ", might occur if using config from future version.");
                    continue;
                }

                _persisted[key] = split[1];
            }
        }

        void SavePersisted()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in (from key in _persisted.Keys orderby key select key))
            {
                var value = _persisted[key];
                if (value != null && value != "")
                {
                    if (value.Contains("\r") || value.Contains("\n"))
                        throw new CoordinatePicturesException("config values cannot contain newline, for key "+key);
                    sb.AppendLine(key.ToString() + "=" + value);
                }
            }
            File.WriteAllText(_path, sb.ToString());
        }

        public void Set(ConfigsPersistedKeys key, string s)
        {
            _persisted[key] = s;
            SavePersisted();
        }
        public void SetBool(ConfigsPersistedKeys key, bool b)
        {
            Set(key, b ? "true" : "");
        }

        public string Get(ConfigsPersistedKeys key)
        {
            string s;
            return _persisted.TryGetValue(key, out s) ? s : "";
        }
        public bool GetBool(ConfigsPersistedKeys key)
        {
            return !string.IsNullOrEmpty(Get(key));
        }

        // in-memory non-persisted settings
        public string Directory { get; private set; }
        public bool SupressDialogs { get; set; }
    }
}

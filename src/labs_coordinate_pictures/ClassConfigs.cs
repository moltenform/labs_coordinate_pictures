// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace labs_coordinate_pictures
{
    public enum ConfigKey
    {
        // nb: unlike a C++ enum, it's changing the names and
        // not the order that will cause compat issues.
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
        FilepathMp3DirectCut,
        FilepathExifTool,
        FilepathChecksumAltEditorImage,
        FilepathChecksumPython,
        FilepathChecksumWinMerge,
        FilepathChecksumJpegCrop,
        FilepathChecksumMozJpeg,
        FilepathChecksumCWebp,
        FilepathChecksumDWebp,
        FilepathChecksumMediaPlayer,
        FilepathChecksumMediaEditor,
        FilepathChecksumCreateSync,
        FilepathChecksumEncodeMusicDropQ,
        FilepathChecksumMp3DirectCut,
        FilepathChecksumExifTool,
        CategoriesModeCategorizeAndRename,
        CategoriesModeResizeKeepExif,
        CategoriesModeCheckFilesizes,
        CategoriesModeMarkWavQuality,
        CategoriesModeMarkMp3Quality,
        GalleryViewCategories,
        MRUOpenImageDirectory,
        MRUOpenImageKeepExifDirectory,
        MRUOpenAudioDirectory,
        MRUOpenWavAudioDirectory,
        MRURenameImage,
        MRURenameWavAudio,
        MRURenameOther,
        MRURenameReplaceInName,
        MRUEditConvertResizeImage,
        MRUEditCategoriesString,
    }

    public enum InputBoxHistory
    {
        None,
        OpenImageDirectory,
        OpenImageKeepExifDirectory,
        OpenAudioDirectory,
        OpenWavAudioDirectory,
        RenameImage,
        RenameWavAudio,
        RenameOther,
        RenameReplaceInName,
        EditConvertResizeImage,
        EditCategoriesString,
    }

    public sealed class Configs
    {
        static Configs _instance;
        string _path;
        Dictionary<ConfigKey, string> _persisted = new Dictionary<ConfigKey, string>();

        internal Configs(string path)
        {
            this._path = path;
        }

        // in-memory non-persisted settings
        public string Directory { get; private set; }

        public bool SupressDialogs { get; set; }

        public static Configs Current
        {
            get
            {
                return _instance;
            }
        }

        public static ConfigKey ConfigsPersistedKeysFromString(string s)
        {
            ConfigKey e = ConfigKey.None;
            return Enum.TryParse(s, out e) ? e : ConfigKey.None;
        }

        public static void Init(string path)
        {
            _instance = new Configs(path);
            _instance.Directory = Path.GetDirectoryName(path);
        }

        public void LoadPersisted()
        {
            if (!File.Exists(this._path))
                return;

            var lines = File.ReadAllLines(this._path);
            for (int i = 0; i < lines.Length; i++)
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

                ConfigKey key = ConfigsPersistedKeysFromString(split[0]);
                if (key == ConfigKey.None)
                {
                    SimpleLog.Current.WriteWarning("unrecognized config key on line " + i + ", might occur if using config from future version.");
                    continue;
                }

                this._persisted[key] = split[1];
            }
        }

        void SavePersisted()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in from key in this._persisted.Keys orderby key select key)
            {
                var value = this._persisted[key];
                if (value != null && value != "")
                {
                    if (value.Contains("\r") || value.Contains("\n"))
                        throw new CoordinatePicturesException("config values cannot contain newline, for key " + key);
                    sb.AppendLine(key.ToString() + "=" + value);
                }
            }

            File.WriteAllText(this._path, sb.ToString());
        }

        public void Set(ConfigKey key, string s)
        {
            this._persisted[key] = s;
            SavePersisted();
        }

        public void SetBool(ConfigKey key, bool b)
        {
            Set(key, b ? "true" : "");
        }

        public string Get(ConfigKey key)
        {
            string s;
            return this._persisted.TryGetValue(key, out s) ? s : "";
        }

        public bool GetBool(ConfigKey key)
        {
            return !string.IsNullOrEmpty(Get(key));
        }
    }
}

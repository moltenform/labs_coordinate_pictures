using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace labs_coordinate_pictures
{
    public enum ConfigsMemoryKeys
    {
        None,
        SupressDialogs
    }
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
        FilepathMozjpeg,
        FilepathWebp
    }
    public class ClassConfigs
    {
        public static ConfigsMemoryKeys ConfigsMemoryKeysFromString(string s)
        {
            ConfigsMemoryKeys e = ConfigsMemoryKeys.None;
            return Enum.TryParse(s, out e) ? e : ConfigsMemoryKeys.None;
        }
        public static ConfigsPersistedKeys ConfigsPersistedKeysFromString(string s)
        {
            ConfigsPersistedKeys e = ConfigsPersistedKeys.None;
            return Enum.TryParse(s, out e) ? e : ConfigsPersistedKeys.None;
        }

        private static readonly ClassConfigs currentconfig = new ClassConfigs("./config.ini");
        internal ClassConfigs(string path) { _path = path; }
        Dictionary<ConfigsPersistedKeys, string> _persisted = new Dictionary<ConfigsPersistedKeys, string>();
        Dictionary<ConfigsMemoryKeys, string> _memory = new Dictionary<ConfigsMemoryKeys, string>();
        string _path;
        public static ClassConfigs Current
        {
            get
            {
                return currentconfig;
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
        public void SetTemporary(ConfigsMemoryKeys key, string s)
        {
            _memory[key] = s;
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
        public string GetTemporary(ConfigsMemoryKeys key)
        {
            string s;
            return _memory.TryGetValue(key, out s) ? s : "";
        }
        public bool GetBool(ConfigsPersistedKeys key)
        {
            return !string.IsNullOrEmpty(Get(key));
        }
    }
}

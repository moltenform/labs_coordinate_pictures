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
    public sealed class ModeMarkMp3Other
    {
        public override bool SupportsCompletionAction()
        {
            if (Configs.Current.GetBool(ConfigKey.EnablePersonalFeatures))
            {
                string explanation = @"Description";

                if (Utils.AskToConfirm(explanation))
                {
                    var tagpattern = "%artist% - %title% (%albumartist%) [%comment%]";
                    if (Utils.AskToConfirm("Tags are often set with the pattern '" +
                        tagpattern + "'. Put this pattern in the clipboard?"))
                    {
                        Clipboard.SetText(tagpattern);
                    }

                    return true;
                }
            }

            return false;
        }

        static Tuple<int, string> videoIdFromFilename(string s)
        {
            s = Path.GetFileName(s);
            var regex = new Regex(@"\[([^ 	]{11})\]");
            foreach (Match match in regex.Matches(s))
            {
                return new Tuple<int, string>(match.Groups[1].Index, match.Groups[1].ToString());
            }

            return null;
        }

        static void sendToRawMaterial(string path, bool copy = false)
        {
            // "delete" it by sending it to raw material
            var material = Configs.Current.Get(ConfigKey.FilepathSortMusicKeepAsMaterialDir);
            if (string.IsNullOrEmpty(material) || !Directory.Exists(material))
            {
                material = InputBoxForm.GetStrInput(
                    "Choose a directory where deleted music will be sent as raw material:",
                    Configs.Current.Get(ConfigKey.FilepathSortMusicKeepAsMaterialDir),
                    mustBeDirectory: true);

                if (string.IsNullOrEmpty(material) || !Directory.Exists(material))
                {
                    throw new CoordinatePicturesException("Directory not found.");
                }
                else
                {
                    Configs.Current.Set(ConfigKey.FilepathSortMusicKeepAsMaterialDir, material);
                }
            }

            var dest = Path.Combine(material, Path.GetFileName(path) + Utils.GetRandomDigits());
            if (copy)
            {
                SimpleLog.Current.WriteLog("Send to raw-material. File.Copy " + path + " to " + dest);
                File.Copy(path, dest);
            }
            else
            {
                SimpleLog.Current.WriteLog("Send to raw-material. File.Move " + path + " to " + dest);
                File.Move(path, dest);
            }
        }

        public override void OnCompletionAction(string baseDirectory,
            string path, string pathWithoutCategory, Tuple<string, string, string> category)
        {
            if (!pathWithoutCategory.EndsWith(".m4a", StringComparison.InvariantCulture) &&
                !pathWithoutCategory.EndsWith(".mp4", StringComparison.InvariantCulture) &&
                !pathWithoutCategory.EndsWith(".mp3", StringComparison.InvariantCulture) &&
                !pathWithoutCategory.EndsWith(".ogg", StringComparison.InvariantCulture) &&
                !pathWithoutCategory.EndsWith(".wma", StringComparison.InvariantCulture) &&
                !pathWithoutCategory.EndsWith(".aac", StringComparison.InvariantCulture))
            {
                return;
            }

            int ncategory = category.Item3 == "flac" ? 320 : int.Parse(category.Item3);
            if (ncategory < 20)
            {
                sendToRawMaterial(path);
            }
            else
            {
                var videolink = videoIdFromFilename(pathWithoutCategory);
                string suffix = " (" + ncategory.ToString().Substring(0, 2) + ")";
                if (ncategory < 50)
                {
                    if (videolink == null)
                    {
                        Utils.MessageErr("markas 24 implies a link, but file " + path +
                            " does not have a video id in the form [0YfIHieP30g].");
                        return;
                    }

                    sendToRawMaterial(path, copy: true);
                    suffix = " (vv)";
                }
                else if (ncategory > 250)
                {
                    suffix = " (^)";
                }

                // look for a video link so that we add the suffix to the right place.
                var shortname = Path.GetFileName(pathWithoutCategory);
                var insertSuffixAt = shortname.Length - Path.GetExtension(shortname).Length;
                if (videolink != null)
                {
                    insertSuffixAt = videolink.Item1 - " [".Length;
                }

                var newname = shortname.Insert(insertSuffixAt, suffix);
                var dest = Path.Combine(Path.GetDirectoryName(pathWithoutCategory), newname);
                SimpleLog.Current.WriteLog("File.Move " + path + " to " + dest);
                File.Move(path, dest);
            }
        }
    }
}
﻿// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public static class CreateFileCombinations
    {
        enum ModifiedTimes {
            SmTime,
            MTime
        }

        enum Content {
            SmText,
            AltText,
            AddText
        }

        enum Filename {
            SmName,
            MName
        }

        enum ExtraCopies {
            None,
            OneOnLeft,
            OneOnRight
        }

        public static int Go(string dirLeft, string dirRight)
        {
            int filesCreated = 0;
            var baseTime = DateTime.Now;
            foreach (ModifiedTimes m in Enum.GetValues(typeof(ModifiedTimes)))
            {
                foreach (Content c in Enum.GetValues(typeof(Content)))
                {
                    foreach (Filename f in Enum.GetValues(typeof(Filename)))
                    {
                        foreach (ExtraCopies copies in Enum.GetValues(typeof(ExtraCopies)))
                        {
                            filesCreated += Go(dirLeft, dirRight, baseTime, m, c, f, copies);
                        }
                    }
                }
            }

            return filesCreated;
        }

        public static int CountPossibleModifiedTimes()
        {
            return Enum.GetValues(typeof(ModifiedTimes)).Length;
        }

        public static int CountPossibleContents()
        {
            return Enum.GetValues(typeof(Content)).Length;
        }

        public static int CountPossibleFilenames()
        {
            return Enum.GetValues(typeof(Filename)).Length;
        }

        static int Go(string dirLeft, string dirRight, DateTime baseTime,
            ModifiedTimes m, Content c, Filename f, ExtraCopies copies)
        {
            var baseName = m.ToString() + c.ToString() + f.ToString() + copies.ToString();
            var fileLeft = Path.Combine(
                dirLeft, baseName + ".a");
            var fileRight = Path.Combine(
                dirRight, baseName + (f == Filename.MName ? ".z" : ".a"));

            WriteFiles(fileLeft, fileRight, baseTime, m, c);
            switch (copies)
            {
                case ExtraCopies.OneOnLeft:
                    File.Copy(fileLeft, fileLeft + "_1");
                    break;
                case ExtraCopies.OneOnRight:
                    File.Copy(fileRight, fileRight + "_1");
                    break;
            }

            return copies == ExtraCopies.None ? 2 : 3;
        }

        static void WriteFiles(string fileLeft, string fileRight,
            DateTime baseTime, ModifiedTimes m, Content c)
        {
            var addTime = (m == ModifiedTimes.MTime) ? 1 : 0;
            var contentsLeft = fileLeft;
            var contentsRight = contentsLeft;
            if (c == Content.AltText)
            {
                // replace first char with *
                contentsRight = "*" + contentsRight.Substring(1);
            }
            else if (c == Content.AddText)
            {
                contentsRight += "***";
            }

            TestUtil.IsTrue(!File.Exists(fileLeft) && !File.Exists(fileRight));
            File.WriteAllText(fileLeft, contentsLeft);
            File.SetLastWriteTimeUtc(fileLeft, baseTime);
            File.WriteAllText(fileRight, contentsRight);
            File.SetLastWriteTimeUtc(fileRight, baseTime.AddHours(addTime));
        }
    }

    public static class CoordinateFilesTests
    {
        static void TestMethod_ValidateGoodFilesSettings()
        {
            var dirFirst = TestUtil.GetTestSubDirectory("first");
            var dirSecond = TestUtil.GetTestSubDirectory("second");
            var settings = FormSortFiles.FillFromUI(SortFilesAction.SearchDifferences, "", "",
                dirFirst, dirSecond, true, true, false, false);

            TestUtil.IsEq(true, settings.AllowFiletimesDifferForFAT);
            TestUtil.IsEq(true, settings.AllowFiletimesDifferForDST);
            TestUtil.IsEq(dirSecond, settings.RightDirectory);
            TestUtil.IsTrue(Directory.Exists(Path.GetDirectoryName(settings.LogFile)));
            TestUtil.IsEq(false, settings.Mirror);
            TestUtil.IsEq(false, settings.PreviewOnly);
            TestUtil.IsEq(false, settings.SearchDuplicatesCanUseFiletimes);
            TestUtil.IsStringArrayEq(null, settings.SkipDirectories);
            TestUtil.IsStringArrayEq(null, settings.SkipFiles);
            TestUtil.IsEq(dirFirst, settings.LeftDirectory);

            settings = FormSortFiles.FillFromUI(
                SortFilesAction.SearchDifferences, "a", "a\nb b\n\nc\n\n ",
                dirSecond, dirFirst, false, false, true, true);

            TestUtil.IsEq(false, settings.AllowFiletimesDifferForFAT);
            TestUtil.IsEq(false, settings.AllowFiletimesDifferForDST);
            TestUtil.IsEq(dirFirst, settings.RightDirectory);
            TestUtil.IsTrue(Directory.Exists(Path.GetDirectoryName(settings.LogFile)));
            TestUtil.IsEq(true, settings.Mirror);
            TestUtil.IsEq(true, settings.PreviewOnly);
            TestUtil.IsEq(false, settings.SearchDuplicatesCanUseFiletimes);
            TestUtil.IsStringArrayEq("a", settings.SkipDirectories);
            TestUtil.IsStringArrayEq("a|b b|c", settings.SkipFiles);
            TestUtil.IsEq(dirSecond, settings.LeftDirectory);

            settings = FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicatesInOneDir, "", "",
                dirFirst, dirSecond, false, false, true, true);

            TestUtil.IsEq(dirFirst, settings.LeftDirectory);
            TestUtil.IsEq(dirFirst, settings.RightDirectory);
            TestUtil.IsEq(false, settings.SearchDuplicatesCanUseFiletimes);

            settings = FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, dirSecond, true, true, false, false);
            TestUtil.IsEq(true, settings.AllowFiletimesDifferForFAT);
            TestUtil.IsEq(true, settings.AllowFiletimesDifferForDST);
            TestUtil.IsEq(true, settings.SearchDuplicatesCanUseFiletimes);
        }

        static void TestMethod_RejectBadFilesSettings()
        {
            var dirFirst = TestUtil.GetTestSubDirectory("first");
            var dirSecond = TestUtil.GetTestSubDirectory("second");

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                Path.Combine(dirFirst, "notexist"), dirSecond, true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, Path.Combine(dirSecond, "notexist"), true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst + Utils.Sep, dirSecond, true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, dirSecond + Utils.Sep, true, true, true, true));

            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, dirFirst, true, true, true, true));

            Directory.CreateDirectory(Path.Combine(dirFirst, "sub"));
            TestUtil.IsEq(null, FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicates, "", "",
                dirFirst, Path.Combine(dirFirst, "sub"), true, true, true, true));

            // valid for dest to be empty if action is FindDupeFilesInOneDir
            TestUtil.IsTrue(
                FormSortFiles.FillFromUI(SortFilesAction.SearchDuplicatesInOneDir, "", "",
                dirFirst, "", true, false, true, true) != null);
        }

        static void TestMethod_AreTimesEqual()
        {
            var time = DateTime.Now;
            var timePlus3s = time.AddSeconds(3);
            var timePlus1hr = time.AddHours(1);

            // strict compare
            var settings = new SortFilesSettings();
            settings.AllowFiletimesDifferForDST = false;
            settings.AllowFiletimesDifferForFAT = false;
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(time, time, settings));
            TestUtil.IsEq(false,
                SortFilesSearchDifferences.AreTimesEqual(time, timePlus3s, settings));
            TestUtil.IsEq(false,
                SortFilesSearchDifferences.AreTimesEqual(timePlus3s, time, settings));
            TestUtil.IsEq(false,
                SortFilesSearchDifferences.AreTimesEqual(time, timePlus1hr, settings));
            TestUtil.IsEq(false,
                SortFilesSearchDifferences.AreTimesEqual(timePlus1hr, time, settings));

            // allow DST
            settings.AllowFiletimesDifferForDST = true;
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(time, time, settings));
            TestUtil.IsEq(false,
                SortFilesSearchDifferences.AreTimesEqual(time, timePlus3s, settings));
            TestUtil.IsEq(false,
                SortFilesSearchDifferences.AreTimesEqual(timePlus3s, time, settings));
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(time, timePlus1hr, settings));
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(timePlus1hr, time, settings));

            // allow close
            settings.AllowFiletimesDifferForFAT = true;
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(time, time, settings));
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(time, timePlus3s, settings));
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(timePlus3s, time, settings));
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(time, timePlus1hr, settings));
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(timePlus1hr, time, settings));

            // disallow DST
            settings.AllowFiletimesDifferForDST = false;
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(time, time, settings));
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(time, timePlus3s, settings));
            TestUtil.IsEq(true,
                SortFilesSearchDifferences.AreTimesEqual(timePlus3s, time, settings));
            TestUtil.IsEq(false,
                SortFilesSearchDifferences.AreTimesEqual(time, timePlus1hr, settings));
            TestUtil.IsEq(false,
                SortFilesSearchDifferences.AreTimesEqual(timePlus1hr, time, settings));
        }

        static void TestMethod_MapFilesizesToFilenames()
        {
            var dirTest = TestUtil.GetTestSubDirectory("testMapFilesizesToFilenames");
            File.WriteAllText(Path.Combine(dirTest, "a.txt"), "abcd");
            File.WriteAllText(Path.Combine(dirTest, "b.txt"), "abcde");
            File.WriteAllText(Path.Combine(dirTest, "c.txt"), "1234");

            // adjust the lmt of c.txt
            File.SetLastWriteTimeUtc(Path.Combine(dirTest, "c.txt"), DateTime.Now.AddDays(1));

            var map = SortFilesSearchDuplicates.MapFilesizesToFilenames(dirTest,
                new DirectoryInfo(dirTest).EnumerateFiles("*"));
            var mapSorted = (from item in map[4] orderby item.Filename select item).ToArray();
            TestUtil.IsEq(2, map.Count);
            TestUtil.IsEq(2, map[4].Count);
            TestUtil.IsEq(1, map[5].Count);

            // test that FileInfoForComparison was set correctly
            TestUtil.IsEq(Utils.Sep + "a.txt", mapSorted[0].Filename);
            TestUtil.IsEq(null, mapSorted[0].ContentHash);
            TestUtil.IsEq(4L, mapSorted[0].FileSize);
            TestUtil.IsEq(File.GetLastWriteTimeUtc(Path.Combine(dirTest, "a.txt")),
                mapSorted[0].LastModifiedTime);

            TestUtil.IsEq(Utils.Sep + "b.txt", map[5][0].Filename);
            TestUtil.IsEq(null, map[5][0].ContentHash);
            TestUtil.IsEq(5L, map[5][0].FileSize);
            TestUtil.IsEq(File.GetLastWriteTimeUtc(Path.Combine(dirTest, "b.txt")),
                map[5][0].LastModifiedTime);

            TestUtil.IsEq(Utils.Sep + "c.txt", mapSorted[1].Filename);
            TestUtil.IsEq(null, mapSorted[1].ContentHash);
            TestUtil.IsEq(4L, mapSorted[1].FileSize);
            TestUtil.IsEq(File.GetLastWriteTimeUtc(Path.Combine(dirTest, "c.txt")),
                mapSorted[1].LastModifiedTime);
        }

        static string ResultsToString(IEnumerable<FileComparisonResult> list)
        {
            var lines = from item in list
                        orderby item.FileInfoLeft?.Filename,
                        item.FileInfoRight?.Filename
                        select
                        (item.FileInfoLeft?.Filename ?? "") + "|" +
                        (item.FileInfoRight?.Filename ?? "") + "|" +
                        item.Type.ToString();
            return string.Join(Utils.NL, lines);
        }

        static void CompareResultsToString(List<FileComparisonResult> list, string expected)
        {
            var received = ResultsToString(list).Replace(Utils.Sep, "").Replace(
                "_File", "").Replace("_Only", "").Replace("\r\n", "\n");
            expected = expected.Replace("\r\n", "\n");
            TestUtil.IsEq(expected, received);
        }

        static int CountFilenames(string s)
        {
            // valid if each filename has exactly one file extension.
            return (from c in s where c == '.' select c).Count();
        }

        static void TestMethod_TestSortFilesOperations()
        {
            // run the methods on actual files. first create combinations of modified/not modified.
            var settings = new SortFilesSettings();
            settings.LeftDirectory = TestUtil.GetTestSubDirectory("left_fndmved", true);
            settings.RightDirectory = TestUtil.GetTestSubDirectory("right_fndmved", true);
            var filesCreated = CreateFileCombinations.Go(
                settings.LeftDirectory, settings.RightDirectory);
            TestUtil.IsEq(
                CreateFileCombinations.CountPossibleModifiedTimes() *
                CreateFileCombinations.CountPossibleContents() *
                CreateFileCombinations.CountPossibleFilenames() *
                ((1 * 2) + (3 * 2)), // ExtraCopies.None -> 2 files, the rest -> 3 files
                filesCreated);

            // search for duplicates in one dir, only ones it will find are 'extra copy on left.'
            var results = SortFilesSearchDuplicatesInOneDir.Go(settings);
            TestUtil.IsEq(
                CreateFileCombinations.CountPossibleModifiedTimes() *
                CreateFileCombinations.CountPossibleContents() *
                CreateFileCombinations.CountPossibleFilenames(), results.Count);

            // verify sort order. for each pair, the left side should sort first alphabetically
            var expectedDuplicates =
@"MTimeAddTextMNameOneOnLeft.a|MTimeAddTextMNameOneOnLeft.a_1|Same_Contents
MTimeAddTextSmNameOneOnLeft.a|MTimeAddTextSmNameOneOnLeft.a_1|Same_Contents
MTimeAltTextMNameOneOnLeft.a|MTimeAltTextMNameOneOnLeft.a_1|Same_Contents
MTimeAltTextSmNameOneOnLeft.a|MTimeAltTextSmNameOneOnLeft.a_1|Same_Contents
MTimeSmTextMNameOneOnLeft.a|MTimeSmTextMNameOneOnLeft.a_1|Same_Contents
MTimeSmTextSmNameOneOnLeft.a|MTimeSmTextSmNameOneOnLeft.a_1|Same_Contents
SmTimeAddTextMNameOneOnLeft.a|SmTimeAddTextMNameOneOnLeft.a_1|Same_Contents
SmTimeAddTextSmNameOneOnLeft.a|SmTimeAddTextSmNameOneOnLeft.a_1|Same_Contents
SmTimeAltTextMNameOneOnLeft.a|SmTimeAltTextMNameOneOnLeft.a_1|Same_Contents
SmTimeAltTextSmNameOneOnLeft.a|SmTimeAltTextSmNameOneOnLeft.a_1|Same_Contents
SmTimeSmTextMNameOneOnLeft.a|SmTimeSmTextMNameOneOnLeft.a_1|Same_Contents
SmTimeSmTextSmNameOneOnLeft.a|SmTimeSmTextSmNameOneOnLeft.a_1|Same_Contents";
            CompareResultsToString(results, expectedDuplicates);

            // search for duplicates across directories
            // should find all files on the right marked 'SmText'.
            results = SortFilesSearchDuplicates.Go(settings);
            var countExpectedDuplicates =
                (from filename in Directory.EnumerateFiles(settings.RightDirectory)
                 where filename.Contains("SmText")
                 select filename).Count();
            TestUtil.IsEq(countExpectedDuplicates, results.Count);

            // verify sort order
            expectedDuplicates =
@"MTimeSmTextMNameNone.a|MTimeSmTextMNameNone.z|Same_Contents
MTimeSmTextMNameOneOnLeft.a|MTimeSmTextMNameOneOnLeft.z|Same_Contents
MTimeSmTextMNameOneOnRight.a|MTimeSmTextMNameOneOnRight.z|Same_Contents
MTimeSmTextMNameOneOnRight.a|MTimeSmTextMNameOneOnRight.z_1|Same_Contents
MTimeSmTextSmNameNone.a|MTimeSmTextSmNameNone.a|Same_Contents
MTimeSmTextSmNameOneOnLeft.a|MTimeSmTextSmNameOneOnLeft.a|Same_Contents
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a|Same_Contents
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a_1|Same_Contents
SmTimeSmTextMNameNone.a|SmTimeSmTextMNameNone.z|Same_Contents
SmTimeSmTextMNameOneOnLeft.a|SmTimeSmTextMNameOneOnLeft.z|Same_Contents
SmTimeSmTextMNameOneOnRight.a|SmTimeSmTextMNameOneOnRight.z|Same_Contents
SmTimeSmTextMNameOneOnRight.a|SmTimeSmTextMNameOneOnRight.z_1|Same_Contents
SmTimeSmTextSmNameNone.a|SmTimeSmTextSmNameNone.a|Same_Contents
SmTimeSmTextSmNameOneOnLeft.a|SmTimeSmTextSmNameOneOnLeft.a|Same_Contents
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a|Same_Contents
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a_1|Same_Contents";
            CompareResultsToString(results, expectedDuplicates);

            // search for duplicates across directories, but uses lmt as a shortcut (less thorough)
            // it will now think that the SmTimeAltText ones are equal because,
            // when it sees the lmt are the same, it treats them as the same and doesn't check hash
            settings.SearchDuplicatesCanUseFiletimes = true;
            results = SortFilesSearchDuplicates.Go(settings);
            settings.SearchDuplicatesCanUseFiletimes = false;

            expectedDuplicates =
@"MTimeSmTextMNameNone.a|MTimeSmTextMNameNone.z|Same_Contents
MTimeSmTextMNameOneOnLeft.a|MTimeSmTextMNameOneOnLeft.z|Same_Contents
MTimeSmTextMNameOneOnRight.a|MTimeSmTextMNameOneOnRight.z|Same_Contents
MTimeSmTextMNameOneOnRight.a|MTimeSmTextMNameOneOnRight.z_1|Same_Contents
MTimeSmTextSmNameNone.a|MTimeSmTextSmNameNone.a|Same_Contents
MTimeSmTextSmNameOneOnLeft.a|MTimeSmTextSmNameOneOnLeft.a|Same_Contents
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a|Same_Contents
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a_1|Same_Contents
SmTimeAltTextSmNameNone.a|SmTimeAltTextSmNameNone.a|Same_Contents
SmTimeAltTextSmNameOneOnLeft.a|SmTimeAltTextSmNameOneOnLeft.a|Same_Contents
SmTimeAltTextSmNameOneOnRight.a|SmTimeAltTextSmNameOneOnRight.a|Same_Contents
SmTimeSmTextMNameNone.a|SmTimeSmTextMNameNone.z|Same_Contents
SmTimeSmTextMNameOneOnLeft.a|SmTimeSmTextMNameOneOnLeft.z|Same_Contents
SmTimeSmTextMNameOneOnRight.a|SmTimeSmTextMNameOneOnRight.z|Same_Contents
SmTimeSmTextMNameOneOnRight.a|SmTimeSmTextMNameOneOnRight.z_1|Same_Contents
SmTimeSmTextSmNameNone.a|SmTimeSmTextSmNameNone.a|Same_Contents
SmTimeSmTextSmNameOneOnLeft.a|SmTimeSmTextSmNameOneOnLeft.a|Same_Contents
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a|Same_Contents
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a_1|Same_Contents";
            CompareResultsToString(results, expectedDuplicates);

            // search for differences in similar directories.
            results = SortFilesSearchDifferences.Go(settings);
            var expectedDifferences =
@"|MTimeAddTextMNameNone.z|Right
|MTimeAddTextMNameOneOnLeft.z|Right
|MTimeAddTextMNameOneOnRight.z|Right
|MTimeAddTextMNameOneOnRight.z_1|Right
|MTimeAddTextSmNameOneOnRight.a_1|Right
|MTimeAltTextMNameNone.z|Right
|MTimeAltTextMNameOneOnLeft.z|Right
|MTimeAltTextMNameOneOnRight.z|Right
|MTimeAltTextMNameOneOnRight.z_1|Right
|MTimeAltTextSmNameOneOnRight.a_1|Right
|MTimeSmTextMNameNone.z|Right
|MTimeSmTextMNameOneOnLeft.z|Right
|MTimeSmTextMNameOneOnRight.z|Right
|MTimeSmTextMNameOneOnRight.z_1|Right
|MTimeSmTextSmNameOneOnRight.a_1|Right
|SmTimeAddTextMNameNone.z|Right
|SmTimeAddTextMNameOneOnLeft.z|Right
|SmTimeAddTextMNameOneOnRight.z|Right
|SmTimeAddTextMNameOneOnRight.z_1|Right
|SmTimeAddTextSmNameOneOnRight.a_1|Right
|SmTimeAltTextMNameNone.z|Right
|SmTimeAltTextMNameOneOnLeft.z|Right
|SmTimeAltTextMNameOneOnRight.z|Right
|SmTimeAltTextMNameOneOnRight.z_1|Right
|SmTimeAltTextSmNameOneOnRight.a_1|Right
|SmTimeSmTextMNameNone.z|Right
|SmTimeSmTextMNameOneOnLeft.z|Right
|SmTimeSmTextMNameOneOnRight.z|Right
|SmTimeSmTextMNameOneOnRight.z_1|Right
|SmTimeSmTextSmNameOneOnRight.a_1|Right
MTimeAddTextMNameNone.a||Left
MTimeAddTextMNameOneOnLeft.a||Left
MTimeAddTextMNameOneOnLeft.a_1||Left
MTimeAddTextMNameOneOnRight.a||Left
MTimeAddTextSmNameNone.a|MTimeAddTextSmNameNone.a|Changed
MTimeAddTextSmNameOneOnLeft.a|MTimeAddTextSmNameOneOnLeft.a|Changed
MTimeAddTextSmNameOneOnLeft.a_1||Left
MTimeAddTextSmNameOneOnRight.a|MTimeAddTextSmNameOneOnRight.a|Changed
MTimeAltTextMNameNone.a||Left
MTimeAltTextMNameOneOnLeft.a||Left
MTimeAltTextMNameOneOnLeft.a_1||Left
MTimeAltTextMNameOneOnRight.a||Left
MTimeAltTextSmNameNone.a|MTimeAltTextSmNameNone.a|Changed
MTimeAltTextSmNameOneOnLeft.a|MTimeAltTextSmNameOneOnLeft.a|Changed
MTimeAltTextSmNameOneOnLeft.a_1||Left
MTimeAltTextSmNameOneOnRight.a|MTimeAltTextSmNameOneOnRight.a|Changed
MTimeSmTextMNameNone.a||Left
MTimeSmTextMNameOneOnLeft.a||Left
MTimeSmTextMNameOneOnLeft.a_1||Left
MTimeSmTextMNameOneOnRight.a||Left
MTimeSmTextSmNameNone.a|MTimeSmTextSmNameNone.a|Changed
MTimeSmTextSmNameOneOnLeft.a|MTimeSmTextSmNameOneOnLeft.a|Changed
MTimeSmTextSmNameOneOnLeft.a_1||Left
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a|Changed
SmTimeAddTextMNameNone.a||Left
SmTimeAddTextMNameOneOnLeft.a||Left
SmTimeAddTextMNameOneOnLeft.a_1||Left
SmTimeAddTextMNameOneOnRight.a||Left
SmTimeAddTextSmNameNone.a|SmTimeAddTextSmNameNone.a|Changed
SmTimeAddTextSmNameOneOnLeft.a|SmTimeAddTextSmNameOneOnLeft.a|Changed
SmTimeAddTextSmNameOneOnLeft.a_1||Left
SmTimeAddTextSmNameOneOnRight.a|SmTimeAddTextSmNameOneOnRight.a|Changed
SmTimeAltTextMNameNone.a||Left
SmTimeAltTextMNameOneOnLeft.a||Left
SmTimeAltTextMNameOneOnLeft.a_1||Left
SmTimeAltTextMNameOneOnRight.a||Left
SmTimeAltTextSmNameOneOnLeft.a_1||Left
SmTimeSmTextMNameNone.a||Left
SmTimeSmTextMNameOneOnLeft.a||Left
SmTimeSmTextMNameOneOnLeft.a_1||Left
SmTimeSmTextMNameOneOnRight.a||Left
SmTimeSmTextSmNameOneOnLeft.a_1||Left";
            CompareResultsToString(results, expectedDifferences);

            // account for all 96 files.
            // (SortFilesSearchDifferences doesn't check hashes, so although it knows
            // AddText are different because filesize changes,
            // it won't detect AltText unless filesize or lmt are also different.)
            var expectedSame =
@"SmTimeAltTextSmNameNone.a|SmTimeAltTextSmNameNone.a
SmTimeAltTextSmNameOneOnLeft.a|SmTimeAltTextSmNameOneOnLeft.a
SmTimeAltTextSmNameOneOnRight.a|SmTimeAltTextSmNameOneOnRight.a
SmTimeSmTextSmNameNone.a|SmTimeSmTextSmNameNone.a
SmTimeSmTextSmNameOneOnLeft.a|SmTimeSmTextSmNameOneOnLeft.a
SmTimeSmTextSmNameOneOnRight.a|SmTimeSmTextSmNameOneOnRight.a";
            TestUtil.IsEq(filesCreated,
                CountFilenames(expectedDifferences) + CountFilenames(expectedSame));

            // search for identical files with different write times
            // will find all with MTimeSmText
            var found = SortFilesSearchDuplicates.SearchForIdenticalFilesWithDifferentWriteTimes(
                settings.LeftDirectory, settings.RightDirectory, results);
            var expectedIdenticalContents =
@"MTimeSmTextSmNameNone.a|MTimeSmTextSmNameNone.a|Changed
MTimeSmTextSmNameOneOnLeft.a|MTimeSmTextSmNameOneOnLeft.a|Changed
MTimeSmTextSmNameOneOnRight.a|MTimeSmTextSmNameOneOnRight.a|Changed";
            CompareResultsToString(found, expectedIdenticalContents);
        }

        static void SelectFirstItems(List<FileComparisonResult> selectedItems,
            ListView listView, int itemsToSelect)
        {
            selectedItems.Clear();
            for (int i = 0; i < itemsToSelect; i++)
            {
                selectedItems.Add((FileComparisonResult)listView.Items[i]);
            }
        }

        static void TestMethod_TestSortFilesListOperations()
        {
            // create settings object
            var settings = new SortFilesSettings();
            var left = TestUtil.GetTestSubDirectory("left_sortfileslist");
            var right = TestUtil.GetTestSubDirectory("right_sortfileslist");
            settings.LeftDirectory = left;
            settings.RightDirectory = right;

            // first, set up test files
            File.WriteAllText(left + Utils.Sep + "onlyleft.txt", "onlyl");
            File.WriteAllText(left + Utils.Sep + "changed1.txt", "a");
            File.WriteAllText(left + Utils.Sep + "changed2.txt", "123");
            File.WriteAllText(left + Utils.Sep + "same.txt", "s");
            File.WriteAllText(right + Utils.Sep + "onlyright.txt", "onlyr");
            File.WriteAllText(right + Utils.Sep + "changed1.txt", "abc");
            File.WriteAllText(right + Utils.Sep + "changed2.txt", "124");
            File.WriteAllText(right + Utils.Sep + "same.txt", "s");
            Action checkFileContents = () =>
            {
                TestUtil.IsEq("onlyl", File.ReadAllText(Path.Combine(left, "onlyleft.txt")));
                TestUtil.IsEq("a", File.ReadAllText(Path.Combine(left, "changed1.txt")));
                TestUtil.IsEq("123", File.ReadAllText(Path.Combine(left, "changed2.txt")));
                TestUtil.IsEq("onlyr", File.ReadAllText(Path.Combine(right, "onlyright.txt")));
                TestUtil.IsEq("abc", File.ReadAllText(Path.Combine(right, "changed1.txt")));
                TestUtil.IsEq("124", File.ReadAllText(Path.Combine(right, "changed2.txt")));
            };

            // tweak last write times to ensure files on right look different
            File.SetLastWriteTime(right + Utils.Sep + "changed1.txt",
                File.GetLastWriteTime(right + Utils.Sep + "changed1.txt").AddDays(1));
            File.SetLastWriteTime(right + Utils.Sep + "changed2.txt",
                File.GetLastWriteTime(right + Utils.Sep + "changed2.txt").AddDays(1));
            File.SetLastWriteTime(right + Utils.Sep + "same.txt",
                File.GetLastWriteTime(left + Utils.Sep + "same.txt"));

            // create form and run searchdifferences
            var form = new FormSortFilesList(
                SortFilesAction.SearchDifferences, settings, "", allActionsSynchronous: true);
            ListView listView;
            form.GetTestHooks(out listView, out List<FileComparisonResult> mockSelection,
                out UndoStack<List<IUndoableFileOp>> undoStack);
            form.RunSortFilesAction();

            // simulate column-header click to sort by path
            form.listView_ColumnClick(null, new ColumnClickEventArgs(2));

            // verify listview contents
            var items = listView.Items.Cast<FileComparisonResult>().ToArray();
            TestUtil.IsEq(4, items.Length);
            TestUtil.IsEq(Utils.Sep + "changed1.txt", items[0].FileInfoLeft.Filename);
            TestUtil.IsEq(null, items[0].FileInfoLeft.ContentHash);
            TestUtil.IsEq(1L, items[0].FileInfoLeft.FileSize);
            TestUtil.IsEq(Utils.Sep + "changed1.txt", items[0].FileInfoRight.Filename);
            TestUtil.IsEq(null, items[0].FileInfoRight.ContentHash);
            TestUtil.IsEq(3L, items[0].FileInfoRight.FileSize);
            TestUtil.IsEq(Utils.Sep + "changed2.txt", items[1].FileInfoLeft.Filename);
            TestUtil.IsEq(null, items[1].FileInfoLeft.ContentHash);
            TestUtil.IsEq(3L, items[1].FileInfoLeft.FileSize);
            TestUtil.IsEq(Utils.Sep + "changed2.txt", items[1].FileInfoRight.Filename);
            TestUtil.IsEq(null, items[1].FileInfoRight.ContentHash);
            TestUtil.IsEq(3L, items[1].FileInfoRight.FileSize);
            TestUtil.IsEq(Utils.Sep + "onlyleft.txt", items[2].FileInfoLeft.Filename);
            TestUtil.IsEq(null, items[2].FileInfoLeft.ContentHash);
            TestUtil.IsEq(5L, items[2].FileInfoLeft.FileSize);
            TestUtil.IsEq(null, items[2].FileInfoRight);
            TestUtil.IsEq(Utils.Sep + "onlyright.txt", items[3].FileInfoRight.Filename);
            TestUtil.IsEq(null, items[3].FileInfoRight.ContentHash);
            TestUtil.IsEq(5L, items[3].FileInfoRight.FileSize);
            TestUtil.IsEq(null, items[3].FileInfoLeft);

            // test CheckSelectedItemsSameType
            mockSelection.Clear();
            TestUtil.IsEq(false, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[0]);
            TestUtil.IsEq(true, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[1]);
            TestUtil.IsEq(true, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[2]);
            TestUtil.IsEq(false, form.CheckSelectedItemsSameType());
            mockSelection.Clear();
            TestUtil.IsEq(false, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[1]);
            TestUtil.IsEq(true, form.CheckSelectedItemsSameType());
            mockSelection.Add((FileComparisonResult)listView.Items[2]);
            TestUtil.IsEq(false, form.CheckSelectedItemsSameType());

            // delete all on left
            checkFileContents();
            SelectFirstItems(mockSelection, listView, items.Length);
            form.OnClickDeleteFile(left: true, needConfirm: false);

            // see if undo was set as expected
            var lastUndo = undoStack.PeekUndo();
            TestUtil.IsEq(3, lastUndo.Count);
            TestUtil.IsEq(Path.Combine(left, "changed1.txt"), lastUndo[0].Source);
            TestUtil.IsEq(Path.Combine(left, "changed2.txt"), lastUndo[1].Source);
            TestUtil.IsEq(Path.Combine(left, "onlyleft.txt"), lastUndo[2].Source);

            // test file presence
            TestUtil.IsTrue(!File.Exists(Path.Combine(left, "changed1.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(left, "changed2.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(left, "onlyleft.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(right, "changed1.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(right, "changed2.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(right, "onlyright.txt")));

            // run undo
            form.OnUndoClick(needConfirm: false);
            TestUtil.IsEq(null, undoStack.PeekUndo());
            checkFileContents();

            // delete all on right
            SelectFirstItems(mockSelection, listView, items.Length);
            form.OnClickDeleteFile(left: false, needConfirm: false);

            // see if undo was set as expected
            lastUndo = undoStack.PeekUndo();
            TestUtil.IsEq(3, lastUndo.Count);
            TestUtil.IsEq(Path.Combine(right, "changed1.txt"), lastUndo[0].Source);
            TestUtil.IsEq(Path.Combine(right, "changed2.txt"), lastUndo[1].Source);
            TestUtil.IsEq(Path.Combine(right, "onlyright.txt"), lastUndo[2].Source);

            // test file presence
            TestUtil.IsTrue(File.Exists(Path.Combine(left, "changed1.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(left, "changed2.txt")));
            TestUtil.IsTrue(File.Exists(Path.Combine(left, "onlyleft.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(right, "changed1.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(right, "changed2.txt")));
            TestUtil.IsTrue(!File.Exists(Path.Combine(right, "onlyright.txt")));

            // run undo
            form.OnUndoClick(needConfirm: false);
            TestUtil.IsEq(null, undoStack.PeekUndo());
            checkFileContents();

            // copy all left to right
            SelectFirstItems(mockSelection, listView, items.Length);
            form.OnClickCopyFile(left: true, needConfirm: false);

            // see if undo was set as expected
            lastUndo = undoStack.PeekUndo();
            TestUtil.IsEq(5, lastUndo.Count);
            TestUtil.IsTrue(lastUndo[0] is FileOpFileMove);
            TestUtil.IsEq(Path.Combine(right, "changed1.txt"), lastUndo[0].Source);
            TestUtil.IsTrue(lastUndo[1] is FileOpFileCopy);
            TestUtil.IsEq(Path.Combine(left, "changed1.txt"), lastUndo[1].Source);
            TestUtil.IsEq(Path.Combine(right, "changed1.txt"), lastUndo[1].Dest);
            TestUtil.IsTrue(lastUndo[2] is FileOpFileMove);
            TestUtil.IsEq(Path.Combine(right, "changed2.txt"), lastUndo[2].Source);
            TestUtil.IsTrue(lastUndo[3] is FileOpFileCopy);
            TestUtil.IsEq(Path.Combine(left, "changed2.txt"), lastUndo[3].Source);
            TestUtil.IsEq(Path.Combine(right, "changed2.txt"), lastUndo[3].Dest);
            TestUtil.IsTrue(lastUndo[4] is FileOpFileCopy);
            TestUtil.IsEq(Path.Combine(left, "onlyleft.txt"), lastUndo[4].Source);
            TestUtil.IsEq(Path.Combine(right, "onlyleft.txt"), lastUndo[4].Dest);

            // test file contents
            TestUtil.IsEq("onlyl", File.ReadAllText(Path.Combine(left, "onlyleft.txt")));
            TestUtil.IsEq("a", File.ReadAllText(Path.Combine(left, "changed1.txt")));
            TestUtil.IsEq("123", File.ReadAllText(Path.Combine(left, "changed2.txt")));
            TestUtil.IsEq("onlyl", File.ReadAllText(Path.Combine(right, "onlyleft.txt")));
            TestUtil.IsEq("onlyr", File.ReadAllText(Path.Combine(right, "onlyright.txt")));
            TestUtil.IsEq("a", File.ReadAllText(Path.Combine(right, "changed1.txt")));
            TestUtil.IsEq("123", File.ReadAllText(Path.Combine(right, "changed2.txt")));

            // run undo
            form.OnUndoClick(needConfirm: false);
            TestUtil.IsEq(null, undoStack.PeekUndo());
            TestUtil.IsTrue(!File.Exists(Path.Combine(right, "onlyleft.txt")));
            checkFileContents();

            // copy all right to left
            SelectFirstItems(mockSelection, listView, items.Length);
            form.OnClickCopyFile(left: false, needConfirm: false);

            // see if undo was set as expected
            lastUndo = undoStack.PeekUndo();
            TestUtil.IsEq(5, lastUndo.Count);
            TestUtil.IsTrue(lastUndo[0] is FileOpFileMove);
            TestUtil.IsEq(Path.Combine(left, "changed1.txt"), lastUndo[0].Source);
            TestUtil.IsTrue(lastUndo[1] is FileOpFileCopy);
            TestUtil.IsEq(Path.Combine(right, "changed1.txt"), lastUndo[1].Source);
            TestUtil.IsEq(Path.Combine(left, "changed1.txt"), lastUndo[1].Dest);
            TestUtil.IsTrue(lastUndo[2] is FileOpFileMove);
            TestUtil.IsEq(Path.Combine(left, "changed2.txt"), lastUndo[2].Source);
            TestUtil.IsTrue(lastUndo[3] is FileOpFileCopy);
            TestUtil.IsEq(Path.Combine(right, "changed2.txt"), lastUndo[3].Source);
            TestUtil.IsEq(Path.Combine(left, "changed2.txt"), lastUndo[3].Dest);
            TestUtil.IsTrue(lastUndo[4] is FileOpFileCopy);
            TestUtil.IsEq(Path.Combine(right, "onlyright.txt"), lastUndo[4].Source);
            TestUtil.IsEq(Path.Combine(left, "onlyright.txt"), lastUndo[4].Dest);

            // test file contents
            TestUtil.IsEq("onlyl", File.ReadAllText(Path.Combine(left, "onlyleft.txt")));
            TestUtil.IsEq("onlyr", File.ReadAllText(Path.Combine(left, "onlyright.txt")));
            TestUtil.IsEq("abc", File.ReadAllText(Path.Combine(left, "changed1.txt")));
            TestUtil.IsEq("124", File.ReadAllText(Path.Combine(left, "changed2.txt")));
            TestUtil.IsEq("onlyr", File.ReadAllText(Path.Combine(right, "onlyright.txt")));
            TestUtil.IsEq("abc", File.ReadAllText(Path.Combine(right, "changed1.txt")));
            TestUtil.IsEq("124", File.ReadAllText(Path.Combine(right, "changed2.txt")));

            // run undo
            form.OnUndoClick(needConfirm: false);
            TestUtil.IsEq(null, undoStack.PeekUndo());
            TestUtil.IsTrue(!File.Exists(Path.Combine(left, "onlyright.txt")));
            checkFileContents();
            form.Dispose();
        }

        static void TestMethod_TestSearchMovedFiles()
        {
            var settings = new SortFilesSettings();
            var left = TestUtil.GetTestSubDirectory("left_fndmved", true);
            var right = TestUtil.GetTestSubDirectory("right_fndmved", true);
            settings.LeftDirectory = left;
            settings.RightDirectory = right;

            // first, set up test files
            File.WriteAllText(left + Utils.Sep + "onlyleft.txt", "onlyL");
            File.WriteAllText(left + Utils.Sep + "renamed1.txt", "renamed1");
            File.WriteAllText(left + Utils.Sep + "renamed2.txt", "renamed2");
            File.WriteAllText(left + Utils.Sep + "empty1.txt", "");
            File.WriteAllText(left + Utils.Sep + "changed1.txt", "123");
            File.WriteAllText(left + Utils.Sep + "same.txt", "s");
            File.WriteAllText(right + Utils.Sep + "onlyright.txt", "onlyR");
            File.WriteAllText(right + Utils.Sep + "renamed1.a", "renamed1");
            File.WriteAllText(right + Utils.Sep + "renamed2.a", "renamed2");
            File.WriteAllText(right + Utils.Sep + "empty1.a", "");
            File.WriteAllText(right + Utils.Sep + "changed1.txt", "124");
            File.WriteAllText(right + Utils.Sep + "same.txt", "s");

            // set last-write-times
            var dtNow = DateTime.Now;
            foreach (var filename in Directory.EnumerateFiles(left).Concat(
                Directory.EnumerateFiles(right)))
            {
                File.SetLastWriteTimeUtc(filename, dtNow);
            }

            File.SetLastWriteTimeUtc(right + Utils.Sep + "changed1.txt", dtNow.AddDays(1));

            // run search-for-differences
            var results = SortFilesSearchDifferences.Go(settings);
            var expectedDifferences =
@"|empty1.a|Right
|onlyright.txt|Right
|renamed1.a|Right
|renamed2.a|Right
changed1.txt|changed1.txt|Changed
empty1.txt||Left
onlyleft.txt||Left
renamed1.txt||Left
renamed2.txt||Left";
            CompareResultsToString(results, expectedDifferences);

            // run search for moved files
            var query = from item in results
                        where item.Type == FileComparisonResultType.Left_Only
                        select item;
            var resultsMoved = SortFilesSearchDuplicates.SearchMovedFiles(
                settings.LeftDirectory, settings.RightDirectory, query);
            TestUtil.IsEq(2, resultsMoved.Count);

            // the 0-length empty.txt isn't included in this list, we don't treat it as a duplicate
            TestUtil.IsEq(Utils.Sep + "renamed1.txt",
                resultsMoved[0].Item1.FileInfoLeft.Filename);
            TestUtil.IsEq(Utils.Sep + "renamed1.a",
                resultsMoved[0].Item2);
            TestUtil.IsEq(Utils.Sep + "renamed2.txt",
                resultsMoved[1].Item1.FileInfoLeft.Filename);
            TestUtil.IsEq(Utils.Sep + "renamed2.a",
                resultsMoved[1].Item2);
        }

        static void TestMethod_TestFileOpFileSetWritetime()
        {
            var dir = TestUtil.GetTestSubDirectory("right_fndmved", true);
            var now = DateTime.UtcNow;
            var future = now.AddMinutes(25);
            var past = now.AddMinutes(-25);

            // re-use existing logic for fuzzy time comparison,
            // in case, say, we're running this test on a FAT drive with imprecise times.
            var settingsForTimeComparison = new SortFilesSettings();
            settingsForTimeComparison.AllowFiletimesDifferForDST = false;
            settingsForTimeComparison.AllowFiletimesDifferForFAT = true;

            // set write-time to be the future, and undo.
            var path = dir + Utils.Sep + "testIntoFuture.txt";
            File.WriteAllText(path, "testIntoFuture");
            File.SetLastWriteTimeUtc(path, now);
            var op = new FileOpFileSetWritetime(path, now, future);
            op.Do();
            TestUtil.IsTrue(SortFilesSearchDifferences.AreTimesEqual(future,
                File.GetLastWriteTimeUtc(path), settingsForTimeComparison));
            op.Undo();
            TestUtil.IsTrue(SortFilesSearchDifferences.AreTimesEqual(now,
                File.GetLastWriteTimeUtc(path), settingsForTimeComparison));

            path = dir + Utils.Sep + "testIntoPast.txt";
            File.WriteAllText(path, "testIntoPast");
            File.SetLastWriteTimeUtc(path, now);
            op = new FileOpFileSetWritetime(path, now, past);
            op.Do();
            TestUtil.IsTrue(SortFilesSearchDifferences.AreTimesEqual(past,
                File.GetLastWriteTimeUtc(path), settingsForTimeComparison));
            op.Undo();
            TestUtil.IsTrue(SortFilesSearchDifferences.AreTimesEqual(now,
                File.GetLastWriteTimeUtc(path), settingsForTimeComparison));
        }
    }
}

// Copyright (c) Ben Fisher, 2016.
// Licensed under GPLv3. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace labs_coordinate_pictures
{
    public static class CoordinatePicturesTests
    {
        static void TestMethod_ImageCache()
        {
            var dir = TestUtil.GetTestSubDirectory("imcache");
            for (int i = 0; i < 8; i++)
            {
                File.WriteAllText(Path.Combine(dir, "a" + i + ".png"), "fake image");
                File.WriteAllText(Path.Combine(dir, "b" + i + ".png"), "fake image");
            }

            // provide callbacks that record what was removed.
            const int cacheSize = 3;
            List<object> removedFromCache = new List<object>();
            Func<Bitmap, bool> canDisposeBitmap =
                (bmp) =>
                {
                    removedFromCache.Add(bmp);
                    return true;
                };
            Func<Action, bool> callbackOnUiThread =
                (act) =>
                {
                    act();
                    return true;
                };

            // standard lookup
            using (var cache = new ImageCache(20, 20, cacheSize,
                callbackOnUiThread, canDisposeBitmap, null, false))
            {
                // retrieve from the cache
                var bmp1 = cache.Get(Path.Combine(dir, "a1.png"), out int gotW, out int gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);

                // retrieving same path from the cache should return the exact same image
                var bmp1Same = cache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);
                TestUtil.IsEq((object)bmp1, (object)bmp1Same);

                // however, if lmt has changed, cached copy should be refreshed.
                var wasTime = File.GetLastWriteTime(Path.Combine(dir, "a1.png"));
                File.SetLastWriteTime(
                    Path.Combine(dir, "a1.png"), wasTime - new TimeSpan(0, 0, 10));
                var bmp1Changed = cache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq(1, gotW);
                TestUtil.IsEq(1, gotH);
                TestUtil.IsTrue((object)bmp1 != (object)bmp1Changed);

                // and further lookups should get this new copy.
                var bmp1ChangedAfter = cache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                TestUtil.IsEq((object)bmp1ChangedAfter, (object)bmp1Changed);
            }

            // add past the limit
            using (var cache = new ImageCache(20, 20, cacheSize,
                callbackOnUiThread, canDisposeBitmap, null, false))
            {
                // fill up cache
                removedFromCache.Clear();
                int gotW = 0, gotH = 0;
                var bmp1 = cache.Get(Path.Combine(dir, "a1.png"), out gotW, out gotH);
                var bmp2 = cache.Get(Path.Combine(dir, "a2.png"), out gotW, out gotH);
                var bmp3 = cache.Get(Path.Combine(dir, "a3.png"), out gotW, out gotH);
                TestUtil.IsEq(0, removedFromCache.Count);

                // add one more
                var bmp4 = cache.Get(Path.Combine(dir, "a4.png"), out gotW, out gotH);
                TestUtil.IsEq(1, removedFromCache.Count);
                TestUtil.IsEq((object)bmp1, (object)removedFromCache[0]);

                // bmp4 should now be in the cache
                var bmp4Again = cache.Get(Path.Combine(dir, "a4.png"), out gotW, out gotH);
                TestUtil.IsEq((object)bmp4Again, (object)bmp4);

                // add one more
                var bmp5 = cache.Get(Path.Combine(dir, "a5.png"), out gotW, out gotH);
                TestUtil.IsEq(2, removedFromCache.Count);
                TestUtil.IsEq((object)bmp2, (object)removedFromCache[1]);

                // add many
                removedFromCache.Clear();
                cache.Add(new string[] { Path.Combine(dir, "b1.png"),
                    Path.Combine(dir, "b2.png"), Path.Combine(dir, "b3.png"),
                    Path.Combine(dir, "b4.png"), Path.Combine(dir, "b5.png") });
                TestUtil.IsEq(5, removedFromCache.Count);
                TestUtil.IsEq((object)bmp3, (object)removedFromCache[4]);
                TestUtil.IsEq((object)bmp4, (object)removedFromCache[3]);
                TestUtil.IsEq((object)bmp5, (object)removedFromCache[2]);
            }
        }

        static void TestMethod_ImageViewExcerptCoordinates()
        {
            using (var excerpt = new ImageViewExcerpt(160, 120))
            using (var bitmapFull = new Bitmap(200, 240))
            {
                excerpt.GetShiftAmount(bitmapFull, clickX: 30, clickY: 40,
                    widthOfResizedImage: 150, heightOfResizedImage: 110,
                    shiftX: out int shiftx, shiftY: out int shifty);

                TestUtil.IsEq(-40, shiftx);
                TestUtil.IsEq(27, shifty);

                excerpt.GetShiftAmount(bitmapFull, clickX: 90, clickY: 20,
                    widthOfResizedImage: 150, heightOfResizedImage: 110,
                    shiftX: out shiftx, shiftY: out shifty);

                TestUtil.IsEq(40, shiftx);
                TestUtil.IsEq(-17, shifty);
            }
        }

        static void TestMethod_CategoriesStringToTuple()
        {
            { // should be valid to have no categories defined
                var tuples = ModeUtils.CategoriesStringToTuple("");
                TestUtil.IsEq(0, tuples.Length);
            }

            { // typical valid categories string
                var tuples = ModeUtils.CategoriesStringToTuple(
                    "A/categoryReadable/categoryId|B/categoryReadable2/categoryId2");

                TestUtil.IsEq(2, tuples.Length);
                TestUtil.IsEq("A", tuples[0].Item1);
                TestUtil.IsEq("categoryReadable", tuples[0].Item2);
                TestUtil.IsEq("categoryId", tuples[0].Item3);
                TestUtil.IsEq("B", tuples[1].Item1);
                TestUtil.IsEq("categoryReadable2", tuples[1].Item2);
                TestUtil.IsEq("categoryId2", tuples[1].Item3);
            }
        }

        static void TestMethod_CategorizeOnCompletionAction()
        {
            // the completion action will create directories and move files
            var mode = new ModeCategorizeAndRename();
            var dir = TestUtil.GetTestSubDirectory("categorize");
            File.WriteAllText(Path.Combine(dir, "t1.png"), "1234");
            File.WriteAllText(Path.Combine(dir, "t2__MARKAS__art.png"), "1234");
            File.WriteAllText(Path.Combine(dir, "t3__MARKAS__art.jpg"), "1234");
            File.WriteAllText(Path.Combine(dir, "t4__MARKAS__other.png"), "1234");
            File.WriteAllText(Path.Combine(dir, "t5__MARKAS__badfiletype.wav"), "1234");
            File.WriteAllText(Path.Combine(dir, "t6__MARKAS__unknowncategory.jpg"), "1234");
            var prevCategories = Configs.Current.Get(ConfigKey.CategoriesModeCategorizeAndRename);
            try
            {
                Configs.Current.Set(ConfigKey.CategoriesModeCategorizeAndRename, mode.GetDefaultCategories());
                using (var form = new FormGallery(mode, dir))
                {
                    form.CallCompletionAction();
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.png")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "art", "t2.png")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "art", "t3.jpg")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "other", "t4.png")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t5__MARKAS__badfiletype.wav")));
                    TestUtil.IsTrue(File.Exists(Path.Combine(dir, "unknowncategory", "t6.jpg")));
                }
            }
            finally
            {
                Configs.Current.Set(ConfigKey.CategoriesModeCategorizeAndRename, prevCategories);
            }
        }

        static void TestMethod_UndoStack()
        {
            UndoStack<int?> stack = new UndoStack<int?>();
            TestUtil.IsEq(null, stack.PeekUndo());
            TestUtil.IsEq(null, stack.PeekRedo());
            stack.Add(1);
            TestUtil.IsEq(1, stack.PeekUndo().Value);
            stack.Add(2);
            TestUtil.IsEq(2, stack.PeekUndo().Value);
            stack.Add(3);
            TestUtil.IsEq(3, stack.PeekUndo().Value);
            stack.Add(4);
            TestUtil.IsEq(4, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(3, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(2, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(1, stack.PeekUndo().Value);
            stack.Redo();
            TestUtil.IsEq(2, stack.PeekUndo().Value);
            stack.Redo();
            TestUtil.IsEq(3, stack.PeekUndo().Value);

            // not at the top of stack, will overwrite other values
            stack.Add(40);
            TestUtil.IsEq(40, stack.PeekUndo().Value);

            stack.Add(50);
            TestUtil.IsEq(50, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(40, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(3, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(2, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(1, stack.PeekUndo().Value);
            stack.Undo();
            TestUtil.IsEq(null, stack.PeekUndo());
            stack.Undo();
            TestUtil.IsEq(null, stack.PeekUndo());
        }

        static void TestMethod_FormWrapMoveFile()
        {
            var dir = TestUtil.GetTestSubDirectory("movefile");
            File.WriteAllText(Path.Combine(dir, "t1.bmp"), "1234");
            File.WriteAllText(Path.Combine(dir, "t2.bmp"), "123456789");
            var mode = new ModeCheckFilesizes();
            using (var form = new FormGallery(mode, dir))
            {
                // ok to call undo when undo stack is empty.
                form.UndoOrRedo(true);

                // wrapmovefile should be able to change file case
                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "t1.bmp"), Path.Combine(dir, "T1.bmp")));
                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "T1.bmp"), Path.Combine(dir, "t1.bmp")));

                // call wrapmovefile
                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "t1.bmp"), Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));

                // call Undo
                form.UndoOrRedo(true);
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.bmp")));

                // call WrapMoveFile
                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "t1.bmp"), Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));

                TestUtil.IsTrue(form.WrapMoveFile(
                    Path.Combine(dir, "t2.bmp"), Path.Combine(dir, "t2m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t2.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t2m.bmp")));

                // call Undo
                form.UndoOrRedo(true);
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t2m.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t2.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));
                form.UndoOrRedo(true);
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.bmp")));

                // call Redo
                form.UndoOrRedo(false);
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1m.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "t1.bmp")));
                form.UndoOrRedo(true);
            }
        }

        static void TestMethod_AutoAcceptSmallFiles()
        {
            var dir = TestUtil.GetTestSubDirectory("movefile");
            File.WriteAllText(Path.Combine(dir, "t1.bmp"), "1234");
            File.WriteAllText(Path.Combine(dir, "t2.bmp"), "123456789");
            File.WriteAllText(Path.Combine(dir, "a1.jpg"), "1");
            File.WriteAllText(Path.Combine(dir, "a2.jpg"), "1234");
            File.WriteAllText(Path.Combine(dir, "a3.jpg"), "123456789");

            var mode = new ModeCheckFilesizes();
            using (var form = new FormGallery(mode, dir))
            {
                // test AutoAcceptSmallFiles
                var acceptFilesSmallerThanBytes = 6;
                mode.AutoAcceptSmallFiles(form, acceptFilesSmallerThanBytes, acceptFilesSmallerThanBytes);
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t1.bmp")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "t2.bmp")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "a1.jpg")));
                TestUtil.IsTrue(!File.Exists(Path.Combine(dir, "a2.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a3.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a1__MARKAS__size is good.jpg")));
                TestUtil.IsTrue(File.Exists(Path.Combine(dir, "a2__MARKAS__size is good.jpg")));
            }
        }

        static void TestMethod_CheckKeyBindings()
        {
            var bindings = new Dictionary<string, string>
            {
                { "1", "category 1" },
                { "F", "category f" }
            };

            TestUtil.IsEq(null,
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.F1, bindings));
            TestUtil.IsEq(null,
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.Oem1, bindings));
            TestUtil.IsEq(null,
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.G, bindings));
            TestUtil.IsEq("category 1",
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.D1, bindings));
            TestUtil.IsEq("category f",
                FormGallery.CheckKeyBindingsToAssignCategory(Keys.F, bindings));
        }

        static void TestMethod_PersistMostRecentlyUsedList()
        {
            string path = Path.Combine(TestUtil.GetTestSubDirectory("testcfg"), "testmru.ini");
            Configs cfg = new Configs(path);

            // using None should be a no-op
            var mruNone = new PersistMostRecentlyUsedList(InputBoxHistory.None, cfg);
            TestUtil.IsStringArrayEq(null, mruNone.Get());
            mruNone.AddToHistory("abcd");
            TestUtil.IsStringArrayEq(null, mruNone.Get());

            // try to set invalid data
            var mruTestInvalid = new PersistMostRecentlyUsedList(InputBoxHistory.RenameImage, cfg);
            TestUtil.IsStringArrayEq(null, mruTestInvalid.Get());
            mruTestInvalid.AddToHistory("contains||||");
            TestUtil.IsStringArrayEq(null, mruTestInvalid.Get());
            mruTestInvalid.AddToHistory("");
            TestUtil.IsStringArrayEq(null, mruTestInvalid.Get());
            var longText = Enumerable.Range(0, 500).Select(x => x.ToString());
            mruTestInvalid.AddToHistory(string.Join("a", longText));
            TestUtil.IsStringArrayEq(null, mruTestInvalid.Get());

            // add, without first calling Get
            var mruTestAddFirst = new PersistMostRecentlyUsedList(InputBoxHistory.RenameImage, cfg);
            mruTestAddFirst.AddToHistory("abc");
            TestUtil.IsStringArrayEq("abc", mruTestAddFirst.Get());

            // set valid data
            var mruTest = new PersistMostRecentlyUsedList(InputBoxHistory.RenameImage, cfg, 10);
            TestUtil.IsStringArrayEq("abc", mruTest.Get());
            mruTest.AddToHistory("def");
            TestUtil.IsStringArrayEq("def|abc", mruTest.Get());
            mruTest.AddToHistory("ghi");
            TestUtil.IsStringArrayEq("ghi|def|abc", mruTest.Get());

            // redundant should not be additional, but still moved to top
            mruTest.AddToHistory("abc");
            TestUtil.IsStringArrayEq("abc|ghi|def", mruTest.Get());
            mruTest.AddToHistory("def");
            TestUtil.IsStringArrayEq("def|abc|ghi", mruTest.Get());
            mruTest.AddToHistory("def");
            TestUtil.IsStringArrayEq("def|abc|ghi", mruTest.Get());

            // length should be capped at 10.
            for (int i = 0; i <= 10; i++)
            {
                mruTest.AddToHistory(i.ToString());
            }

            TestUtil.IsStringArrayEq("10|9|8|7|6|5|4|3|2|1", mruTest.Get());
        }
    }
}

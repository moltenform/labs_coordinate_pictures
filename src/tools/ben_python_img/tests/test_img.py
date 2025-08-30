

import sys
import pytest
import PIL
from PIL import Image
from shinerainsevenlib.standard import *
from shinerainsevenlib.core import *
from test_img_utils import createTestImage, fxTmpDirImplementation, checkFilesAndSizes

sys.path.append('..')
import img_utils
import img_convert_resize
import img_resize_keep_exif


def testGetMarkFromFilename():
    # tests splitting a filename that contains the "__MARKAS__" marker.
    assert ('/test/file.jpg', '123') == img_utils.getMarkFromFilename('/test/file__MARKAS__123.jpg')
    assert ('/test/file.also.jpg', '123') == img_utils.getMarkFromFilename('/test/file.also__MARKAS__123.jpg')
    assert ('/test/file.jpg', '') == img_utils.getMarkFromFilename('/test/file__MARKAS__.jpg')
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/dirmark__MARKAS__b/file__MARKAS__123.jpg'), ValueError, 'Directories')
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/dirmark__MARKAS__b/file.jpg'), ValueError, 'Directories')
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/file__MARKAS__123__MARKAS__123.jpg'), ValueError, 'exactly one marker')
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/file.jpg'), ValueError, 'exactly one marker')
    
    # we recently changed it so that the after-marker can have a . character
    assert ('/test/file.jpg', '123.foo') == img_utils.getMarkFromFilename('/test/file__MARKAS__123.foo.jpg')
        
def testGetFilesWithWrongExtension(fxTmpDir):
    # looks for files that do not have the given extension.
    tmpDirExt = files.join(fxTmpDir, 'testWrongExtension')
    files.makeDirs(tmpDirExt)
    files.writeAll(files.join(tmpDirExt, 'a.jpg'), 'content')
    files.writeAll(files.join(tmpDirExt, 'B.JPG'), 'content')
    files.writeAll(files.join(tmpDirExt, 'c.jpg'), 'content')
    files.writeAll(files.join(tmpDirExt, 'd.txt'), 'content')
    files.writeAll(files.join(tmpDirExt, 'e'), 'content')
    files.makeDirs(tmpDirExt + '/subdir')
    fnGetFiles = files.listFiles
    setRet = img_utils.getFilesWrongExtension(tmpDirExt, fnGetFiles, ['jpg'])
    expected = [files.join(tmpDirExt, 'd.txt'), files.join(tmpDirExt, 'e')]
    assert expected == list(sorted(f[0] for f in setRet))
        
def testGetNewSizeFromResizeSpec():
    # common valid cases
    assert (50, 100) == img_convert_resize.getNewSizeFromResizeSpec('50%', 100, 200)
    assert (90, 180) == img_convert_resize.getNewSizeFromResizeSpec('90%', 101, 201)
    assert (80, 160) == img_convert_resize.getNewSizeFromResizeSpec('80h', 100, 200)
    assert (160, 80) == img_convert_resize.getNewSizeFromResizeSpec('80h', 200, 100)
    assert (5, 10) == img_convert_resize.getNewSizeFromResizeSpec('5%', 100, 200)
    
    # invalid spec
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('50x', 100, 200), ValueError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('50', 100, 200), ValueError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('0.5%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec(' 50%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('50% ', 100, 200), ValueError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('50%%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('50%50%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('0%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('00%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('h', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('1a0%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('1a0h', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('110%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('-10%', 100, 200), AssertionError)
    assertException(lambda: img_convert_resize.getNewSizeFromResizeSpec('-10h', 100, 200), AssertionError)
    
    # cases not to resize.
    assert (0, 0) == img_convert_resize.getNewSizeFromResizeSpec('100%', 100, 200)
    assert (0, 0) == img_convert_resize.getNewSizeFromResizeSpec('101h', 100, 200)
    assert (0, 0) == img_convert_resize.getNewSizeFromResizeSpec('101h', 200, 100)

def testResizeKeepExifWithActualFiles(fxTmpDir):
    fxTmpDir = files.join(fxTmpDir, 'testResizeKeepExif')
    files.makeDirs(fxTmpDir)
    
    # create initial files
    im = createTestImage(96, 144, 1)
    filenames = [files.join(fxTmpDir, 'a100p__MARKAS__100%.jpg'),
        files.join(fxTmpDir, 'a50p__MARKAS__50%.jpg'),
        files.join(fxTmpDir, 'a32h__MARKAS__32h.jpg'),
        files.join(fxTmpDir, 'a200h__MARKAS__200h.jpg')]
    for filename in filenames:
        im.save(filename)
    del im
       
    for index, filename in enumerate(filenames):
        assert (96, 144) == img_utils.getImageDims(filename)
        
        # set an obscure tag that won't be transferred
        img_utils.setExifField(filename, 'ProfileCopyright', 'ObscureTagSet' + str(index))
        assert 'ObscureTagSet' + str(index) == img_utils.readExifField(filename, 'ProfileCopyright')
        
        # set a common tag that will be transferred
        img_utils.setExifField(filename, 'Make', 'TestingMake' + str(index))
        assert 'TestingMake' + str(index) == img_utils.readExifField(filename, 'Make')
    
    # run the resizes. resizeAllAndKeepExif resizes based on the filename.
    img_resize_keep_exif.resizeAllAndKeepExif(fxTmpDir,
        recurse=False, storeOriginalFilename=True, storeExifFromOriginal=True, jpgHighQualityChromaSampling=False)
    
    # check dimensions
    assert (96, 144) == img_utils.getImageDims(files.join(fxTmpDir, 'a100p.jpg'))
    assert (48, 72) == img_utils.getImageDims(files.join(fxTmpDir, 'a50p.jpg'))
    assert (32, 48) == img_utils.getImageDims(files.join(fxTmpDir, 'a32h.jpg'))
    assert (96, 144) == img_utils.getImageDims(files.join(fxTmpDir, 'a200h.jpg'))
    
    # check common tag, should have been transferred
    assert 'TestingMake0' == img_utils.readExifField(files.join(fxTmpDir, 'a100p.jpg'), 'Make')
    assert 'TestingMake1' == img_utils.readExifField(files.join(fxTmpDir, 'a50p.jpg'), 'Make')
    assert 'TestingMake2' == img_utils.readExifField(files.join(fxTmpDir, 'a32h.jpg'), 'Make')
    assert 'TestingMake3' == img_utils.readExifField(files.join(fxTmpDir, 'a200h.jpg'), 'Make')
    
    # check uncommon tag, should only be present for the ones moved instead of resized
    assert 'ObscureTagSet0' == img_utils.readExifField(files.join(fxTmpDir, 'a100p.jpg'), 'ProfileCopyright')
    assert '' == img_utils.readExifField(files.join(fxTmpDir, 'a50p.jpg'), 'ProfileCopyright')
    assert '' == img_utils.readExifField(files.join(fxTmpDir, 'a32h.jpg'), 'ProfileCopyright')
    assert 'ObscureTagSet3' == img_utils.readExifField(files.join(fxTmpDir, 'a200h.jpg'), 'ProfileCopyright')
    
    # check that original filename is stored in exif data
    assert 'a100p.jpg' == img_utils.readOriginalFilename(files.join(fxTmpDir, 'a100p.jpg'))
    assert 'a50p.jpg' == img_utils.readOriginalFilename(files.join(fxTmpDir, 'a50p.jpg'))
    assert 'a32h.jpg' == img_utils.readOriginalFilename(files.join(fxTmpDir, 'a32h.jpg'))
    assert 'a200h.jpg' == img_utils.readOriginalFilename(files.join(fxTmpDir, 'a200h.jpg'))
    
    expectedSizes = '''a100p.jpg|8524
a200h.jpg|8524
a200h__MARKAS__200h.jpg|8502
a32h.jpg|1293
a32h__MARKAS__32h.jpg|8502
a50p.jpg|2506
a50p__MARKAS__50%.jpg|8502'''.replace('\r\n', '\n')
    resultSizes = '\n'.join([short + '|' + str(files.getSize(file))
        for file, short in sorted(files.listFiles(fxTmpDir))])
    try:
        pVersion = PIL.PILLOW_VERSION
    except AttributeError:
        pVersion = PIL.__version__
    assert expectedSizes, resultSizes == 'current pillow version=%s' % pVersion

def testResizeKeepExifCleanup(fxTmpDir):
    # when the user has reviewed that the conversion looks correct, they'll run cleanup()
    # which will discard the previous files with __MARKAS__.
    fxTmpDir = files.join(fxTmpDir, 'testCleanup')
    files.makeDirs(fxTmpDir)
    files.writeAll(files.join(fxTmpDir, 'a1.jpg'), '')
    files.writeAll(files.join(fxTmpDir, 'a1__MARKAS__50%.jpg'), '')
    files.writeAll(files.join(fxTmpDir, 'a2.jpg'), '')
    files.writeAll(files.join(fxTmpDir, 'a2__MARKAS__200h.jpg'), '')
    files.writeAll(files.join(fxTmpDir, 'a3.png'), '')
    files.writeAll(files.join(fxTmpDir, 'a3__MARKAS__100%.png'), '')
    
    # file with no corresponding markas should not be deleted.
    files.writeAll(files.join(fxTmpDir, 'a4.jpg'), '')
    
    # files with no corresponding converted file should not be deleted.
    files.writeAll(files.join(fxTmpDir, 'a5__MARKAS__100%.jpg'), '')
    files.writeAll(files.join(fxTmpDir, 'a6__MARKAS__.jpg'), '')
    
    img_resize_keep_exif.cleanup(fxTmpDir, recurse=False, prompt=False)
    expectedSizes = '''a1.jpg|0
a2.jpg|0
a3.png|0
a3__MARKAS__100%.png|0
a4.jpg|0
a5__MARKAS__100%.jpg|0
a6__MARKAS__.jpg|0'''.replace('\r\n', '\n')
    resultSizes = '\n'.join([short + '|' + str(files.getSize(file))
        for file, short in sorted(files.listFiles(fxTmpDir))])
    assert expectedSizes == resultSizes



def testResizeKeepExifErrorsShouldRaise(fxTmpDir):
    # most exif operations on an invalid jpg should raise PythonImgExifError
    files.writeAll(files.join(fxTmpDir, 'invalidjpg.jpg'), 'not a valid jpg')
    files.writeAll(files.join(fxTmpDir, 'invalidjpg2.jpg'), 'not a valid jpg')
    with pytest.raises(img_utils.PythonImgExifError):
        got = img_utils.readOriginalFilename(files.join(fxTmpDir, 'invalidjpg.jpg'))
        if not got:
            raise img_utils.PythonImgExifError('this is also ok as long as it is not truthy')
    
    assertException(lambda: img_utils.stampJpgWithOriginalFilename(
        files.join(fxTmpDir, 'invalidjpg.jpg'), 'test'), img_utils.PythonImgExifError)
    img_utils.transferMostUsefulExifTags(
        files.join(fxTmpDir, 'invalidjpg.jpg'),
        files.join(fxTmpDir, 'invalidjpg2.jpg'))
    assertException(lambda: img_utils.removeResolutionTags(
        files.join(fxTmpDir, 'invalidjpg.jpg')), img_utils.PythonImgExifError)


    
def testCombinatoricImageConversion(fxTmpDir):
    width, height = 120, 120
    testImage = createTestImage(width, height, 1)
    
    # go from each format to every other format!
    # note: bmp should be first in the list
    formats = ['bmp', 'png', 'jpg', 'webp']
    jpgQuality = 100
    
    for format in formats:
        startfile = files.join(fxTmpDir, 'start.' + format)
        if format == 'bmp':
            testImage.save(startfile)
        else:
            img_convert_resize.convertOrResizeImage(files.join(fxTmpDir, 'start.bmp'),
                startfile, jpgQuality=jpgQuality)
        for outformat in formats:
            if outformat != format:
                outfile = startfile + '.' + outformat
                assert not files.exists(outfile)
                img_convert_resize.convertOrResizeImage(startfile, outfile, jpgQuality=jpgQuality)
                assert files.exists(outfile)
                
    expectedSizes = strToList('''start.bmp|43254
start.bmp.jpg|15536
start.bmp.png|39430
start.bmp.webp|14454
start.jpg|15536
start.jpg.bmp|43254
start.jpg.png|39483
start.jpg.webp|14454
start.png|39430
start.png.bmp|43254
start.png.jpg|15536
start.png.webp|14454
start.webp|14454
start.webp.bmp|43254
start.webp.jpg|15536
start.webp.png|22366''')
    
    checkFilesAndSizes(fxTmpDir, expectedSizes, startWith='start')
    
    # are bmps equivalent
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp'), files.join(fxTmpDir, 'start.png.bmp'))
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp'), files.join(fxTmpDir, 'start.webp.bmp'))
    
    # are jpgs equivalent
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp.jpg'), files.join(fxTmpDir, 'start.jpg'))
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp.jpg'), files.join(fxTmpDir, 'start.png.jpg'))
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp.jpg'), files.join(fxTmpDir, 'start.webp.jpg'))

    # are webps equivalent
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp.webp'), files.join(fxTmpDir, 'start.png.webp'))
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp.webp'), files.join(fxTmpDir, 'start.webp'))
    
    # are pngs equivalent
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp.png'), files.join(fxTmpDir, 'start.png'))
    
    # png written by dwebp is different, but it should still roundtrip
    img_convert_resize.convertOrResizeImage(files.join(fxTmpDir, 'start.webp.png'), files.join(fxTmpDir, 'start.webp.png.bmp'))
    assert files.fileContentsEqual(files.join(fxTmpDir, 'start.bmp'), files.join(fxTmpDir, 'start.webp.png.bmp'))

def testJpgQualities(fxTmpDir):
    width, height = 120, 120
    testImage = createTestImage(width, height, 1)

    # simply write several jpgs at different qualities, and make sure the file sizes are as expected.
    fxTmpDir = files.join(fxTmpDir, 'testJpgQuality')
    files.makeDirs(fxTmpDir)
    testImage.save(files.join(fxTmpDir, 'start.bmp'))
    qualities = [100, 90, 60, 10]
    for qual in qualities:
        img_convert_resize.convertOrResizeImage(files.join(fxTmpDir, 'start.bmp'),
            files.join(fxTmpDir, 'q%d.jpg'%qual), jpgQuality=qual)
    
    expectedSizes = strToList('''q10.jpg|961
q100.jpg|15536
q60.jpg|5093
q90.jpg|9361
start.bmp|43254''')

    checkFilesAndSizes(fxTmpDir, expectedSizes, startWith='')

# passes on pillow 3.2, 3.3, 4.0


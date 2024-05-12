
import img_utils
import img_convert_resize
import img_resize_keep_exif
import PIL
from PIL import Image
import sys
sys.path.append('bn_python_common.zip')
from bn_python_common import *

def img_utils_testGetMarkFromFilename():
    # tests splitting a filename that contains the "__MARKAS__" marker.
    assertEq(('/test/file.jpg', '123'), img_utils.getMarkFromFilename('/test/file__MARKAS__123.jpg'))
    assertEq(('/test/file.also.jpg', '123'), img_utils.getMarkFromFilename('/test/file.also__MARKAS__123.jpg'))
    assertEq(('/test/file.jpg', ''), img_utils.getMarkFromFilename('/test/file__MARKAS__.jpg'))
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/dirmark__MARKAS__b/file__MARKAS__123.jpg'), ValueError, 'Directories')
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/dirmark__MARKAS__b/file.jpg'), ValueError, 'Directories')
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/file__MARKAS__123__MARKAS__123.jpg'), ValueError, 'exactly one marker')
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/file.jpg'), ValueError, 'exactly one marker')
    assertException(lambda: img_utils.getMarkFromFilename(
        '/test/file__MARKAS__123.foo.jpg'), ValueError, 'after the marker')
        
def img_utils_testGetFilesWithWrongExtension(tmpDir):
    # looks for files that do not have the given extension.
    tmpDirExt = files.join(tmpDir, 'testWrongExtension')
    files.makedirs(tmpDirExt)
    files.writeall(files.join(tmpDirExt, 'a.jpg'), 'content')
    files.writeall(files.join(tmpDirExt, 'B.JPG'), 'content')
    files.writeall(files.join(tmpDirExt, 'c.jpg'), 'content')
    files.writeall(files.join(tmpDirExt, 'd.txt'), 'content')
    files.writeall(files.join(tmpDirExt, 'e'), 'content')
    files.makedirs(tmpDirExt + '/subdir')
    fnGetFiles = files.listfiles
    setRet = img_utils.getFilesWrongExtension(tmpDirExt, fnGetFiles, ['jpg'])
    expected = [files.join(tmpDirExt, 'd.txt'), files.join(tmpDirExt, 'e')]
    assertEq(expected, list(sorted(f[0] for f in setRet)))
        
def img_convert_testGetNewSizeFromResizeSpec():
    # common valid cases
    assertEq((50, 100), img_convert_resize.getNewSizeFromResizeSpec('50%', 100, 200))
    assertEq((90, 180), img_convert_resize.getNewSizeFromResizeSpec('90%', 101, 201))
    assertEq((80, 160), img_convert_resize.getNewSizeFromResizeSpec('80h', 100, 200))
    assertEq((160, 80), img_convert_resize.getNewSizeFromResizeSpec('80h', 200, 100))
    assertEq((5, 10), img_convert_resize.getNewSizeFromResizeSpec('5%', 100, 200))
    
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
    assertEq((0, 0), img_convert_resize.getNewSizeFromResizeSpec('100%', 100, 200))
    assertEq((0, 0), img_convert_resize.getNewSizeFromResizeSpec('101h', 100, 200))
    assertEq((0, 0), img_convert_resize.getNewSizeFromResizeSpec('101h', 200, 100))

def img_resize_keep_exif_testActualFiles(tmpDir):
    trace('img_resize_keep_exif_testActualFiles started.')
    tmpDir = files.join(tmpDir, 'testResizeKeepExif')
    files.makedirs(tmpDir)
    
    # create initial files
    im = createTestImage(96, 144, 1)
    filenames = [files.join(tmpDir, 'a100p__MARKAS__100%.jpg'),
        files.join(tmpDir, 'a50p__MARKAS__50%.jpg'),
        files.join(tmpDir, 'a32h__MARKAS__32h.jpg'),
        files.join(tmpDir, 'a200h__MARKAS__200h.jpg')]
    for filename in filenames:
        im.save(filename)
    del im
       
    for index, filename in enumerate(filenames):
        assertEq((96, 144), img_utils.getImageDims(filename))
        
        # set an obscure tag that won't be transferred
        img_utils.setExifField(filename, 'ProfileCopyright', 'ObscureTagSet' + str(index))
        assertEq('ObscureTagSet' + str(index), img_utils.readExifField(filename, 'ProfileCopyright'))
        
        # set a common tag that will be transferred
        img_utils.setExifField(filename, 'Make', 'TestingMake' + str(index))
        assertEq('TestingMake' + str(index), img_utils.readExifField(filename, 'Make'))
    
    # run the resizes. resizeAllAndKeepExif resizes based on the filename.
    img_resize_keep_exif.resizeAllAndKeepExif(tmpDir,
        recurse=False, storeOriginalFilename=True, storeExifFromOriginal=True, jpgHighQualityChromaSampling=False)
    
    # check dimensions
    assertEq((96, 144), img_utils.getImageDims(files.join(tmpDir, 'a100p.jpg')))
    assertEq((48, 72), img_utils.getImageDims(files.join(tmpDir, 'a50p.jpg')))
    assertEq((32, 48), img_utils.getImageDims(files.join(tmpDir, 'a32h.jpg')))
    assertEq((96, 144), img_utils.getImageDims(files.join(tmpDir, 'a200h.jpg')))
    
    # check common tag, should have been transferred
    assertEq('TestingMake0', img_utils.readExifField(files.join(tmpDir, 'a100p.jpg'), 'Make'))
    assertEq('TestingMake1', img_utils.readExifField(files.join(tmpDir, 'a50p.jpg'), 'Make'))
    assertEq('TestingMake2', img_utils.readExifField(files.join(tmpDir, 'a32h.jpg'), 'Make'))
    assertEq('TestingMake3', img_utils.readExifField(files.join(tmpDir, 'a200h.jpg'), 'Make'))
    
    # check uncommon tag, should only be present for the ones moved instead of resized
    assertEq('ObscureTagSet0', img_utils.readExifField(files.join(tmpDir, 'a100p.jpg'), 'ProfileCopyright'))
    assertEq('', img_utils.readExifField(files.join(tmpDir, 'a50p.jpg'), 'ProfileCopyright'))
    assertEq('', img_utils.readExifField(files.join(tmpDir, 'a32h.jpg'), 'ProfileCopyright'))
    assertEq('ObscureTagSet3', img_utils.readExifField(files.join(tmpDir, 'a200h.jpg'), 'ProfileCopyright'))
    
    # check that original filename is stored in exif data
    assertEq('a100p.jpg', img_utils.readOriginalFilename(files.join(tmpDir, 'a100p.jpg')))
    assertEq('a50p.jpg', img_utils.readOriginalFilename(files.join(tmpDir, 'a50p.jpg')))
    assertEq('a32h.jpg', img_utils.readOriginalFilename(files.join(tmpDir, 'a32h.jpg')))
    assertEq('a200h.jpg', img_utils.readOriginalFilename(files.join(tmpDir, 'a200h.jpg')))
    
    expectedSizes = '''a100p.jpg|8524
a200h.jpg|8524
a200h__MARKAS__200h.jpg|8502
a32h.jpg|1293
a32h__MARKAS__32h.jpg|8502
a50p.jpg|2506
a50p__MARKAS__50%.jpg|8502'''.replace('\r\n', '\n')
    resultSizes = '\n'.join([short + '|' + str(files.getsize(file))
        for file, short in sorted(files.listfiles(tmpDir))])
    try:
        pVersion = PIL.PILLOW_VERSION
    except AttributeError:
        pVersion = PIL.__version__
    assertEq(expectedSizes, resultSizes, 'current pillow version=%s' % pVersion)
    trace('img_resize_keep_exif_testActualFiles passed.')

def img_resize_keep_exif_testCleanup(tmpDir):
    # when the user has reviewed that the conversion looks correct, they'll run cleanup()
    # which will discard the previous files with __MARKAS__.
    trace('img_resize_keep_exif_testCleanup started.')
    tmpDir = files.join(tmpDir, 'testCleanup')
    files.makedirs(tmpDir)
    files.writeall(files.join(tmpDir, 'a1.jpg'), '')
    files.writeall(files.join(tmpDir, 'a1__MARKAS__50%.jpg'), '')
    files.writeall(files.join(tmpDir, 'a2.jpg'), '')
    files.writeall(files.join(tmpDir, 'a2__MARKAS__200h.jpg'), '')
    files.writeall(files.join(tmpDir, 'a3.png'), '')
    files.writeall(files.join(tmpDir, 'a3__MARKAS__100%.png'), '')
    
    # file with no corresponding markas should not be deleted.
    files.writeall(files.join(tmpDir, 'a4.jpg'), '')
    
    # files with no corresponding converted file should not be deleted.
    files.writeall(files.join(tmpDir, 'a5__MARKAS__100%.jpg'), '')
    files.writeall(files.join(tmpDir, 'a6__MARKAS__.jpg'), '')
    
    img_resize_keep_exif.cleanup(tmpDir, recurse=False, prompt=False)
    expectedSizes = '''a1.jpg|0
a2.jpg|0
a3.png|0
a3__MARKAS__100%.png|0
a4.jpg|0
a5__MARKAS__100%.jpg|0
a6__MARKAS__.jpg|0'''.replace('\r\n', '\n')
    resultSizes = '\n'.join([short + '|' + str(files.getsize(file))
        for file, short in sorted(files.listfiles(tmpDir))])
    assertEq(expectedSizes, resultSizes)
    trace('img_resize_keep_exif_testCleanup passed.')

def assertExceptionOrFalse(fn, excType):
    ret = False
    try:
        ret = fn()
    except:
        e = sys.exc_info()[1]
        assertTrue(isinstance(e, excType), 'wrong exc type')
    assertTrue(not ret)

def img_resize_keep_exif_testExifErrorsShouldRaise(tmpDir):
    # most exif operations on an invalid jpg should raise PythonImgExifError
    files.writeall(files.join(tmpDir, 'invalidjpg.jpg'), 'not a valid jpg')
    files.writeall(files.join(tmpDir, 'invalidjpg2.jpg'), 'not a valid jpg')
    assertExceptionOrFalse(lambda: not img_utils.readOriginalFilename(
        files.join(tmpDir, 'invalidjpg.jpg')), img_utils.PythonImgExifError)
    assertException(lambda: img_utils.stampJpgWithOriginalFilename(
        files.join(tmpDir, 'invalidjpg.jpg'), 'test'), img_utils.PythonImgExifError)
    assertException(lambda: img_utils.transferMostUsefulExifTags(
        files.join(tmpDir, 'invalidjpg.jpg'),
        files.join(tmpDir, 'invalidjpg2.jpg')), img_utils.PythonImgExifError)
    assertException(lambda: img_utils.removeResolutionTags(
        files.join(tmpDir, 'invalidjpg.jpg')), img_utils.PythonImgExifError)

class RNG(object):
    # so that same sequence is generated regardless of Python version
    def __init__(self, seed=0):
        self.previous = seed

    def next(self):
        # use contants from glibc's rand()
        modulus = 2**31 - 1
        a, c = 1103515245, 12345
        ret = (self.previous * a + c) % modulus
        self.previous = ret
        return ret

def createTestImage(width, height, seed):
    rng = RNG(seed)
    im = Image.new("RGB", (width, height))
    for y in xrange(height):
        for x in xrange(width):
            v = rng.next() % 256
            im.putpixel((x, y), (v, v, v))
    return im
    
def testCombinatoricImageConversion(tmpDir, testImage):
    # go from each format to every other format!
    # note: bmp should be first in the list
    formats = ['bmp', 'png', 'jpg', 'webp']
    jpgQuality = 100
    if not getInputBool('run combinatoricImageConversionTest?'):
        return
    
    for format in formats:
        startfile = files.join(tmpDir, 'start.' + format)
        if format == 'bmp':
            testImage.save(startfile)
        else:
            img_convert_resize.convertOrResizeImage(files.join(tmpDir, 'start.bmp'),
                startfile, jpgQuality=jpgQuality)
        for outformat in formats:
            if outformat != format:
                outfile = startfile + '.' + outformat
                assertTrue(not files.exists(outfile))
                img_convert_resize.convertOrResizeImage(startfile, outfile, jpgQuality=jpgQuality)
                assertTrue(files.exists(outfile))
                
    expectedSizes = '''start.bmp|43254
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
start.webp.png|22366'''.replace('\r\n', '\n')
    
    resultSizes = '\n'.join([short + '|' + str(files.getsize(file))
        for file, short in sorted(files.listfiles(tmpDir)) if short.startswith('start')])
    assertEq(expectedSizes, resultSizes)
    
    # are bmps equivalent
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp'), files.join(tmpDir, 'start.png.bmp')))
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp'), files.join(tmpDir, 'start.webp.bmp')))
    
    # are jpgs equivalent
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp.jpg'), files.join(tmpDir, 'start.jpg')))
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp.jpg'), files.join(tmpDir, 'start.png.jpg')))
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp.jpg'), files.join(tmpDir, 'start.webp.jpg')))

    # are webps equivalent
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp.webp'), files.join(tmpDir, 'start.png.webp')))
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp.webp'), files.join(tmpDir, 'start.webp')))
    
    # are pngs equivalent
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp.png'), files.join(tmpDir, 'start.png')))
    
    # png written by dwebp is different, but it should still roundtrip
    img_convert_resize.convertOrResizeImage(files.join(tmpDir, 'start.webp.png'), files.join(tmpDir, 'start.webp.png.bmp'))
    assertTrue(files.fileContentsEqual(files.join(tmpDir, 'start.bmp'), files.join(tmpDir, 'start.webp.png.bmp')))

def testJpgQualities(tmpDir, testImage):
    # simply write several jpgs at different qualities, and make sure the file sizes are as expected.
    tmpDir = files.join(tmpDir, 'testJpgQuality')
    files.makedirs(tmpDir)
    testImage.save(files.join(tmpDir, 'start.bmp'))
    qualities = [100, 90, 60, 10]
    for qual in qualities:
        img_convert_resize.convertOrResizeImage(files.join(tmpDir, 'start.bmp'),
            files.join(tmpDir, 'q%d.jpg'%qual), jpgQuality=qual)
    
    expectedSizes = '''q10.jpg|961
q100.jpg|15536
q60.jpg|5093
q90.jpg|9361
start.bmp|43254'''.replace('\r\n', '\n')
    resultSizes = '\n'.join([short + '|' + str(files.getsize(file))
        for file, short in sorted(files.listfiles(tmpDir))])
    assertEq(expectedSizes, resultSizes)

def img_convert_resize_tests(tmpDir):
    width, height = 120, 120
    testImage = createTestImage(width, height, 1)
    testCombinatoricImageConversion(tmpDir, testImage)
    testJpgQualities(tmpDir, testImage)


if __name__ == '__main__':
    # passes on pillow 3.2, 3.3, 4.0
    tmpDir = files.join(img_utils.getTempLocation(), 'testimgconvert')
    if files.isdir(tmpDir):
        files.rmtree(tmpDir)
    files.makedirs(tmpDir)

    try:
        img_utils_testGetMarkFromFilename()
        img_utils_testGetFilesWithWrongExtension(tmpDir)
        img_resize_keep_exif_testActualFiles(tmpDir)
        img_resize_keep_exif_testCleanup(tmpDir)
        img_resize_keep_exif_testExifErrorsShouldRaise(tmpDir)
        img_convert_testGetNewSizeFromResizeSpec()
        img_convert_resize_tests(tmpDir)
    finally:
        files.rmtree(tmpDir)

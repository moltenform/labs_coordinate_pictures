from ben_python_common import *
import img_utils
import img_convert_resize

def img_utils_tests():
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

def createTestImage(width, height, seed):
    import random
    random.seed(seed)
    from PIL import Image
    im = Image.new("RGB", (width, height))
    for y in xrange(height):
        for x in xrange(width):
            v = random.choice([0, 255])
            im.putpixel((x, y), (v, v, v))
    
    return im
    
def combinatoricImageConversionTest(im, tmpDir):
    # go from each format to every other format!
    formats = ['bmp', 'png', 'jpg', 'webp']  # bmp should be first in the list
    jpgQuality = 100
    
    for format in formats:
        startfile = files.join(tmpDir, 'start.' + format)
        if format == 'bmp':
            im.save(startfile)
        else:
            img_convert_resize.convertOrResizeImage(files.join(tmpDir, 'start.bmp'),
                startfile, jpgQuality=jpgQuality)
        for outformat in formats:
            if outformat != format:
                outfile = startfile + '.' + outformat
                assertTrue(not files.exists(outfile))
                img_convert_resize.convertOrResizeImage(startfile, outfile, jpgQuality=jpgQuality)
                assertTrue(files.exists(outfile))
                
    # if the PIL version changes, these might need to be updated
    expectedSizes = '''start.bmp|43254
start.bmp.jpg|16960
start.bmp.png|4526
start.bmp.webp|1872
start.jpg|16960
start.jpg.bmp|43254
start.jpg.png|4750
start.jpg.webp|2164
start.png|4526
start.png.bmp|43254
start.png.jpg|16960
start.png.webp|1872
start.webp|1872
start.webp.bmp|43254
start.webp.jpg|16960
start.webp.png|5786'''.replace('\r\n', '\n')
    resultSizes = '\n'.join([short + '|' + str(files.getsize(file))
        for file, short in files.listfiles(tmpDir) if short.startswith('start')])
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

def img_convert_resize_tests(tmpDir):
    width, height = 120, 120
    im = createTestImage(width, height, 1)
    combinatoricImageConversionTest(im, tmpDir)

if __name__ == '__main__':
    tmpDir = files.join(img_utils.getTempLocation(), 'testimgconvert')
    if files.isdir(tmpDir):
        files.rmtree(tmpDir)
    files.makedirs(tmpDir)
        
    try:
        img_utils_tests()
        img_convert_resize_tests(tmpDir)
    finally:
        files.rmtree(tmpDir)


import sys
from PIL import Image
from shinerainsevenlib.standard import *
from shinerainsevenlib.core import *
import pytest
import tempfile

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

def checkFilesAndSizes(dir, expected, startWith):
    expectNames = jslike.map(expected, lambda s:s.split('|')[0])
    resultNames = files.listFiles(dir, filenamesOnly=True)
    resultNames = jslike.filter(resultNames, lambda s: s.startswith(startWith))
    assert len(resultNames) > 1
    assert sorted(resultNames) == sorted(expectNames)

    # now check sizes
    for f, short in files.listFiles(dir):
        if short.startswith(startWith):
            resultSize = files.getSize(f)
            expectSizeRow = jslike.find(expected, lambda s: s.startswith(short + '|'))
            expectSize = int(expectSizeRow.split('|')[1])
            assert expectSize == pytest.approx(resultSize, rel=0.05)

@pytest.fixture(name='fxTmpDir')
def fxTmpDirImplementation():
    "A fixture providing a empty directory for testing."
    basedir = files.join(tempfile.gettempdir(), 'shinerainsevenlib_test', 'empty')
    files.ensureEmptyDirectory(basedir)
    yield basedir
    files.ensureEmptyDirectory(basedir)

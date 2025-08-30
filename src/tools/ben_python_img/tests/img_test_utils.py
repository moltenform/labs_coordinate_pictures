
import sys
from PIL import Image
from shinerainsevenlib.standard import *
from shinerainsevenlib.core import *

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

def assertExceptionOrFalse(fn, excType):
    ret = False
    try:
        ret = fn()
    except:
        e = sys.exc_info()[1]
        assertTrue(isinstance(e, excType), 'wrong exc type')
    assertTrue(not ret)

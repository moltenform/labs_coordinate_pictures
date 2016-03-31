
def readOption(key):
    fname = '../options.ini'
    if not files.exists(fname):
        raise RuntimeError('cannot locate options.ini file.')
    
    with open(fname, 'r') as f:
        for line in f:
            if line.startswith(key + '='):
                line = line.strip()
                return line[len(key+'='):]
    raise RuntimeError('key '+key+' not found in options.ini')

def getCwebpLocation():
    ret = readOption('FilepathWebp')
    assertTrue(ret.endswith(files.pathsep + 'cwebp.exe')
    return ret
    
def getMozjpegLocation():
    ret = readOption('FilepathMozJpeg')
    assertTrue(ret.endswith(files.pathsep + 'cjpeg.exe')
    return ret
    
def getMozjpegLocation():
    ret = readOption('FilepathExifTool')
    assertTrue(ret.endswith(files.pathsep + 'exiftool.exe')
    return ret

def getDwebpLocation():
    cwebpLocation = readOption('FilepathWebp')
    return files.join(files.getparent(cwebpLocation), 'dwebp.exe')
    
def getTempLocation():
    # will also be periodically deleted by coordinate_pictures
    import tempfile
    dir = files.join(tempfile.gettempdir(), 'test_labs_coordinate_pictures')
    files.makedirs(dir)
    return dir
    
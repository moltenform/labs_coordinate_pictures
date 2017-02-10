from ben_python_common import *
import sys

# field to store original title in. There's an exif tag OriginalRawFileName AKA OriginalFilename, but isn't shown in UI.
ExifFieldForOriginalTitle = "Copyright"
MarkerString = "__MARKAS__"

def getToolPath(path):
    ret = path + ('.exe' if sys.platform.startswith('win') else '')
    if not files.exists(ret):
        raise RuntimeError('tool not found, did not see ' + ret)
    return ret

def getCwebpLocation():
    return getToolPath('../webp/cwebp')
    
def getMozjpegLocation():
    return getToolPath('../mozjpeg/cjpeg')
    
def getExifToolLocation():
    return getToolPath('../exiftool/exiftool')

def getDwebpLocation():
    return getToolPath('../webp/dwebp')
    
def getTempLocation():
    # will also be periodically deleted by coordinate_pictures
    import tempfile
    dir = files.join(tempfile.gettempdir(), 'test_labs_coordinate_pictures')
    if not files.exists(dir):
        files.makedirs(dir)
    return dir
    
def getImageDims(path):
    from PIL import Image
    with Image.open(path) as im:
        ret = im.size
    return ret

def exiftool():
    return getExifToolLocation()

class PythonImgExifError(Exception):
    pass
    
def verifyExifToolIsPresent():
    path = exiftool()
    if not path or not files.exists(path):
        warn('could not find exiftool, expected to find it at ' + path)

def readExifField(filename, exifField):
    args = "{0}|-S|-{1}|{2}".format(exiftool(), exifField, filename)
    args = args.split('|')
    ret, stdout, stderr = files.run(args, shell=False, throwOnFailure=PythonImgExifError)
    sres = stdout.strip()
    if sres:
        if not startswith(sres, exifField + ': '):
            raise PythonImgExifError('expected ' + exifField + ': but got ' + sres)
        return sres[len(exifField + ': '):]
    else:
        return asbytes('')

def setExifField(filename, exifField, value):
    # -m flag lets exiftool() ignores minor errors
    args = "{0}|-{1}={2}|-overwrite_original|-m|{3}".format(
        exiftool(), exifField, value, filename)
    args = args.split('|')
    files.run(args, shell=False, throwOnFailure=PythonImgExifError)

def readThumbnails(dirname, removeThumbnails, outputDir=None):
    if outputDir and not files.isdir(outputDir):
        files.makedirs(outputDir)
    assertTrue(files.isdir(dirname))
    for filename, short in files.listfiles(dirname):
        if filename.lower().endswith('.jpg'):
            if not removeThumbnails:
                outFile = files.join(outputDir, short)
                assertTrue(not files.exists(outFile), 'file already exists ' + outFile)
                args = "{0}|-b|-ThumbnailImage|{1}|>|{2}".format(
                    exiftool(), filename, outFile)
            else:
                args = "{0}|-overwrite_original|-ThumbnailImage=|{1}".format(exiftool(), filename)
            
            trace(short)
            ret, stdout, stderr = files.run(args.split('|'), shell=True, throwOnFailure=PythonImgExifError)
            trace(stdout)

def readExifCreationTime(filename):
    for field in ('-CreateDate', '-DateTimeOriginal'):
        args = "{0}|{1}|{2}|-T".format(
            exiftool(), field, filename)
        args = args.split('|')
        ret, stdout, stderr = files.run(args, shell=False, throwOnFailure=PythonImgExifError)
        sres = stdout.strip()
        if sres:
            return sres
    return ''

def readOriginalFilename(filename):
    return readExifField(filename, ExifFieldForOriginalTitle)

def stampJpgWithOriginalFilename(filename, originalFilename):
    setExifField(filename, ExifFieldForOriginalTitle, originalFilename)
    
def removeResolutionTags(filename):
    # -m flag lets exiftool() ignores minor errors
    # cannot set to empty string, so follow what Python PIL/Pillow do and set to 1
    args = "{0}|-XResolution=1|-YResolution=1|-ResolutionUnit=None|-overwrite_original|-m|{1}".format(
        exiftool(), filename)
    args = args.split('|')
    ret, stdout, stderr = files.run(args, shell=False, throwOnFailure=PythonImgExifError)

def removeAllExifTags(filename):
    args = "{0}|-all=|-overwrite_original|{1}".format(
        exiftool(), filename)
    args = args.split('|')
    files.run(args, shell=False)
    
def removeAllExifTagsInDirectory(dirname):
    assertTrue(files.isdir(dirname))
    if getInputBool('remove all tags?'):
        for filename, short in files.listfiles(dirname):
            if filename.lower().endswith('.jpg'):
                removeAllExifTags(filename)

def transferMostUsefulExifTags(src, dest):
    '''When resizing a jpg file, all exif data is lost, since we convert to raw as an intermediate step.
    Also, if we copied all exif metadata, modern cameras add quite a bit of exif+xmp, at least 15kb.
    And the thumbnail is often at least 20kb.
    So, use an allow-list to copy over only the most useful exif tags.
    There's a list of fields at http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/EXIF.html'''
    
    # to get everything except xmp and thumbnail, use
    # [exiftool(), '-tagsFromFile', src, '-XMP:All=', '-ThumbnailImage=', '-m', dest]
    
    tagsWanted = [
        'DateTimeOriginal',
        'CreateDate',  # aka DateTimeDigitized
        'Make',
        'Model',
        'LensInfo',
        'FNumber',
        'ExposureTime',
        'ISO',  # aka ISOSpeedRatings
        'ExposureCompensation',  # aka ExposureBiasValue
        'FocalLength',
        'ApertureValue',
        'MaxApertureValue',
        'MeteringMode',
        'Flash',
        'FlashEnergy',
        'FocalLengthIn35mmFormat',  # aka FocalLengthIn35mmFilm
        'DigitalZoomRatio']
        
    cmd = [exiftool(), '-tagsFromFile', src]
    for tag in tagsWanted:
        cmd.append('-' + tag)
    cmd.append('-overwrite_original')
    cmd.append('-m')  # ignore minor errors
    cmd.append(dest)
    files.run(cmd, shell=False, throwOnFailure=PythonImgExifError)
    
def getFilesWrongExtension(root, fnGetFiles, inputExt):
    return set(fnGetFiles(root)) - set(fnGetFiles(root, allowedexts=[inputExt]))
    
def getMarkFromFilename(pathAndCategory):
    '''returns tuple pathWithoutCategory, category'''
    
    # check nothing in path has mark
    if (MarkerString in files.getparent(pathAndCategory)):
        raise ValueError('Directories should not have marker')

    parts = pathAndCategory.split(MarkerString)
    if len(parts) != 2:
        raise ValueError('Expected path to have exactly one marker')

    partsAfterMarker = parts[1].split('.')
    if (len(partsAfterMarker) != 2):
        raise ValueError('Parts after the marker shouldn\'t have another .')
    
    category = partsAfterMarker[0]
    pathWithoutCategory = parts[0] + "." + partsAfterMarker[1]
    return pathWithoutCategory, category

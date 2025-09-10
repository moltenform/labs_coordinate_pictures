import sys
import subprocess
from shinerainsevenlib.standard import *
from shinerainsevenlib.core import *

# field to store original title in. There's an exif tag OriginalRawFileName AKA OriginalFilename, but isn't shown in UI.
ExifFieldForOriginalTitle = "Copyright"
MarkerString = "__MARKAS__"


def getToolPath(path):
    ret = path + ('.exe' if sys.platform.startswith('win') else '')
    if files.findBinaryOnPath(ret):
        return ret
    if files.findBinaryOnPath('../exiftool/' + ret):
        return '../exiftool/' + ret
    if files.findBinaryOnPath('../webp/' + ret):
        return '../webp/' + ret
    if files.findBinaryOnPath('../mozjpeg/' + ret):
        return '../mozjpeg/' + ret

    raise RuntimeError('tool not found, did not see ' + ret)


def getCwebpLocation():
    return getToolPath('cwebp')


def getMozjpegLocation():
    return getToolPath('cjpeg')


def getExifToolLocation():
    return getToolPath('exiftool')


def getDwebpLocation():
    return getToolPath('dwebp')


def getTempLocation():
    # will also be periodically deleted by coordinate_pictures
    import tempfile
    dir = files.join(tempfile.gettempdir(), 'test_labs_coordinate_pictures')
    if not files.exists(dir):
        files.makeDirs(dir)
    return dir


def getImageDims(path):
    from PIL import Image
    with Image.open(path) as im:
        ret = im.size

    return ret


def exiftool():
    # originally we required an absolute path,
    # maybe because scoop wrappers worked differently wrt return codes?
    # scoop wrappers seem to work fine now.
    return getExifToolLocation()


class PythonImgExifError(Exception):
    pass


def verifyExifToolIsPresent():
    path = exiftool()
    if not files.findBinaryOnPath(path):
        warn('could not find exiftool, expected to find it at ' + path)


def readExifField(filename, exifField):
    # returns string, not byte sequence
    assertTrue(isinstance(exifField, str))
    args = [exiftool(), '-S', '-%s' % exifField, filename]

    retcode, stdout, stderr = files.run(args, shell=False, throwOnFailure=PythonImgExifError)
    sres = bytesToString(stdout.strip())
    if sres:
        if not sres.startswith(exifField + ': '):
            raise PythonImgExifError(
                'expected (' + exifField + '): but got (' + sres + ')'
            )
        return sres[len(exifField + ': '):]
    else:
        return ''


def readContainsExifFieldViaStdin(f, exifField):
    # this way we won't be blocked by exiftool not opening unicode filenames
    data = files.readAll(f, 'rb')
    args = [exiftool(), '-S', '-%s' % exifField, '-']

    current_version = sys.version_info
    assertTrue(current_version[0] >= 3 and current_version[1] >= 7)
    kwargs = dict(input=data, capture_output=True)

    if sys.platform.startswith('win'):
        kwargs['creationflags'] = 0x08000000
    results = subprocess.run(args, check=False, **kwargs)
    assertEq(0, results.returncode, 'non zero code', f)
    sres = results.stdout.strip(
    ) # leave in bytes format because we can run into unicode errors otherwise
    if sres:
        if not sres.startswith((exifField + ': ').encode('utf-8')):
            raise PythonImgExifError(
                'expected (' + exifField + '): but got (' + sres + ')'
            )
        v = (sres[len(exifField + ': '):]).strip()
        return bool(v)
    else:
        return False


def setExifField(filename, exifField, value):
    # -m flag lets exiftool() ignores minor errors
    assertTrue(isinstance(exifField, str))
    args = [
        exiftool(),
        '-%s=%s' % (exifField, value), '-overwrite_original', '-m', filename
    ]
    stampBefore = files.getLastModTime(filename, files.TimeUnits.Nanoseconds)
    files.run(args, shell=False, throwOnFailure=PythonImgExifError)
    files.setLastModTime(filename, stampBefore, files.TimeUnits.Nanoseconds)


def readThumbnails(dirname, removeThumbnails, outputDir=None):
    if outputDir and not files.isDir(outputDir):
        files.makeDirs(outputDir)
    assertTrue(files.isDir(dirname))
    for filename, short in files.listFiles(dirname):
        if filename.lower().endswith('.jpg'):
            if not removeThumbnails:
                outFile = files.join(outputDir, short)
                assertTrue(not files.exists(outFile), 'file already exists ' + outFile)
                args = "{0}|-b|-ThumbnailImage|{1}|>|{2}".format(
                    exiftool(), filename, outFile
                )
            else:
                args = "{0}|-overwrite_original|-ThumbnailImage=|{1}".format(
                    exiftool(), filename
                )

            trace(short)
            retcode, stdout, stderr = files.run(
                args.split('|'), shell=True, throwOnFailure=PythonImgExifError
            )
            trace(stdout)


def readExifCreationTime(filename):
    for field in ('-CreateDate', '-DateTimeOriginal'):
        args = "{0}|{1}|{2}|-T".format(exiftool(), field, filename)
        args = args.split('|')
        retcode, stdout, stderr = files.run(
            args, shell=False, throwOnFailure=PythonImgExifError
        )
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
        exiftool(), filename
    )
    args = args.split('|')
    retcode, stdout, stderr = files.run(args, shell=False, throwOnFailure=PythonImgExifError)


def removeAllExifTags(filename):
    args = "{0}|-all=|-overwrite_original|{1}".format(exiftool(), filename)
    args = args.split('|')
    files.run(args, shell=False)


def removeAllExifTagsInDirectory(dirname):
    assertTrue(files.isDir(dirname))
    if getInputBool('remove all tags?'):
        for filename, _short in files.listFiles(dirname):
            if filename.lower().endswith('.jpg') or filename.lower().endswith('.jxl'):
                removeAllExifTags(filename)


def stampJpgWithFilenameInDirectory(dirname, recurse=False, onlyIfNotAlreadySet=True):
    fn = files.recurseFiles if recurse else files.listFiles
    for f, short in fn(dirname):
        if short.lower().endswith('.jpg') or short.lower().endswith('.jxl'):
            if onlyIfNotAlreadySet:
                try:
                    alreadySet = readOriginalFilename(f)
                except:
                    trace('Error seen on', f)
                    raise
                if not alreadySet:
                    trace('setting', f)
                    stampJpgWithOriginalFilename(f, short)
                else:
                    trace('skipping because already set', f)
            else:
                trace('setting', f)
                stampJpgWithOriginalFilename(f, short)


def transferMostUsefulExifTags(src, dest):
    '''When resizing a jpg file, all exif data is lost, since we convert to raw as an intermediate step.
    Also, if we copied all exif metadata, modern cameras add quite a bit of exif+xmp, at least 15kb.
    And the thumbnail is often at least 20kb.
    So, use an allow-list to copy over only the most useful exif tags.
    There's a list of fields at http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/EXIF.html'''

    # to get everything except xmp and thumbnail, use
    # [exiftool(), '-tagsFromFile', src, '-XMP:All=', '-ThumbnailImage=', '-m', dest]

    tagsWanted = [
        ExifFieldForOriginalTitle,
        'DateTimeOriginal',
        'CreateDate', # aka DateTimeDigitized
        'Orientation', # preserve rotation
        'Make',
        'Model',
        'LensInfo',
        'FNumber',
        'ExposureTime',
        'ISO', # aka ISOSpeedRatings
        'ExposureCompensation', # aka ExposureBiasValue
        'FocalLength',
        'ApertureValue',
        'MaxApertureValue',
        'MeteringMode',
        'Flash',
        'FlashEnergy',
        'FocalLengthIn35mmFormat', # aka FocalLengthIn35mmFilm
        'DigitalZoomRatio'
    ]

    cmd = [exiftool(), '-tagsFromFile', src]
    for tag in tagsWanted:
        cmd.append('-' + tag)
    cmd.append('-overwrite_original')
    cmd.append('-m') # ignore minor errors
    cmd.append(dest)
    files.run(cmd, shell=False, throwOnFailure=PythonImgExifError)


def getFilesWrongExtension(root, fnGetFiles, arrInputExt):
    assertTrue(isinstance(arrInputExt, list))
    return set(fnGetFiles(root)) - set(fnGetFiles(root, allowedExts=arrInputExt))


def getMarkFromFilename(pathAndCategory):
    '''returns tuple pathWithoutCategory, category'''

    # check nothing in path has mark
    if MarkerString in files.getParent(pathAndCategory):
        raise ValueError('Directories should not have marker')

    parts = pathAndCategory.split(MarkerString)
    if len(parts) != 2:
        raise ValueError('Expected path to have exactly one marker')

    partsAfterMarker = parts[1].rsplit('.', 1)

    category = partsAfterMarker[0]
    pathWithoutCategory = parts[0] + "." + files.getExt(pathAndCategory)
    return pathWithoutCategory, category


def copyLastModified(infile, outfile):
    lmt = files.getLastModTime(infile, files.TimeUnits.Nanoseconds)
    files.setLastModTime(outfile, lmt, files.TimeUnits.Nanoseconds)

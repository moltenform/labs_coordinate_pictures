import img_utils
import img_convert_resize
import sys
from shinerainsevenlib.standard import *
from shinerainsevenlib.core import *

# Refer to README.md for more documentation.


def cleanup(dir, recurse, informat='jpg', prompt=True):
    '''when the user has reviewed that the conversion looks correct, they'll run cleanup()
    which will discard the previous files with __MARKAS__.'''
    if prompt and not getInputBool('run cleanup?'):
        return

    fnGetFiles = files.recurseFiles if recurse else files.listFiles
    for fullpath, short in fnGetFiles(dir, allowedExts=[informat]):
        if img_utils.MarkerString in short:
            pathWithoutCategory, category = img_utils.getMarkFromFilename(fullpath)
            if files.exists(pathWithoutCategory):
                trace('deleting', short)
                softDeleteFile(fullpath)
            else:
                trace('not deleting', short, 'has not yet been processed')


def resizeAndKeepExif(
    fullpath, storeOriginalFilename, storeExifFromOriginal, jpgHighQualityChromaSampling
):
    '''The filenames tell us what action to take. File named a__MARKAS__50%.jpg becomes a.jpg resized by 50%.'''
    trace(fullpath)
    pathWithoutCategory, category = img_utils.getMarkFromFilename(fullpath)
    assertTrue(
        not files.exists(pathWithoutCategory),
        'file already exists ' + pathWithoutCategory
    )
    if category == '100%':
        files.move(fullpath, pathWithoutCategory, False)
        needTransferTags = False
        fileWasMovedNotCopied = True
    else:
        # tell convertOrResizeImage not to transfer the tags yet, we'll do that ourselves.
        img_convert_resize.convertOrResizeImage(
            fullpath,
            pathWithoutCategory,
            resizeSpec=category,
            jpgQuality=None,
            doTransferMostUsefulExifTags=False
        )
        needTransferTags = True
        fileWasMovedNotCopied = False

    assertTrue(files.exists(pathWithoutCategory))

    try:
        if storeOriginalFilename:
            img_utils.stampJpgWithOriginalFilename(
                pathWithoutCategory, files.getName(pathWithoutCategory)
            )
        if storeExifFromOriginal and needTransferTags:
            img_utils.transferMostUsefulExifTags(fullpath, pathWithoutCategory)
    except img_utils.PythonImgExifError as e:
        # upon exception, move it back to the original spot.
        trace('Exif exception occurred ' + str(e) + 'for file ' + fullpath)
        if fileWasMovedNotCopied:
            assertTrue(not files.exists(fullpath))
            files.move(pathWithoutCategory, fullpath, False)
        else:
            assertTrue(files.exists(fullpath))
            softDeleteFile(pathWithoutCategory)


def resizeAllAndKeepExif(
    root,
    recurse,
    storeOriginalFilename,
    storeExifFromOriginal,
    jpgHighQualityChromaSampling,
    inputFormat=None
):
    if inputFormat is None:
        inputFormat = ['jpg']
    fnGetFiles = files.recurseFiles if recurse else files.listFiles
    filesWithWrongExtension = img_utils.getFilesWrongExtension(
        root, fnGetFiles, inputFormat
    )
    if len(filesWithWrongExtension) > 0:
        warn('files seen with wrong extension: ' + str(filesWithWrongExtension))

    if storeOriginalFilename or storeExifFromOriginal:
        img_utils.verifyExifToolIsPresent()

    # If we didn't call list(files) to first freeze the list of files to process, we would encounter as input the files we just created.
    allfiles = list(fnGetFiles(root, allowedExts=inputFormat))
    for fullpath, _short in allfiles:
        if img_utils.MarkerString not in fullpath:
            continue

        resizeAndKeepExif(
            fullpath, storeOriginalFilename, storeExifFromOriginal,
            jpgHighQualityChromaSampling
        )


def simpleResize(
    root,
    recursive,
    inputformat='png',
    outputformat='jpg',
    resizeSpec='100%',
    jpgQuality=None,
    addPrefix='',
    softDeleteOriginals=False
):
    # If we didn't call list(files) to first freeze the list of files to process, we would encounter as input the files we just created.
    fnGetFiles = files.recurseFiles if recursive else files.listFiles
    allfiles = list(fnGetFiles(root, allowedExts=[inputformat]))
    for fullpath, short in allfiles:
        if files.getExt(fullpath) == inputformat:
            trace(short)
            outname = files.getParent(fullpath) + files.sep + addPrefix + files.splitExt(
                short
            )[0] + '.' + outputformat
            if files.exists(outname):
                trace('already exists', outname)
            else:
                img_convert_resize.convertOrResizeImage(
                    fullpath, outname, resizeSpec=resizeSpec, jpgQuality=jpgQuality
                )
                assertTrue(files.exists(outname))
                if softDeleteOriginals:
                    softDeleteFile(fullpath)


if __name__ == '__main__':
    topDir = '/path/to/directory_with_files_to_resize'
    topRecurse = False
    topStoreOriginalFilename = getInputBool('store original filename in exif data?')
    topStoreExifFromOriginal = True
    topJpgHighQualityChromaSampling = False

    # resizeAllAndKeepExif(topDir, topRecurse, topStoreOriginalFilename, topStoreExifFromOriginal, topJpgHighQualityChromaSampling)
    # cleanup(topDir, topRecurse)

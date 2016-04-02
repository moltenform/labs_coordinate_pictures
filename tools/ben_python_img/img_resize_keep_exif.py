from ben_python_common import *
import img_utils
import img_convert_resize

def cleanup(dir, recurse, informat='jpg'):
    if not getInputBool('run cleanup?'):
        return
    
    fnGetFiles = files.recursefiles if recurse else files.listfiles
    for fullpath, short in fnGetFiles(dir, allowedexts=[informat]):
        if img_utils.MarkerString in short:
            pathWithoutCategory, category = img_utils.getMarkFromFilename(fullpath)
            if files.exists(pathWithoutCategory):
                trace('deleting', short)
                softDeleteFile(fullpath)
            else:
                trace('not deleting', short, 'has not yet been processed')

def resizeAndKeepExif(fullpath, storeOriginalFilename, storeExifFromOriginal, jpgHighQualityChromaSampling):
    '''The filenames tell us what action to take. File named a__MARKAS__50%.jpg becomes a.jpg resized by 50%.'''
    trace(fullpath)
    pathWithoutCategory, category = img_utils.getMarkFromFilename(fullpath)
    assertTrue(not files.exists(pathWithoutCategory), 'file already exists ' + pathWithoutCategory)
    if category == '100%':
        files.move(fullpath, pathWithoutCategory, False)
        needTransferTags = False
        fileWasMovedNotCopied = True
    else:
        ret = img_convert_resize.convertOrResizeImage(fullpath, pathWithoutCategory, resizeSpec=category, jpgQuality=None)
        needTransferTags = ret != img_convert_resize.ConvertResult.SuccessCopied
        fileWasMovedNotCopied = False
    
    assertTrue(files.exists(pathWithoutCategory))
    
    try:
        if storeOriginalFilename:
            img_utils.stampJpgWithOriginalFilename(pathWithoutCategory, files.getname(pathWithoutCategory))
        if storeExifFromOriginal and needTransferTags:
            img_utils.transferMostUsefulExifTags(fullpath, pathWithoutCategory)
    except img_utils.PythonImgExifException as e:
        # upon exception, move it back to the original spot.
        trace('Exif exception occurred ' + str(e) + 'for file ' + fullpath)
        if fileWasMovedNotCopied:
            assertTrue(not files.exists(fullpath))
            files.move(pathWithoutCategory, fullpath, False)
        else:
            assertTrue(files.exists(fullpath))
            softDeleteFile(pathWithoutCategory)

def resizeAllAndKeepExif(root, recurse, storeOriginalFilename, storeExifFromOriginal, jpgHighQualityChromaSampling):
    inputformat = 'jpg'
    fnGetFiles = files.recursefiles if recurse else files.listfiles
    filesWithWrongExtension = img_utils.getFilesWrongExtension(root, fnGetFiles, inputformat)
    if len(filesWithWrongExtension) > 0:
        warn('files seen with wrong extension: ' + str(filesWithWrongExtension))
    
    allfiles = list(fnGetFiles(root, allowedexts=[inputformat]))
    for fullpath, short in allfiles:
        if img_utils.MarkerString not in fullpath:
            continue
        
        resizeAndKeepExif(fullpath, storeOriginalFilename, storeExifFromOriginal, jpgHighQualityChromaSampling)


if __name__ == '__main__':
    root = r'C:\test'
    recurse = False
    storeOriginalFilename = getInputBool('store original filename in exif data?')
    storeExifFromOriginal = True
    jpgHighQualityChromaSampling = False
    
    # resizeAllAndKeepExif(root, recurse, inputExt, storeOriginalFilename, storeExifFromOriginal, jpgHighQualityChromaSampling)
    # cleanup(root, recurse)
    
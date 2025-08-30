
from PIL import Image
import img_utils
import sys
from enum import StrEnum, auto
from shinerainsevenlib.standard import *
from shinerainsevenlib.core import *
from io import BytesIO as cBytesIO

class ConvertResult(StrEnum):
    SuccessConverted = auto()
    SuccessCopied = auto()

def convertOrResizeImage(infile, outfile, resizeSpec='100%',
        jpgQuality=None, jpgHighQualityChromaSampling=False, jpgCorrectResolution=False,
        doTransferMostUsefulExifTags=True, useIm=False):

    if files.getExt(outfile) == 'jpg':
        useMozJpeg = True
        defaultJpgQuality = 94 if useMozJpeg else 93
        if not jpgQuality:
            jpgQuality = defaultJpgQuality

    if files.getExt(outfile) != 'jpg' and jpgQuality and jpgQuality != 100:
        raise ValueError('only jpg files can have a quality less than 100.')
            
    if sys.platform == 'win32' and infile.lower() == outfile.lower():
        return ConvertResult.SuccessCopied
    
    if infile == outfile:
        return ConvertResult.SuccessCopied
        
    if files.exists(outfile):
        raise ValueError('output file already exists.')
        
    if not files.exists(infile):
        raise ValueError('input file not found.')
        
    needsNoResize = resizeSpec == '100%'
    if resizeSpec.endswith('h'):
        width, height = img_utils.getImageDims(infile)
        if getNewSizeFromResizeSpec(resizeSpec, width, height, loggingContext=infile) == (0, 0):
            needsNoResize = True
        
    if needsNoResize:
        # shortcut: just copy the file if no format conversion or resize
        if files.getExt(infile) == files.getExt(outfile):
            files.copy(infile, outfile, False)
            return ConvertResult.SuccessCopied
        
    if useIm:
        assertTrue(not jpgHighQualityChromaSampling)
        assertTrue(not jpgCorrectResolution)
        assertTrue(not doTransferMostUsefulExifTags)
        return convertOrResizeImageIm(infile, outfile, resizeSpec, jpgQuality)
    
    if needsNoResize:
        # shortcut: dwebp natively can save to common formats
        dwebpsupports = ['png', 'tif']
        if files.getExt(infile) == 'webp' and files.getExt(outfile) in dwebpsupports:
            saveWebpToPng(infile, outfile)
            img_utils.copyLastModified(infile, outfile)
            return ConvertResult.SuccessConverted
            
        # shortcut: cwebp natively can save from common formats
        cwebpsupports = ['bmp', 'png', 'tif']
        if files.getExt(outfile) == 'webp' and files.getExt(infile) in cwebpsupports:
            saveBmpOrPngToWebp(infile, outfile)
            img_utils.copyLastModified(infile, outfile)
            return ConvertResult.SuccessConverted
            
        # shortcut: mozjpeg takes a bmp, we have a bmp.
        if files.getExt(infile) == 'bmp' and files.getExt(outfile) == 'jpg':
            saveToMozJpeg(False, infile, outfile,
                jpgQuality, jpgHighQualityChromaSampling, jpgCorrectResolution)
            img_utils.copyLastModified(infile, outfile)
            return ConvertResult.SuccessConverted
            
    im = None
    tmpPngOut = None
    memoryStreamIn = None
    memoryStreamOut = None
    try:
        # load and resize image
        im, memoryStreamIn = loadImageFromFile(infile, outfile)
        im = resizeImage(im, resizeSpec, outfile)
        
        # save image
        if files.getExt(outfile) == 'jpg':
            memoryStreamOut = cBytesIO()
            im.save(memoryStreamOut, format='bmp')
            saveToMozJpeg(True, memoryStreamOut.getvalue(),
                outfile, jpgQuality, jpgHighQualityChromaSampling, jpgCorrectResolution)
        elif files.getExt(outfile) == 'webp':
            tmpPngOut = getTempFilename('png')
            im.save(tmpPngOut)
            assertTrue(files.exists(tmpPngOut))
            saveBmpOrPngToWebp(tmpPngOut, outfile)
        else:
            im.save(outfile)
            assertTrue(files.exists(outfile))
            
    finally:
        im.close()
        del im
        if tmpPngOut:
            files.deleteSure(tmpPngOut)
        if memoryStreamIn:
            memoryStreamIn.close()
            del memoryStreamIn
        if memoryStreamOut:
            memoryStreamOut.close()
            del memoryStreamOut

    if files.getExt(infile) == 'jpg' and files.getExt(outfile) == 'jpg':
        if doTransferMostUsefulExifTags:
            try:
                img_utils.transferMostUsefulExifTags(infile, outfile)
            except BaseException as e:
                # upon exception, just print a message and continue
                trace('Exif exception occurred ' + str(e) + 'for ' + infile)
                trace('Continuing...')

    img_utils.copyLastModified(infile, outfile)
    return ConvertResult.SuccessConverted

def loadImageFromFile(infile, outfile):
    memoryStream = None
    if files.getExt(infile) == 'webp':
        im, memoryStream = loadImageFromWebp(infile)
    else:
        im = Image.open(infile)
    
    # discard the transparency channel (by replacing with white) if saving to jpg
    if im.mode == 'RGBA' and (outfile.lower().endswith('jpg') or outfile.lower().endswith('bmp')):
        newimg = Image.new("RGB", im.size, (255, 255, 255))
        newimg.paste(im, mask=im.split()[3])  # 3 is the alpha channel
        im = newimg
        
    return im, memoryStream

def runProcessShowErr(args):
    retcode, stdout, stderr = files.run(args, shell=False, createNoWindow=True,
        throwOnFailure=None, stripText=True, captureOutput=True)
    if retcode != 0:
        raise RuntimeError('failure running ' + str(args) + ' stderr=' + str(stderr))
        
def runProcessWithStdIn(args, sendToStdIn):
    import subprocess
    showNoWindow = 0x08000000 if sys.platform.startswith('win') else 0
    sp = subprocess.Popen(args, shell=False, stdin=subprocess.PIPE,
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, creationflags=showNoWindow)
    comm = sp.communicate(input=sendToStdIn)
    stderr = comm[1]
    retcode = sp.returncode
    if retcode != 0:
        raise RuntimeError('failure running ' + str(args) + ' stderr=' + str(stderr))

def getTempFilename(ext):
    tempdir = img_utils.getTempLocation()
    return files.join(tempdir, getRandomString() + '.' + ext)
    
def loadImageFromWebp(infile):
    # Pillow supports webp, but since we already depend on official tools, may as well use them here.
    # dwebp sends a png to stdout, we'll read it from stdout.
    dwebp = img_utils.getDwebpLocation()
    args = [dwebp, infile, '-o', '-']
    retcode, stdout, stderr = files.run(args, shell=False, createNoWindow=True,
        throwOnFailure=None, stripText=False, captureOutput=True)
    if retcode != 0:
        raise RuntimeError('failure running ' + str(args) + ' stderr=' + str(stderr))

    # read the png directly from memory
    memoryStream = cBytesIO(stdout)
    return Image.open(memoryStream), memoryStream

def saveWebpToPng(infile, outfile):
    dwebp = img_utils.getDwebpLocation()
    args = [dwebp, infile, '-o', outfile]
    if files.getExt(outfile) == 'bmp':
        # the bmp written by dwebp does not seem to be readable by mozjpeg.
        raise ValueError('Format not supported')
    elif files.getExt(outfile) == 'tif':
        args.append('-tiff')
    runProcessShowErr(args)
    if not files.exists(outfile):
        raise RuntimeError('failure running ' + str(args) + ' output not found')
    
    return outfile

def saveBmpOrPngToWebp(infile, outfile):
    # specifying -q 100 even when mode is -lossless results in smaller files (at expense of time).
    cwebp = img_utils.getCwebpLocation()
    args = [cwebp, infile, '-lossless', '-z', '9', '-m', '6', '-q', '100', '-o', outfile]
    runProcessShowErr(args)
    if not files.exists(outfile):
        raise RuntimeError('failure running ' + str(args) + ' output not found')
    
    return outfile

def saveToMozJpeg(infileIsMemoryStream, infile, outfile,
        quality, useBetterChromaSample, jpgCorrectResolution):
    # cjpeg cannot convert from png or tif, it needs a bmp.
    # we used to write a temporary bmp to disk, but it's better to write the bmp to memory and send it via stdin.
    assertTrue(isinstance(quality, int))
    args = [img_utils.getMozjpegLocation()]
    args.extend(['-quality', str(quality), '-optimize'])
    if useBetterChromaSample:
        args.extend(['-sample', '1x1'])
        
    args.extend(['-outfile', outfile])
    if not infileIsMemoryStream:
        # input provided by file on disk
        args.extend([infile])
        runProcessShowErr(args)
    else:
        # input provided through stdin
        runProcessWithStdIn(args, infile)
        
    if not files.exists(outfile):
        raise RuntimeError('failure running ' + str(args) + ' output not found')
    
    # for some reason mozjpeg sets xresolution=94, it is usually 96.
    if jpgCorrectResolution:
        img_utils.removeResolutionTags(outfile)

def getNewSizeFromResizeSpec(resizeSpec, width, height, loggingContext=''):
    # returning 0, 0 means to return the original image unchanged.
    if not resizeSpec:
        return 0, 0
    elif resizeSpec == '100%':
        return 0, 0
    elif resizeSpec.endswith('%'):
        assertTrue(resizeSpec[0:-1].isdigit(), 'spec not digits ' + loggingContext)
        mult = int(resizeSpec[0:-1]) / 100.0
        assertTrue(mult > 0.0 and mult < 1.0, 'invalid mult ' + loggingContext)
        newWidth = int(width * mult)
        newHeight = int(height * mult)
        return newWidth, newHeight
    elif resizeSpec.endswith('h'):
        assertTrue(resizeSpec[0:-1].isdigit(), 'spec not digits ' + loggingContext)
        smallestDimensionTarget = int(resizeSpec[0:-1])
        
        # set based on the smallest dimension, which isn't always height
        if width >= height:
            if smallestDimensionTarget >= height:
                # if asked to enlarge, return the original
                return 0, 0
            newWidth = int((float(width) / height) * smallestDimensionTarget)
            newHeight = smallestDimensionTarget
        else:
            if smallestDimensionTarget >= width:
                # if asked to enlarge, return the original
                return 0, 0
            newHeight = int((float(height) / width) * smallestDimensionTarget)
            newWidth = smallestDimensionTarget
        
        if not(newWidth % 8 == 0 and newHeight % 8 == 0):
            warn('%s %d %d %d %d'%(loggingContext, width, height, newWidth, newHeight) +
                'we\'d like height and width to both be a multiple of 8.')
        return newWidth, newHeight
    else:
        raise ValueError('unknown resizeSpec')
        
def resizeImage(im, resizeSpec, loggingContext):
    newWidth, newHeight = getNewSizeFromResizeSpec(resizeSpec, im.size[0], im.size[1], loggingContext)
    if newWidth == 0 and newHeight == 0:
        return im
    else:
        # if enlarging, consider Image.BICUBIC
        ret = im.resize((newWidth, newHeight), Image.Resampling.LANCZOS)
        return ret

def convertOrResizeImageIm(infile, outfile, resizeSpec, jpgQuality):
    args = ['magick', 'convert', infile]
    if resizeSpec and resizeSpec != '100%':
        assertTrue(resizeSpec.endswith('%'))
        args.extend(['-resize', resizeSpec])
    
    if jpgQuality:
        jpgQuality = str(int(jpgQuality))
        args.extend(['-quality', jpgQuality])
    
    args.append(outfile)
    files.run(args)
    assertTrue(files.exists(outfile))
    img_utils.copyLastModified(infile, outfile)
    return ConvertResult.SuccessConverted

if __name__ == '__main__':
    if len(sys.argv) > 1:
        cmd = sys.argv[1]
        if cmd == 'convert_resize':
            _, topCmd, topInfile, topOutfile, topResizeSpec, topJpgQuality = sys.argv
            convertOrResizeImage(topInfile, topOutfile, topResizeSpec, int(topJpgQuality))
        else:
            assertTrue(False, 'unknown command' + cmd)

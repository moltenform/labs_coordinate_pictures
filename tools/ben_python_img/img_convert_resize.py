
from ben_python_common import *
from PIL import Image
import readoptions
    
def convertOrResizeImage(infile, outfile, markToResize='100%', jpgQuality=None, jpgHighQualityChromaSampling=False, jpgCorrectResolution=False):
    useMozJpeg = True
    if not jpgQuality:
        jpgQuality = 94 if useMozJpeg else 93
            
    if infile.lower() == outfile.lower():
        raise ArgumentError('writing to itself.')
        
    if files.exists(outfile):
        raise ArgumentError('output file already exists.')
        
    if not files.exists(infile):
        raise ArgumentError('input file not found.')
        
    if markToResize=='100%':
        # shortcut: just copy the file if no format conversion or resize
        if files.getext(infile) == files.getext(outfile):
            files.copy(infile, outfile, False)
            return
        
        # shortcut: dwebp natively can save to bmp or png
        if files.getext(infile) == 'webp' and (files.getext(outfile) == 'bmp' or files.getext(outfile) == 'png'):
            saveFromWebp(infile, outfile)
            return
            
        # shortcut: mozjpeg takes a bmp, we have a bmp.
        if files.getext(infile) == 'bmp' and files.getext(outfile) == 'jpg':
            saveBmpToMozJpeg(infile, outfile, jpgQuality, jpgHighQualityChromaSampling, jpgCorrectResolution)
            return
            
        # shortcut: mozjpeg takes a bmp, skip extra bmp conversion.
        if files.getext(infile) == 'webp' and files.getext(outfile) == 'jpg':
            tmpBmp = getTempFilename('bmp')
            saveFromWebp(infile, tmpBmp)
            try:
                saveBmpToMozJpeg(tmpBmp, outfile, jpgQuality, jpgHighQualityChromaSampling, jpgCorrectResolution)
            finally:
                files.deletesure(tmpBmp)
            return
            
    # PIL can't work directly with webp so convert to png first.
    tmpBmp = None
    tmpPng = None
    if files.getext(infile) == 'webp':
        tmpPng = getTempFilename('png')
        saveFromWebp(infile, tmpPng)
        infile = tmpPng
        
    try:
        # load and resize image
        im = loadImageFromFile(infile)
        if markToResize != '100%':
            im = resizeImage(im, markToResize)
            
        # save image
        if files.getext(outfile) == 'jpg':
            tmpBmp = getTempFilename('bmp')
            im.save(tmpBmp)
            saveBmpToMozJpeg(tmpBmp, outfile, jpgQuality, jpgHighQualityChromaSampling, jpgCorrectResolution)
        else:
            im.save(outfile)
            
    finally:
        del im
        if tmpBmp:
            files.deletesure(tmpBmp)
        if tmpPng:
            files.deletesure(tmpPng)


def resizeImage(im, markToResize):
    assert markToResize != '100%'
    return im

def loadImageFromFile(infile, outfile):
    img = Image.open(infile)
    
    # discard the transparency channel if saving to jpg
    if img.mode == 'RGBA' and (outfile.lower().endswith('jpg') or outfile.lower().endswith('bmp')):
        newimg = Image.new("RGB", img.size, (255, 255, 255))
        newimg.paste(img, mask=img.split()[3]) # 3 is the alpha channel
        img = newimg
        
    return img

def runExeShowErr(args):
    retcode, stdout, stderr = files.run(args, shell=False, createNoWindow=True,
        throwOnFailure=None, stripText=True, captureoutput=True)
    if retcode != 0:
        raise RuntimeError('failure running ' + str(args) + ' stderr='+stderr)
     
def getTempFilename(ext):
    tempdir = readoptions.getTempLocation()
    tempfilepath = files.join(tempdir, getRandomString() + '.' + ext)
    
def saveFromWebp(infile, outfile):
    dwebp = readoptions.getDwebpLocation()
    args = [dwebp, infile, '-o', outfile]
    runExeShowErr(args)
    if not files.exist(outfile):
        raise RuntimeError('failure running ' + str(args) + ' output not found')
    
    return outfile

def saveBmpToMozJpeg(infile, outfile, quality, useBetterChromaSample, jpgCorrectResolution):
    assertTrue(isinstance(quality, int))
    args = [readoptions.getMozjpegLocation()]
    args.extend(['-quality', str(quality), '-optimize'])
    if useBetterChromaSample:
        args.extend(['-sample', '1x1'])
        
    args.extend(['-outfile', outfile, infile])
    runExeShowErr(args)
    if not files.exist(outfile):
        raise RuntimeError('failure running ' + str(args) + ' output not found')
    
    # mozjpeg sets xresolution=94 for some reason, it is usually 96.
    if jpgCorrectResolution:
        import img_exif
        img_exif.deleteResolutionTagsOne(outfile)
    

        
            
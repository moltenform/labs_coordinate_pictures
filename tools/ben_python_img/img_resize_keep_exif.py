from ben_python_common import *


def getFilesBadExtension(root, fnGetFiles, informat):
    return set(fnGetFiles(root)) - set(fnGetFiles(root, allowedexts=[informat]))

def cleanup(dir, recurse=False, informat='jpg'):
    run = getInputBool('run cleanup?')
    if not run:
        return
    print '---'
    fnGetFiles = files.recursefiles if recurse else files.listfiles
    for fullpath, filename in fnGetFiles(dir, allowedexts=[informat]):
        percentage, sPathNew, type = parseFilepathGetPercentage(fullpath)
        if percentage is None:
            continue
        
        if files.exists(sPathNew) and '__MARKAS__' in filename:
            print 'deleting ' + filename
            softDeleteFile(fullpath)
        else:
            print 'not deleting ' + filename + ' because has not been processed yet'
            continue
